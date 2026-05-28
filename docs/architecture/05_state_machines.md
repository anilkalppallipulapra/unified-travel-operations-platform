# State Machine Definitions
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0
**Status:** LOCKED
**Phase:** Phase 3 — Architectural Stabilization (Pre-LLD)
**Classification:** Project Internal — Binding State Transition Contract

---

## Purpose

This document formalizes all state transitions for critical aggregates. Every transition is:
- **Explicitly allowed** (documented) or **explicitly forbidden** (rejected)
- **Triggered by** a command or event
- **Guarded by** invariants
- **Emits** a domain event or none
- **Compensatable** if in saga context

No implicit transitions. All operational transitions documented.

---

## 1. Booking State Machine

### 1.1 State Diagram

```
        ┌─────────┐
        │  DRAFT  │
        └────┬────┘
             │
    CreateBooking (command)
             │
             ▼
    ┌─────────────────────┐
    │ PENDING_VALIDATION  │
    └──────────┬──────────┘
               │
    ValidateAvailability (command) ── or ── external availability check
               │
        ┌──────┴──────┐
        │             │
        ▼             ▼
    CONFIRMED      ESCALATED
        │
    BookingConfirmed (event)
        │
        ▼
    ┌──────────────┐
    │  ALLOCATED   │
    └────┬─────────┘
         │
    ResourceAllocated (event)
         │
         ▼
    ┌──────────────┐
    │  IN_TRANSIT  │
    └────┬─────────┘
         │
    DepartureCompleted (event)
         │
         ▼
    ┌──────────────┐
    │  COMPLETED   │ ◄── Terminal
    └──────────────┘

CANCEL PATH (from any non-terminal state):
    └─── CancelBooking (command) ──► CANCELLED ──► Compensations (event)
                                        │
                                        ▼
                                    REFUNDED ◄── Terminal
```

### 1.2 Transition Table

| Current State | Command/Event | Guard Conditions | New State | Event Emitted | Idempotency |
|---|---|---|---|---|---|
| DRAFT | CreateBooking | OperatorId valid; TravelMode valid; Price > 0; Departure > now | PENDING_VALIDATION | BookingCreated | Idempotency key: (OperatorId, Mode, Route, Date) |
| PENDING_VALIDATION | ValidateAvailability | Availability confirmed | CONFIRMED | BookingConfirmed | Key: BookingId |
| PENDING_VALIDATION | ValidateAvailability | Availability failed | ESCALATED | BookingEscalated | Key: BookingId |
| CONFIRMED | AmendBooking | 2h before departure; valid new price | CONFIRMED | BookingAmended | Key: (BookingId, amendment_version) |
| CONFIRMED | AllocateResource | Resource available; capacity sufficient | ALLOCATED | ResourceAllocated | Key: (BookingId, ResourceId) |
| ALLOCATED | CompleteDeparture | Departure time reached; passengers checked in | IN_TRANSIT | DepartureStarted | Key: BookingId |
| IN_TRANSIT | CompleteArrival | Arrival time reached | COMPLETED | BookingCompleted | Key: BookingId |
| DRAFT, PENDING_VALIDATION, CONFIRMED, ALLOCATED | CancelBooking | Departure not passed | CANCELLED | BookingCancelled | Key: BookingId |
| CANCELLED | ProcessRefund | Refund policy evaluated | REFUNDED | RefundProcessed | Key: (BookingId, refund_request_id) |
| ESCALATED | (manager action) | Manager confirms or rejects | CONFIRMED or CANCELLED | EscalationResolved | Key: (BookingId, manager_id, timestamp) |

### 1.3 Forbidden Transitions

```
DRAFT       ──X── ALLOCATED (must go through CONFIRMED)
DRAFT       ──X── IN_TRANSIT
DRAFT       ──X── COMPLETED
COMPLETED   ──X── any mutation
CANCELLED   ──X── CONFIRMED (no resurrection)
CANCELLED   ──X── ALLOCATED
IN_TRANSIT  ──X── DRAFT
IN_TRANSIT  ──X── CANCELLED (arrival passed)
```

### 1.4 Duplicate Command Handling

