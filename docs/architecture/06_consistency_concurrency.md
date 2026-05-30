# Distributed Consistency & Concurrency Semantics
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0
**Status:** LOCKED
**Phase:** Phase 3 — Architectural Stabilization (Pre-LLD)
**Classification:** Project Internal — Binding Operational Contract

---

## Purpose

This document formalizes operational semantics for distributed, asynchronous, eventual-consistency systems.

Without this:
- Two engineers implement different retry rules
- Duplicate events produce divergent state
- Compensations race with success paths
- Stale commands mutate valid state
- Replay safety becomes inconsistent

This document is the bridge from "DDD diagrams" to "operationally survivable distributed system."

---

## 1. Command Consistency Rules

### 1.1 Command Classification

Every command in UTOP falls into exactly one category:

| Category | Definition | Idempotent | Retryable | Strong Consistency | Compensation |
|----------|-----------|-----------|-----------|-------------------|--------------|
| **Strong Invariant** | Enforces critical business rule; no duplicate execution | Yes | Yes | **YES** | Escalate |
| **Transactional** | Updates state with saga coordination | Yes | Yes | **YES** | Saga step |
| **Idempotent Async** | Safe duplicate execution; order independent | Yes | Yes | NO | None or recompute |
| **Notification** | Best-effort delivery; duplicates harmless | Yes | Yes | NO | None |

### 1.2 Command Consistency Per Booking Context

| Command | Category | Idempotent | Retryable | Strong Consistency | Conflict Resolution | Stale Command Behavior |
|---------|----------|-----------|-----------|-------------------|-------------------|----------------------|
| CreateBooking | Strong Invariant | Yes | No (duplicates rejected) | YES | Reject duplicate | Reject if operator inactive |
| ValidateAvailability | Transactional | Yes | Yes | YES | Reject old attempts | Reject if past window |
| ConfirmBooking | Strong Invariant | Yes | Yes | YES | Optimistic concurrency (stale writer rejected) | Reject if departed |
| AmendBooking | Strong Invariant | Yes | Yes | YES | Optimistic concurrency (stale writer rejected) | Reject if 2h window passed |
| CancelBooking | Strong Invariant | Yes | Yes | YES | Idempotent (already cancelled = success) | Reject if departed |
| AllocateResource | Transactional | Yes | Yes | YES | First-come-wins (conflict = escalate) | Reject if already allocated |

### 1.3 Command Idempotency Mechanism

**Per-command idempotency key:**

```csharp
public interface ICommand
{
    string IdempotencyKey { get; }  // Unique per logical command
    Guid CommandId { get; }          // Instance UUID for causation tracing
}

// Examples:
CreateBooking:
  IdempotencyKey = $"{OperatorId}|{Mode}|{Route.Origin}|{Route.Destination}|{DepartureDate}|{PassengerCount}"
  → Prevents duplicate bookings for exact same operator/route/date/passenger combo

ConfirmBooking:
  IdempotencyKey = $"booking-confirm:{BookingId}"
  → Safe to retry; idempotent confirmation

AmendBooking:
  IdempotencyKey = $"booking-amend:{BookingId}|{AmendmentVersion}"
  → Multiple amendments safe; each version tracked

CancelBooking:
  IdempotencyKey = $"booking-cancel:{BookingId}"
  → Idempotent; already-cancelled returns success
```

**Storage:**

```sql
CREATE TABLE command_idempotency_log (
    idempotency_key VARCHAR(500) PRIMARY KEY,
    aggregate_id VARCHAR(200) NOT NULL,
    command_type VARCHAR(100) NOT NULL,
    command_id UUID NOT NULL,
    result JSONB NOT NULL,                  -- command result/return value
    created_at TIMESTAMPTZ NOT NULL,
    INDEX(aggregate_id, command_type)
);

-- Lookup on arrival:
SELECT result FROM command_idempotency_log
WHERE idempotency_key = @key
  AND created_at > NOW() - INTERVAL '24h'  -- retention window

-- If found: return cached result (no re-execution)
-- If not found: execute command, store result
```

---

## 2. Event Delivery Semantics

### 2.1 Delivery Guarantee Model

UTOP guarantees: **At-Least-Once Delivery (ALD)**

| Guarantee | Definition | Implication |
|-----------|-----------|------------|
| **At-Most-Once** | Event delivered 0 or 1 time | Acceptable for notifications; NOT for business events |
| **At-Least-Once** | Event delivered ≥ 1 time | **UTOP Standard** for domain events |
| **Exactly-Once (transport)** | Infrastructure guarantees single delivery | Not relied upon in UTOP; not achievable without distributed transactions |
| **Exactly-Once (application-level)** | Duplicates neutralized at business layer via idempotency + deduplication + version checks | **UTOP approach** — transport delivers at-least-once; business layer produces exactly-once *effect* |

### 2.2 Outbox Pattern (Prevents Message Loss)

Domain events are **persisted to database BEFORE publishing to RabbitMQ**.

```
Aggregate State Change:
  1. Update aggregate in utop_booking.bookings
  2. Insert event into shared.outbox_events (SAME TRANSACTION)
  3. Commit transaction
  4. Background job: Poll outbox; publish to RabbitMQ
  5. After RabbitMQ ACK: mark event as published

If process crashes between steps 3 and 5:
  Event remains in outbox; is replayed on restart
  No message loss
```

**Outbox Table Schema:**

```sql
CREATE TABLE shared.outbox_events (
    id BIGSERIAL PRIMARY KEY,
    aggregate_id VARCHAR(200) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_id UUID NOT NULL UNIQUE,
    correlation_id VARCHAR(100) NOT NULL,
    causation_id UUID,                          -- parent command UUID
    payload JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    published_at TIMESTAMPTZ,                   -- NULL until published
    publish_retry_count INT DEFAULT 0,
    status VARCHAR(50) DEFAULT 'PENDING',       -- PENDING, PUBLISHED, FAILED
    error_message TEXT,
    INDEX(status, created_at),
    INDEX(aggregate_id, event_type)
);
```

**Publishing Process:**

```csharp
// Background job (runs every 5 seconds)
public class OutboxPublisher
{
    public async Task PublishPendingEventsAsync()
    {
        var pendingEvents = await _db.OutboxEvents
            .Where(e => e.Status == "PENDING" && e.PublishRetryCount < 5)
            .OrderBy(e => e.CreatedAt)
            .Take(100)
            .ToListAsync();

        foreach (var evt in pendingEvents)
        {
            try
            {
                await _eventBus.PublishAsync(evt.EventType, evt.Payload);
                evt.PublishedAt = DateTime.UtcNow;
                evt.Status = "PUBLISHED";
                await _db.SaveChangesAsync();
            }
            catch
            {
                evt.PublishRetryCount++;
                if (evt.PublishRetryCount >= 5)
                {
                    evt.Status = "FAILED";
                    // Alert ops; manual intervention required
                }
                await _db.SaveChangesAsync();
            }
        }
    }
}
```

### 2.3 Inbox Pattern (Prevents Duplicate Processing)

Event consumers **store processed event IDs in inbox** before processing.

```
Event Arrives:
  1. Check: is event_id in shared.inbox_events?
  2. If YES: already processed; skip
  3. If NO:
    a. Insert event_id into inbox (MARK AS PROCESSING)
    b. Execute event handler
    c. Update inbox: mark as PROCESSED
    d. Commit

If handler crashes or times out:
  Event_id marked as PROCESSING; not re-processed until stale timeout
  Prevents double-execution
```

**Inbox Table Schema:**