**CreateBooking (idempotent):**
```
If booking with (OperatorId, Mode, Route, Date, Passengers) exists:
  Return existing booking; do not create duplicate
Else:
  Create new booking
Idempotency check via: database unique constraint or idempotency key table
```

**AmendBooking (idempotent):**
```
If amendment already applied (same route, price, version):
  Return success; do not re-apply
Else:
  Apply amendment; increment amendment_version
```

**CancelBooking (idempotent):**
```
If already CANCELLED or REFUNDED:
  Return success; idempotent
If COMPLETED:
  Throw CannotCancelCompletedBookingException
```

### 1.5 Stale Command Handling

**AmendBooking when 2h window expired:**
```
Throw BookingAmendmentWindowExpiredException
No state change
```

**CancelBooking after departure passed:**
```
Throw BookingDepartureAlreadyPassedException
No state change
```

### 1.6 Replayed Event Handling

**BookingConfirmed (replayed):**
```
If already in CONFIRMED or later state:
  Idempotency key check: if event_id already processed, skip
  Do not re-emit or double-confirm
If in PENDING_VALIDATION:
  Apply confirmation
```

### 1.7 Compensation Transitions

**If saga fails after CONFIRMED:**
```
BookingConfirmed ──► (saga fails at step 4) ──► EscalateSaga (command)
                                                   │
                                                   ▼
                                                ESCALATED
                                                (human resolution required)
```

**If saga fails during allocation:**
```
ALLOCATED ──► (saga fails) ──► ReleaseAllocation (command)
                                  │
                                  ▼
                             CONFIRMED
                          (return to pre-allocated)
```

---

## 2. AllocationDecision State Machine

### 2.1 State Diagram

```
        ┌──────────┐
        │ PENDING  │
        └────┬─────┘
             │
    ResourceAvailable? (check)
        │
    ┌───┴────┐
    │        │
    ▼        ▼
CONFIRMED  ESCALATED ◄── conflict detected
    │          │
    │     (manager decision)
    │     ┌────┴────┐
    │     │        │
    │     ▼        ▼
    │  CONFIRMED  CANCELLED
    │     │
    └─────┘
        │
    ResourceAllocated (event)
        │
        ▼
    ┌─────────────────┐
    │ RELEASED        │ ◄── Terminal
    │ (by saga/cancel)│
    └─────────────────┘
```

### 2.2 Transition Table

| Current State | Command/Event | Guard Conditions | New State | Event Emitted | Idempotency |
|---|---|---|---|---|---|
| PENDING | CheckAvailability | Resource active; capacity ≥ passengers | CONFIRMED | ResourceAllocated | Key: (BookingId, ResourceId) |
| PENDING | CheckAvailability | Resource unavailable OR capacity < passengers OR conflict | ESCALATED | ResourceConflictDetected | Key: (BookingId, ResourceId, date) |
| ESCALATED | ManagerOverride | New resource valid; manager approved | CONFIRMED | AllocationOverridden | Key: (BookingId, ManagerId, timestamp) |
| ESCALATED | ManagerReject | Manager declined all options | CANCELLED | AllocationRejected | Key: (BookingId, ManagerId) |
| CONFIRMED | ReleaseAllocation | Booking cancelled OR departure passed | RELEASED | ResourceReleased | Key: (BookingId, ResourceId) |
| RELEASED | (terminal) | — | — | — | — |

### 2.3 Forbidden Transitions

```
PENDING     ──X── RELEASED (must go through CONFIRMED)
CONFIRMED   ──X── ESCALATED (escalation requires fresh attempt)
RELEASED    ──X── any mutation (terminal)
```

### 2.4 Idempotency

**CheckAvailability (idempotent):**
```
If AllocationDecision exists for (BookingId, ResourceId, DateRange):
  If already CONFIRMED: return existing decision
  If already ESCALATED: return escalation (do not re-check)
  If RELEASED: create new decision (re-allocation)
Else:
  Create new decision
```

**ManagerOverride (idempotent):**
```
If override already applied (same BookingId, same ManagerId, same timestamp):
  Return success; do not re-override
Else:
  Apply override; record timestamp
```

### 2.5 Timeout Handling

**ESCALATED for > 30 minutes:**
```
Scheduled job runs every 5 minutes
If AllocationDecision.Status == ESCALATED 
   AND (UtcNow - CreatedAt) > 30 minutes:
  Send escalation reminder to manager
  If still unresolved after 60 minutes:
    Auto-cancel (CANCELLED)
    Trigger re-allocation saga
```

---

## 3. CostLedger State Machine

### 3.1 State Diagram

```
        ┌────────┐
        │ DRAFT  │
        └───┬────┘
            │
    CalculateShares (command)
            │
            ▼
    ┌──────────────┐
    │   ACTIVE     │
    └───┬────┬─────┘
        │    │
        │    └─── Recalculate (member join/leave)
        │         └─ stay ACTIVE
        │
        ▼
    ┌──────────────┐
    │  SETTLED     │ ◄── Terminal
    └──────────────┘

DISPUTE PATH:
    ACTIVE ──► DisputeShare (command) ──► DISPUTED ──► ResolveDispute (mgr) ──► ACTIVE or SETTLED
```

### 3.2 Transition Table

| Current State | Command/Event | Guard Conditions | New State | Event Emitted | Idempotency |
|---|---|---|---|---|---|
| DRAFT | CalculateShares | GroupId valid; MemberCount ≥ 1; TotalCost > 0 | ACTIVE | CostShareCalculated | Key: (GroupId, CalculationVersion) |
| ACTIVE | RecalculateShares | Member count changed (join/leave) | ACTIVE | CostShareRecalculated | Key: (GroupId, timestamp, member_change_id) |
| ACTIVE | SettleLedger | All payments collected OR manager override | SETTLED | CostSettlementComplete | Key: (GroupId, settlement_timestamp) |
| ACTIVE | DisputeShare | Member initiates dispute | DISPUTED | CostShareDisputed | Key: (GroupId, MemberId, dispute_timestamp) |
| DISPUTED | ResolveDispute (manager) | Manager confirms OR adjusts | ACTIVE or SETTLED | DisputeResolved | Key: (GroupId, MemberId, manager_id) |

### 3.3 Forbidden Transitions

```
SETTLED ──X── any mutation (immutable)
SETTLED ──X── DISPUTED (no new disputes after settlement)
DRAFT   ──X── SETTLED (must go through ACTIVE)
```

### 3.4 Idempotency

**RecalculateShares (idempotent):**
```
If recalculation already applied for this member_change_id:
  Return success; do not recalculate twice
Else:
  Recalculate; increment version
Store member_change_id to detect duplicates
```

---

## 4. PilgrimageGroup State Machine

### 4.1 State Diagram

```
        ┌──────────┐
        │ PLANNING │
        └────┬─────┘
             │
    AssignGuide (command)
             │
             ▼
    ┌──────────────────┐
    │ GUIDE_ASSIGNED   │
    └────┬─────────────┘
         │
    RunComplianceCheck (command) ──passed?──┐
         │                                   │
         └──failed──► ESCALATED ◄────────────┘
                        │
                    (manager action)
                        │
                        ▼
                   PLANNING (retry)
                   OR CANCELLED

    [If compliance passed]
         │
         ▼
    ┌──────────────┐
    │ COMPLIANT    │
    └────┬─────────┘
         │
    BookAllLegs (command)
         │
         ▼
    ┌──────────────┐
    │   BOOKED     │
    └────┬─────────┘
         │
    DepartureDay (event)
         │
         ▼
    ┌──────────────┐
    │ IN_PROGRESS  │
    └────┬─────────┘
         │
    ArrivalCompleted (event)
         │
         ▼
    ┌──────────────┐
    │ COMPLETED    │ ◄── Terminal
    └──────────────┘
```

### 4.2 Transition Table