```sql
CREATE TABLE shared.inbox_events (
    id BIGSERIAL PRIMARY KEY,
    event_id UUID NOT NULL UNIQUE,
    event_type VARCHAR(100) NOT NULL,
    source_context VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    status VARCHAR(50) DEFAULT 'PROCESSING',   -- PROCESSING, PROCESSED, FAILED
    handler_type VARCHAR(200),
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    processing_started_at TIMESTAMPTZ,         -- Lease start time
    processed_at TIMESTAMPTZ,
    reclaim_count INT DEFAULT 0,               -- Times reclaimed after stale lease
    INDEX(status, processing_started_at),      -- For lease reclaim queries
    INDEX(event_id)
);
```

**Inbox PROCESSING Lease Semantics:**

PROCESSING entries are lease-based. A consumer holds a lease while processing. If it crashes, the lease expires and the event becomes reclaimable.

```
Lease Duration: 5 minutes (configurable per event type)

Reclaim Logic (background job, runs every 60 seconds):
  SELECT * FROM inbox_events
  WHERE status = 'PROCESSING'
    AND processing_started_at < NOW() - INTERVAL '5 minutes'
    AND reclaim_count < 3

  For each stale entry:
    UPDATE status = 'PROCESSING'
           processing_started_at = NOW()
           reclaim_count = reclaim_count + 1
    → Re-dispatch to handler

  If reclaim_count >= 3:
    UPDATE status = 'FAILED'
           error_message = 'Max reclaim attempts exceeded; manual intervention required'
    → Alert ops; event in dead-letter state

Result:
  One dead consumer cannot permanently freeze an event
  Max 3 reclaim attempts before ops escalation
  Each reclaim attempt is idempotent (handler checks inbox before processing)
```

**Poison Message Handling:**

```
If event handler repeatedly throws (not crash, but exception):
  Increment reclaim_count
  After 3 attempts: mark FAILED
  Move to dead-letter inspection queue
  Do NOT block other events for same context
  Alert ops via notification
```

### 2.4 Event Ordering Guarantees

UTOP **targets causal ordering per aggregate** but does not assume infrastructure-enforced strict ordering.

RabbitMQ does not guarantee sequential processing under: redelivery, retries, horizontal consumer scaling, poison messages, or network partitions. Consumers MUST tolerate:
- Delayed delivery
- Duplicate delivery
- Out-of-order delivery
- Replay delivery

**Aggregate versioning and idempotency are the authoritative correctness mechanisms — not message ordering.**

```
Per Aggregate (e.g., Booking ABC-123):
  Intent: BookingCreated (t=1) before BookingConfirmed (t=2)
  Reality: May arrive out of order; consumer must handle via version check

  If BookingConfirmed arrives before BookingCreated:
    → Consumer checks aggregate version
    → If version mismatch: reject/requeue
    → Aggregate refuses transition from non-existent state (invariant)
    → Event redelivered after BookingCreated processed

Across Aggregates (Booking XYZ-456 and Group GRP-789):
  No ordering guarantee
  Must be idempotent and order-independent
```

**Implementation:**

```csharp
// RabbitMQ routing key includes aggregate ID for partitioning
routing_key = $"domain.{aggregateType}.{aggregateId}"

// Single consumer per aggregate type recommended during development
// Horizontal scaling must account for per-aggregate ordering via consistent hashing
// If same aggregate: route to same consumer instance; if different: parallel consumers allowed
```

### 2.5 Replay Semantics

**Replay used in:**
- Disaster recovery
- Analytics re-projection
- New consumer onboarding
- Debugging/investigation

**Critical distinction: Operational consumers vs Projection consumers**

```
Operational consumers (Booking, ResourceAllocation, CostSplitting):
  MUST deduplicate via inbox
  MUST NOT reprocess already-processed events
  Inbox deduplication: ENFORCED

Projection/rebuild consumers (Analytics, new context onboarding):
  MAY bypass inbox deduplication
  MUST be declared as replay consumers explicitly
  Replay mode flag: consumer_type = 'REPLAY'
  Inbox deduplication: BYPASSED (intentional)

Replay Consumer Hard Rules (non-negotiable):
  MUST NOT mutate operational aggregates
  MUST NOT emit new operational domain events
  MUST NOT trigger live sagas or command handlers
  ARE restricted to read-model/projection targets only
  Exception: explicitly approved recovery workflows (requires architecture change request)
```

**Replay Guarantees:**

```
Source of Truth: shared.outbox_events (append-only; see Section 3.4)
Replay ordering guarantee: Per-aggregate stream order only — NOT globally deterministic across aggregates.
  → Within one aggregate: events replayed in original creation order
  → Across aggregates: no global ordering; timestamp collisions possible
  → Replay engines must not assume global timestamp ordering
Idempotency: Operational consumers deduplicate; replay consumers do not

Constraints:
  - Cannot replay events deleted from outbox (>24 months)
  - Replay cannot retroactively alter operational aggregate state
  - Replayed events use original timestamps (not replay time)
  - Replayed events use original correlation_id (traceability)
  - Replay consumers must be explicitly registered; no implicit replay
```

---

## 3. Outbox/Inbox Strategy

### 3.1 Transaction Boundaries

**Critical Rule: Outbox persistence is in same transaction as aggregate mutation**

```csharp
// CORRECT:
using (var tx = _db.BeginTransaction())
{
    // Step 1: Mutate aggregate
    booking.Confirm();
    _db.Bookings.Update(booking);

    // Step 2: Persist events in SAME transaction
    foreach (var evt in booking.DomainEvents)
    {
        var outboxEvent = new OutboxEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = booking.BookingId.Value,
            EventType = evt.GetType().Name,
            Payload = JsonConvert.SerializeObject(evt),
            CreatedAt = DateTime.UtcNow
        };
        _db.OutboxEvents.Add(outboxEvent);
    }

    // Step 3: Commit ALL at once
    await _db.SaveChangesAsync();
    tx.Commit();
}
// If commit fails: neither aggregate NOR event persists (both rolled back)
// If commit succeeds: both persisted atomically
```

### 3.2 Message Loss Prevention

**Scenario: Process crashes after commit but before RabbitMQ publish**

Solution: Outbox polling job resumes on restart

```
Time 0: Aggregate mutated + event inserted in outbox; commit successful
Time 1: Background job publishes event to RabbitMQ
Time 2: CRASH (before outbox marks as published)
Time 3: Restart; background job resumes
        Finds event in PENDING status
        Re-publishes to RabbitMQ
        Marks as PUBLISHED
```

**Result: Zero message loss. Consumers handle duplicates via inbox.**

### 3.3 Event Retention Policy

| Event Type | Retention | Reason |
|-----------|-----------|--------|
| Domain Events | 24 months | Audit trail; replay source of truth |
| Processed Events (Inbox) | 7 days | Deduplication window; disk space |
| Failed Events (Outbox) | 30 days | Ops investigation; manual intervention |
| Processed Notification Events | 3 days | No replay needed; immaterial |

---

## 4. Saga Failure Semantics

### 4.1 Saga Definition

A saga is a **distributed transaction that cannot use database-level ACID**.

Components:
- **Saga Orchestrator** (owns the workflow; decides next step)
- **Saga Steps** (service calls; may fail; must be compensatable)
- **Compensation Actions** (undo a step; also idempotent)

### 4.2 Booking Saga Example