| Current State | Command/Event | Guard Conditions | New State | Event Emitted | Idempotency |
|---|---|---|---|---|---|
| PLANNING | AssignGuide | Guide qualified; dates available; certified | GUIDE_ASSIGNED | GuideAssigned | Key: (PilgrimageId, GuideId) |
| GUIDE_ASSIGNED | RunComplianceCheck | All checks passed | COMPLIANT | ComplianceCheckPassed | Key: (PilgrimageId, check_timestamp) |
| GUIDE_ASSIGNED | RunComplianceCheck | Any violation | ESCALATED | ComplianceCheckFailed | Key: (PilgrimageId, check_timestamp) |
| ESCALATED | ManagerResolveCompliance | Manager approves/rejects | COMPLIANT or CANCELLED | EscalationResolved | Key: (PilgrimageId, ManagerId) |
| COMPLIANT | BookAllLegs | All legs booked; accommodations confirmed | BOOKED | PilgrimageFullyBooked | Key: (PilgrimageId, booking_version) |
| BOOKED | DepartureCrossed (event) | Departure time reached | IN_PROGRESS | PilgrimageStarted | Key: PilgrimageId |
| IN_PROGRESS | ArrivalCrossed (event) | Final destination reached | COMPLETED | PilgrimageCompleted | Key: PilgrimageId |

### 4.3 Forbidden Transitions

```
PLANNING    ──X── BOOKED (must go through COMPLIANT)
PLANNING    ──X── IN_PROGRESS
COMPLETED   ──X── any mutation
ESCALATED   ──X── BOOKED (must resolve first)
```

### 4.4 Timeout Handling

**ESCALATED for > 7 days:**
```
Scheduled job: if unresolved after 7 days, auto-cancel
Trigger refund sagas for all pilgrim bookings
```

---

## 5. Group State Machine

### 5.1 State Diagram

```
        ┌─────────┐
        │ FORMING │
        └────┬────┘
             │
    AddMember+ (commands)
    InviteMembers (events)
             │
             ▼
    ┌──────────────┐
    │   BOOKING    │
    └────┬─────────┘
         │
    BookAllLegs (command)
         │
         ▼
    ┌──────────────┐
    │  CONFIRMED   │
    └────┬─────────┘
         │
    DepartureCrossed (event)
         │
         ▼
    ┌──────────────┐
    │ IN_PROGRESS  │
    └────┬─────────┘
         │
    ArrivalCrossed (event)
         │
         ▼
    ┌──────────────┐
    │ COMPLETED    │ ◄── Terminal
    └──────────────┘

CANCEL PATH:
    FORMING, BOOKING, CONFIRMED ──► CancelGroup (command) ──► CANCELLED ◄── Terminal
```

### 5.2 Transition Table

| Current State | Command/Event | Guard Conditions | New State | Event Emitted | Idempotency |
|---|---|---|---|---|---|
| FORMING | AddMember | Member not duplicate; valid UserId; not cancelled | FORMING | GroupMemberJoined | Key: (GroupId, UserId) |
| FORMING | StartBooking | ≥2 members; coordinator exists | BOOKING | GroupBookingStarted | Key: GroupId |
| BOOKING | ConfirmAllBookings | All member bookings CONFIRMED | CONFIRMED | GroupConfirmed | Key: (GroupId, confirmation_timestamp) |
| CONFIRMED | DepartureCrossed (event) | Departure time reached | IN_PROGRESS | GroupStarted | Key: GroupId |
| IN_PROGRESS | ArrivalCrossed (event) | Arrival time reached | COMPLETED | GroupCompleted | Key: GroupId |
| FORMING, BOOKING, CONFIRMED | CancelGroup | Departure not passed | CANCELLED | GroupCancelled | Key: (GroupId, cancellation_timestamp) |

### 5.3 Member Lifecycle (sub-state within Group)

```
INVITED ──► JOINED ──► (ACTIVE or LEFT or REMOVED)
```

| Member Status | Trigger | Next Status | Notes |
|---|---|---|---|
| INVITED | MemberAccepts | ACTIVE | Member confirmed attendance |
| ACTIVE | MemberLeaves | LEFT | Trigger cost recalculation; process refund |
| ACTIVE | CoordinatorRemoves | REMOVED | Force removal; trigger refund |
| LEFT | — | — | Terminal per member |
| REMOVED | — | — | Terminal per member |

---

## 6. Notification State Machine

### 6.1 State Diagram

```
        ┌─────────┐
        │ PENDING │
        └────┬────┘
             │
    AttemptSend (scheduled job)
        ┌────┴─────┐
        │          │
        ▼          ▼
      SENT     RETRY_PENDING
                   │
            (wait exponential backoff)
                   │
            AttemptSend again
        ┌───────────┴──────────┐
        │                      │
        ▼                      ▼
      SENT            PERMANENTLY_FAILED ◄── Terminal
```