```
Saga: BookingConfirmationSaga

Step 1: CreateBooking (Booking Context)
  Command: Confirm(bookingId)
  Compensation: Escalate(bookingId)
  Timeout: 10s

Step 2: ValidateAvailability (external)
  Command: CheckAvailability(route, date, passengers)
  Compensation: None (read-only)
  Timeout: 5s

Step 3: AllocateResource (ResourceAllocation Context)
  Command: Allocate(bookingId, resourceId)
  Compensation: Release(bookingId, resourceId)
  Timeout: 15s

Step 4: CalculateCosts (CostSplitting Context, if group)
  Command: CalculateShares(groupId, bookingId)
  Compensation: None (eventual consistency; recalculation is idempotent)
  Timeout: 10s

Step 5: SendConfirmation (Notification Context)
  Command: Enqueue(bookingId, "confirmation")
  Compensation: None (best-effort; no undo)
  Timeout: 2s
```

### 4.3 Failure Handling

**Compensation Order: Reverse of Execution**

```
If Step 3 fails (Allocate resource):
  Compensation executes:
    Step 2: None (read-only)
    Step 1: Escalate(bookingId)

  Result: Booking in ESCALATED status (human intervention)
```

**Saga State Persistence:**

```sql
CREATE TABLE shared.saga_execution_log (
    saga_id UUID PRIMARY KEY,
    saga_type VARCHAR(100) NOT NULL,
    aggregate_id VARCHAR(200) NOT NULL,
    correlation_id VARCHAR(100) NOT NULL,
    current_step INT NOT NULL,
    status VARCHAR(50) NOT NULL,          -- RUNNING, COMPENSATING, COMPLETED, FAILED
    steps_executed JSONB NOT NULL,        -- { step_name, output, timestamp }
    compensations_executed JSONB,         -- { step_name, timestamp, success }
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    completed_at TIMESTAMPTZ,
    INDEX(status, updated_at),
    INDEX(aggregate_id)
);
```

### 4.4 Idempotent Compensations

**Critical Rule: Compensation commands must be idempotent**

```csharp
// Compensation for AllocateResource step:
public async Task<void> ReleaseAllocation(string bookingId, string resourceId)
{
    var allocation = await _allocationRepo.GetAsync(bookingId, resourceId);
    
    // If already released: idempotent success
    if (allocation == null || allocation.Status == AllocationStatus.Released)
        return;
    
    // Release and record compensation
    allocation.Release();
    await _allocationRepo.SaveAsync(allocation);
    
    // Log in saga execution: compensation succeeded
}

// If this compensation is called twice (saga retry):
//   First call: allocation released
//   Second call: allocation already null/released; returns success
// Result: Idempotent (safe to retry)
```

### 4.5 Retry Policy (Per Saga, Not Global)

```
Saga Retry Rules:

Step-level timeout: If step exceeds timeout, mark TIMEOUT
  → Retry step (up to 3 times)
  → If 3 retries fail: escalate to compensation

Compensation retry: If compensation fails
  → Retry (up to 5 times, exponential backoff)
  → If all retries fail: mark saga as FAILED (manual ops intervention)

Max Saga Duration: 5 minutes (wall-clock)
  → If saga running > 5 min: escalate regardless of steps
  → Prevents zombie sagas

Compensation: If saga fails at step N
  → Compensation executes for steps [N-1, N-2, ..., 1] (reverse order)
  → Each compensation is idempotent and retried independently
```

### 4.6 Saga Ownership and Authority

**For every saga, exactly one context is the owner:**

```
BookingConfirmationSaga
  Owner: BookingContext
  Authority: Booking aggregate decides if saga succeeds or escalates
  If saga fails: BookingContext records failure in booking state

GroupCostSplittingSaga
  Owner: CostSplittingContext
  Authority: CostLedger decides final settlement
  If saga fails: CostSplittingContext escalates; no refunds processed

PilgrimageSaga
  Owner: PilgrimageContext
  Authority: PilgrimageGroup decides if pilgrimage is viable
  If saga fails: PilgrimageContext returns escalation to manager
```

---

## 5. Concurrency Resolution Rules

### 5.1 Concurrency Levels

| Level | Scope | Mechanism | Conflict Handling |
|-------|-------|-----------|-------------------|
| **Optimistic** | Aggregate-level | Version field (`xmin`/`RowVersion`) | First committer wins; stale writer rejected and must reload |
| **Pessimistic** | Resource-level | Exclusive locks | First-writer wins |
| **Eventual** | Cross-aggregate | Idempotent sagas | Saga decides |

### 5.2 Optimistic Locking (Per Aggregate)

**Used for: Single aggregate mutations (Booking, Allocation, CostLedger)**

```csharp
public class Booking
{
    public long Version { get; set; }  // Incremented on every mutation
}

// Mutation attempt:
public async Task ConfirmAsync(string bookingId, long expectedVersion)
{
    var booking = await _repo.GetAsync(bookingId);
    
    if (booking.Version != expectedVersion)
        throw new ConcurrencyException(
            $"Booking version mismatch. Expected {expectedVersion}, got {booking.Version}");
    
    booking.Confirm();
    booking.Version++;  // Increment version
    
    await _repo.SaveAsync(booking);
    // Update includes: WHERE Version = @expectedVersion
    // If version changed between read and write: UPDATE returns 0 rows
    // Throw ConcurrencyException (handled by caller)
}

// Conflict Resolution:
// 1. Client receives ConcurrencyException
// 2. Client reloads booking (gets new version)
// 3. Re-evaluates command against current state
// 4. Retries mutation with new version
// 5. If still invalid: throw domain exception (not concurrency issue)
```

### 5.3 Pessimistic Locking (For High-Contention Resources)

**Used for: Allocation (multiple bookings competing for same resource)**

```sql
-- Resource allocation scenario:
-- Booking A and Booking B both trying to allocate same bus simultaneously

-- Connection 1 (Booking A):
BEGIN TRANSACTION;
SELECT * FROM resources WHERE resource_id = '12345' FOR UPDATE;  -- Exclusive lock
  -- Booking A gets lock; Booking B blocked

-- Check availability:
IF available:
  UPDATE allocations SET resource_id = '12345' WHERE booking_id = 'A';
  COMMIT;
  -- Lock released; Booking B can now proceed

-- Connection 2 (Booking B):
BEGIN TRANSACTION;
SELECT * FROM resources WHERE resource_id = '12345' FOR UPDATE;  -- Waits here
  -- Once Booking A commits and releases lock, Booking B acquires lock
  
-- Check availability (now different):
IF NOT available:  -- Changed by Booking A
  ROLLBACK;
  Throw ResourceUnavailableException;
  -- Return to operator; offer alternatives
```

### 5.4 Eventual Consistency (Cross-Aggregate)

**Used for: Saga workflows (Booking → Allocation → CostSplitting → Notification)**

```
Strong Consistency Point: Booking aggregate confirms itself
  ✓ Booking in CONFIRMED status (strong)
  ✓ All invariants satisfied

Eventual Consistency: Downstream sagas
  ? Allocation may be PENDING
  ? Cost may not yet be calculated
  ? Notification may not yet sent
  
  But: Saga will eventually resolve all
  If saga fails: Compensation ensures Booking reverts

Result: Per-aggregate strong consistency; cross-aggregate eventual consistency
        Sagas enforce distributed invariants over time
```

### 5.5 Write-Write Conflict Resolution

**Scenario: Two users try to amend same booking simultaneously**

```
User A: AmendBooking(bookingId, newRoute, newPrice, version=5)
User B: AmendBooking(bookingId, differentRoute, version=5)

Both readers get version=5
User A commits first: version becomes 6
User B tries to commit: WHERE version=5 fails (version now 6)

Result:
  User B: ConcurrencyException
  → Reload booking (version 6)
  → Re-evaluate: Can B's new route still apply to current state?
  → If yes: retry with version=6
  → If no: throw InvalidBookingAmendmentException
```

### 5.6 Stale Read Handling

**Scenario: Reader loads booking; data changes; reader makes decision**