### 6.2 Transition Table

| Current State | Command/Event | Guard Conditions | New State | Event Emitted | Idempotency |
|---|---|---|---|---|---|
| PENDING | AttemptSend | Channel available; template rendered; no PII exposed | SENT | NotificationSent | Key: NotificationId |
| PENDING | AttemptSend | Channel failure; RetryCount < MaxRetries | RETRY_PENDING | NotificationRetryScheduled | Key: (NotificationId, attempt#) |
| RETRY_PENDING | AttemptSend (after backoff) | RetryCount < MaxRetries | SENT or RETRY_PENDING | NotificationSent or NotificationRetryScheduled | Key: (NotificationId, attempt#) |
| RETRY_PENDING | AttemptSend | RetryCount ≥ MaxRetries | PERMANENTLY_FAILED | NotificationFailed | Key: NotificationId |
| PENDING, RETRY_PENDING | CancelNotification | Manual cancellation | CANCELLED | NotificationCancelled | Key: NotificationId |

### 6.3 Retry Backoff

```
Attempt 1: Immediate
Attempt 2: +1 minute
Attempt 3: +5 minutes
Attempt 4: +15 minutes
Attempt 5: PERMANENTLY_FAILED

Total retry window: ~21 minutes
```

### 6.4 Idempotency

**AttemptSend (idempotent):**
```
If notification already SENT:
  Return success; do not re-send
If PERMANENTLY_FAILED:
  Return error; cannot retry
Else:
  Attempt send; increment RetryCount
Store NotificationId + timestamp to detect duplicate sends
```

---

## State Machine Cross-References

### Orchestration Dependencies

```
Booking.CONFIRMED ──emit──► BookingConfirmed
   │
   └──consumed by──► ResourceAllocation.CheckAvailability
                       │
                       └──emit──► ResourceAllocated
                          │
                          └──consumed by──► CostSplitting.RecalculateShares (if Group)
                                  │
                                  └──emit──► CostShareRecalculated
                                     │
                                     └──consumed by──► Notifications.EnqueueCostBreakdown
```

### Compensation Chain

```
Saga Step 1: Booking.CONFIRMED ✓
Saga Step 2: ResourceAllocation.CONFIRMED ✓
Saga Step 3: CostLedger.ACTIVE ✗ (fails)

Compensation:
  Step 2: ResourceAllocation.ReleaseAllocation ──► RELEASED
  Step 1: Booking.EscalateSaga ──► ESCALATED
```

---

## Implementation Rules
State transitions are authoritative domain behavior. APIs, workflows, schedulers, integrations, batch processes, and UI flows must conform to these contracts and may not bypass aggregate transition rules directly or indirectly.
### Rule 1: Guard Clauses First

```csharp
// WRONG: mutate, then check
public void Confirm() {
    Status = BookingStatus.Confirmed;
    if (Status == BookingStatus.Draft) throw Exception();
}

// CORRECT: check, then mutate
public void Confirm() {
    if (Status != BookingStatus.PendingValidation)
        throw InvalidBookingStateTransitionException();
    Status = BookingStatus.Confirmed;
    AddDomainEvent(new BookingConfirmed(...));
}
```

### Rule 2: Every Transition Throws Specific Exception

```csharp
public void Cancel() {
    if (Status == BookingStatus.Completed)
        throw new CannotCancelCompletedBookingException(BookingId.Value);
    if (Status == BookingStatus.Cancelled)
        throw new BookingAlreadyCancelledException(BookingId.Value);
    if (Itinerary.DepartureTime <= DateTime.UtcNow)
        throw new BookingDepartureAlreadyPassedException(BookingId.Value);
    // ... all guards
    
    Status = BookingStatus.Cancelled;
    // ...
}
```

### Rule 3: Idempotency Keys Mandatory for Write Commands

```csharp
// Every write command must pass IdempotencyKey
public async Task<BookingId> CreateBookingAsync(
    CreateBookingCommand cmd,
    string idempotencyKey)  // Required parameter
{
    var existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey);
    if (existing != null) return existing.BookingId;
    
    var booking = Booking.Create(...);
    await _repository.SaveAsync(booking, idempotencyKey);
    return booking.BookingId;
}
```

---

## Test Coverage Requirements

**Per state machine:**
- ✓ Legal transition test
- ✓ Forbidden transition test (throws correct exception)
- ✓ Guard condition boundary test
- ✓ Duplicate command idempotency test
- ✓ Stale command rejection test
- ✓ Replayed event handling test
- ✓ Timeout/escalation test (if applicable)
- ✓ Compensation transition test (if saga-involved)

---

## Appendix A: Aggregate Decomposition Candidates

### Current Architectural Pressure

Booking aggregate currently owns:
- Lifecycle (Draft → Confirmed → Completed)
- Pricing and financial state
- Itinerary and route management
- Passenger manifest
- Validation orchestration
- Allocation coordination
- Cancellation and refunds
- Pilgrimage/group association
- Transit progression
- Compensation escalation

Assessment: **Pressure visible but manageable.** Cohesion remains tight because:
- Lifecycle transitions are transactionally coupled
- Many invariants are still local (not distributed)
- Compensation semantics remain centered on booking state

### Potential Decomposition Axes (Future Only)

Do NOT split immediately. Future candidates if characteristics diverge:

1. **BookingLifecycleAggregate** — Core lifecycle only (Draft → Confirmed → Completed)
2. **BookingFinancialAggregate** — Pricing, refunds, ledger interactions, revenue recognition
3. **BookingFulfillmentAggregate** — Allocation, transit state, manifest, seat assignment
4. **BookingJourneyAggregate** — Itinerary, passengers, route details, stops

### Decomposition Trigger Conditions

Decompose ONLY when one or more becomes true:

| Trigger | Signal |
|---------|--------|
| Transactional boundaries diverge | Different transaction boundaries across aggregate operations |
| State machine cadence diverges | Some states update frequently; others rarely |
| Scaling characteristics diverge | One subset experiences orders-of-magnitude higher load |
| Ownership diverges | Different teams or contexts need independent authority |
| Invariant distribution emerges | Critical invariants become distributed (not local) |
| Compensation difficulty increases | Sagas become unmanageably complex with centralized aggregate |

### Decomposition Governance Constraints

If decomposition occurs in future phases, apply these non-negotiable rules:

**Authority Rules:**
- Exactly ONE aggregate remains **lifecycle authority**
- Financial state must NEVER become authoritative for booking lifecycle
- Fulfillment state must NOT mutate pricing state directly
- Cross-aggregate synchronization occurs ONLY through domain events

**Consistency Rules:**
- Every existing invariant must be reclassified as:
  - **Local Invariant** (single aggregate, strong consistency required)
  - **Distributed Invariant** (multiple aggregates, saga-enforced)
  - **Eventual Consistency Rule** (no strict synchronization required)
- No decomposition may introduce distributed transactions
- All cross-aggregate updates via saga compensation only

**Orchestration Rules:**
- Saga ownership must be explicitly reassigned (to which context?)
- Compensation must remain idempotent and retry-safe
- Escalation authority must be unambiguous
- No aggregate may have implicit authority over another's state

**Integration Rules:**
- Event contracts must freeze before decomposition
- Existing API surfaces must remain backward-compatible
- No breaking changes to event schema without migration strategy
- Replay semantics must remain consistent

**Testing Rules:**
- All distributed invariants must have explicit saga tests
- All compensation paths must have explicit failure tests
- Concurrency tests must verify no write-write races

### Monitoring Strategy Until Decomposition

Track these metrics to detect decomposition pressure:

| Metric | Threshold | Action |
|--------|-----------|--------|
| Booking aggregate transaction time | > 500ms p99 | Profile; consider split |
| Concurrent mutation conflicts | > 5% retry rate | Review concurrency model |
| Compensation failure rate | > 1% of bookings | Review saga design |
| Saga orchestration steps | > 10 steps per flow | Consider sub-saga or context split |
| Single aggregate DB table size | > 1GB | Consider archival or split |

---

**End of State Machine Definitions**

**Status:** LOCKED — Ready for next stabilization artifact