```
Time 1: Operator loads Booking (status=CONFIRMED, version=5)
Time 2: Saga starts allocation (booking is being allocated)
Time 3: Operator tries to AmendBooking with old data (version=5)

AmendBooking guard checks:
  if Status != CONFIRMED || version != 5:
    throw BookingStateChangedException

Result: Operator sees error; reloads booking; sees true state; tries again

This is CORRECT behavior:
  Stale commands are rejected, not silently lost
```

---

## 6. Clock Authority

### 6.1 Temporal Rules

**Rule 1: All timestamps stored in UTC**

```sql
-- Booking table:
created_at TIMESTAMPTZ DEFAULT (NOW() AT TIME ZONE 'UTC')
departure_time TIMESTAMPTZ NOT NULL              -- UTC
completion_deadline TIMESTAMPTZ NOT NULL         -- UTC

-- Never store local time in database
-- Conversion to user locale happens at presentation layer
```

**Rule 2: Authoritative clock is database server**

```csharp
// NOT:
booking.CreatedAt = DateTime.UtcNow;  // Client clock (risky)

// CORRECT:
// Clock is assigned by database trigger or application-inserted:
INSERT INTO bookings (..., created_at)
VALUES (..., NOW());  -- Database clock

// Rationale:
// - Database clock is authoritative
// - Prevents time-travel attacks
// - Prevents client clock skew issues
```

**Rule 3: Clock tolerance windows**

```
Prayer Schedule Compliance:
  Tolerance: ±5 minutes
  → Booking can depart up to 5 min before scheduled prayer time
  → Booking can arrive up to 5 min after prayer time
  Rationale: Account for travel delays, prayer duration variance

Escalation Timeouts:
  Tolerance: ±10 seconds
  → Escalation triggers at 30min ± 10sec
  Rationale: Scheduler imprecision

Refund Window Calculation:
  Tolerance: Exact (no grace period)
  → Refund percentage based on precise hours remaining
  Rationale: No ambiguity in financial rules
```

### 6.2 Temporal Invariant Enforcement

**Clock Abstraction: IClock**

All temporal invariant checks use an injected `IClock` abstraction — never raw `DateTime.UtcNow`. This enables:
- Operational mode: clock synchronized with database time semantics
- Replay mode: clock fixed at original event timestamp
- Test mode: deterministic clock (no real-time dependency)

```csharp
// Clock abstraction (in UTOP.Shared)
public interface IClock
{
    DateTime UtcNow { get; }
}

// Operational implementation
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    // Note: for critical financial and scheduling checks,
    // time is fetched from DB via SELECT NOW() to eliminate app/DB skew
}

// Replay implementation
public class FixedClock : IClock
{
    private readonly DateTime _fixedTime;
    public FixedClock(DateTime fixedTime) => _fixedTime = fixedTime;
    public DateTime UtcNow => _fixedTime;
}

// Test implementation
public class StubClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
    public void AdvanceBy(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}
```

**Usage in domain (IClock injected via domain service, not directly into aggregate):**

```csharp
// Temporal invariant: Booking must depart in future
public static Booking Create(JourneyRoute route, DateRange dates, IClock clock, ...)
{
    if (dates.Start <= clock.UtcNow)
        throw new DepartureInPastException(dates.Start);
}

// Temporal invariant: Amendment window (2 hours before departure)
public void Amend(JourneyRoute newRoute, IClock clock, ...)
{
    var hoursToStart = (Itinerary.DepartureTime - clock.UtcNow).TotalHours;
    if (hoursToStart < 2)
        throw new AmendmentWindowExpiredException(hoursToStart);
}
```

**Clock Authority Per Mode:**

| Mode | Clock Implementation | Authority Source |
|------|---------------------|-----------------|
| Operational | SystemClock | Database `NOW()` for critical checks; app UTC otherwise |
| Replay | FixedClock (original timestamp) | Original event timestamp |
| Testing | StubClock | Deterministic; controlled by test |

### 6.3 Replay Time Semantics

**For disaster recovery or analytics replays:**

```
Scenario: Replaying events from 6 months ago

Rule 1: Use original event timestamp (not replay time)
  → Original event has timestamp T (6 months ago)
  → Replayed event uses same timestamp T
  → Temporal invariants evaluated against T, not now

Rule 2: Skip time-dependent checks during replay
  → Departure in future check: SKIPPED (was future at T)
  → Amendment window check: SKIPPED (was open at T)
  → Refund window check: Use T (not now)

Rationale:
  Replay must be idempotent
  Using "now" would make replayed events fail temporal checks
  Original temporal context preserved via original timestamp
```

### 6.4 Scheduler Clock Skew

**For background jobs (outbox publishing, escalation checks, retry scheduling):**

```csharp
// Scheduler runs on app server
// Database clock may differ from app server clock

// SAFE:
SELECT * FROM outbox_events
WHERE status = 'PENDING'
  AND created_at < NOW() - INTERVAL '5 minutes'  -- Database time
  AND publish_retry_count < 5

// UNSAFE:
var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);  // App time
SELECT * FROM outbox_events
WHERE created_at < @fiveMinutesAgo               -- Time mismatch possible

// Rationale:
// All time comparisons must use database NOW() to avoid skew

// If app server and database server clocks diverge:
// Safe approach: Database is authoritative
```

---

## 7. Consistency Guarantees Summary

### 7.1 Per-Aggregate Consistency (STRONG)

Within a single aggregate: **ACID-compliant, strongly consistent**

```
Booking aggregate:
  ✓ All invariants satisfied before commit
  ✓ Either all state changes persist or none
  ✓ Concurrent mutations serialized (optimistic lock)
  ✓ No partial updates visible
```

### 7.2 Cross-Aggregate Consistency (EVENTUAL)

Across bounded contexts: **Consistency achieved through sagas over time**

```
Booking → Allocation → CostSplitting → Notification

At T=0: Booking CONFIRMED (strong)
At T=100ms: Allocation PENDING (eventual)
At T=200ms: Allocation CONFIRMED (strong for allocation)
At T=300ms: CostShare CALCULATED (strong for cost)
At T=400ms: Notification SENT (best-effort)

Invariant: "If booking confirmed, then cost must eventually be calculated"
  Enforced by: CostSplittingContext saga
  Timeout: 30 seconds max
  Failure: Escalate to ops
```

### 7.3 Failure Modes and Recovery

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Booking mutation fails | Aggregate throws exception | Client retries or handles error |
| Allocation saga step fails | Saga orchestrator catches | Compensation executes; booking escalates |
| Notification send fails | Outbox retry counter | Exponential backoff; eventually notifies or fails permanently |
| Cross-aggregate race | Optimistic lock / saga idempotency | Conflict detected; saga compensates or retries |

---

## 8. Implementation Checklist

Before any distributed operation:

- [ ] Command has idempotency key defined
- [ ] Aggregate has version field
- [ ] Outbox events inserted in same transaction as aggregate mutation
- [ ] Inbox deduplication implemented for event consumer
- [ ] Saga ownership assigned (one context per saga)
- [ ] Compensation action idempotent and tested
- [ ] Clock source is database (not client)
- [ ] Retry policy defined for each saga step
- [ ] Timeout defined for each saga step
- [ ] Concurrent mutation test written (optimistic lock / pessimistic lock)
- [ ] Stale command test written
- [ ] Replay safety test written (command is idempotent)
- [ ] All domain exceptions are specific (not generic)

---

**End of Distributed Consistency & Concurrency Semantics**

**Status:** LOCKED — Critical Pre-LLD Artifact Complete

**Next Stabilization Artifacts:**
1. Event Contract Governance
2. Context Ownership Matrix
3. Temporal Semantics
4. Shared Kernel Governance
