# Event Contract Governance
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0
**Status:** BASELINE LOCKED
**Phase:** Phase 3 — Architectural Stabilization (Pre-LLD)
**Classification:** Project Internal — Binding Event Contract

---

## Purpose

This document governs all domain events in UTOP. It defines:
- What an event is (and is not)
- Canonical structure every event must conform to
- Versioning and backward compatibility rules
- Ownership (one publisher, many consumers)
- PII handling per event
- Retention and replay eligibility
- Integration event vs domain event distinction

Without this governance, event-driven architecture degrades into messaging chaos: schema drift, undocumented coupling, silent PII leaks, and replay corruption.

---

## 1. Event Classification

### 1.1 Two Event Categories

| Category | Definition | Scope | Published To |
|----------|-----------|-------|-------------|
| **Domain Event** | Significant state change within a bounded context | Internal to context first | RabbitMQ (after outbox persistence) |
| **Integration Event** | Cross-context notification derived from a domain event | Crosses context boundaries | RabbitMQ (consumed by other contexts) |

### 1.2 The Distinction Matters

```
Domain Event:
  Booking.Confirm() fires BookingConfirmed
  → BookingConfirmed is INTERNAL to Booking context first
  → Handler persists it to outbox
  → Outbox publisher promotes it to RabbitMQ as Integration Event

Integration Event:
  BookingConfirmedIntegrationEvent published to RabbitMQ
  → ResourceAllocationContext consumes it
  → NotificationContext consumes it
  → AnalyticsContext consumes it
```

**Why this separation:**
- Domain events carry rich internal domain types (aggregates, value objects)
- Integration events carry only primitive/serializable data (no domain types)
- Integration events are the external contract; domain events are internal implementation
- Domain events may change without affecting consumers; integration event schema changes are breaking

### 1.3 Naming Convention

```
Domain Event:      [Action in past tense]
                   BookingConfirmed
                   ResourceAllocated
                   CostShareCalculated

Integration Event: [Domain Event name] + "IntegrationEvent"
                   BookingConfirmedIntegrationEvent
                   ResourceAllocatedIntegrationEvent
                   CostShareCalculatedIntegrationEvent
```

---

## 2. Canonical Event Envelope

### 2.1 Every Integration Event Must Conform

```json
{
  "envelope": {
    "event_id": "550e8400-e29b-41d4-a716-446655440000",
    "event_type": "BookingConfirmedIntegrationEvent",
    "event_version": "1.0",
    "schema_version": "1.0",
    "source_context": "BookingContext",
    "aggregate_type": "Booking",
    "aggregate_id": "UTOP-BUS-20260115-A7K3X",
    "correlation_id": "utop-20260115-bk-a7k3x",
    "causation_id": "550e8400-e29b-41d4-a716-000000000001",
    "occurred_at": "2026-01-15T09:17:43.123456Z",
    "published_at": "2026-01-15T09:17:43.250000Z",
    "schema_uri": "utop://events/BookingConfirmedIntegrationEvent/v1.0"
  },
  "payload": {
    "booking_id": "UTOP-BUS-20260115-A7K3X",
    "travel_mode": "Bus",
    "travel_category": "Group",
    "origin": "Delhi",
    "destination": "Agra",
    "departure_at": "2026-01-20T06:00:00Z",
    "passenger_count": 4,
    "total_price_amount": 10000.00,
    "total_price_currency": "INR",
    "operator_id": "OP-JOHN-123",
    "group_id": "GRP-2026-0115-XY7",
    "pilgrimage_id": null,
    "locale": "en-US"
  }
}
```

### 2.2 Envelope Fields — Mandatory

| Field | Type | Rule |
|-------|------|------|
| `event_id` | UUID | Globally unique per event instance; used for inbox deduplication |
| `event_type` | String | Full event type name including "IntegrationEvent" suffix |
| `event_version` | String | Semantic version of this event instance (e.g., "1.0", "2.0") |
| `schema_version` | String | Version of the event schema definition |
| `source_context` | String | Publishing bounded context name |
| `aggregate_type` | String | Type of aggregate that produced the event |
| `aggregate_id` | String | Identity of the aggregate instance |
| `correlation_id` | String | Request correlation ID; propagated from API entry point |
| `causation_id` | UUID | Command ID that caused this event; enables causal tracing |
| `occurred_at` | ISO 8601 UTC | When domain event occurred in aggregate (original timestamp) |
| `published_at` | ISO 8601 UTC | When outbox published to RabbitMQ |
| `schema_uri` | String | Logical URI to schema definition (for tooling/validation) |

### 2.3 Payload Fields — Rules

- Payload contains **only primitive types**: string, number, boolean, ISO 8601 dates, null
- **No domain objects**, value objects, or aggregate references
- **No internal IDs** that are meaningless outside the source context
- **All monetary amounts** split into `amount` (decimal) + `currency` (ISO 4217 code)
- **All timestamps** in UTC, ISO 8601 format
- **Nullable fields** explicitly included as `null` (not omitted)
- **No nested objects deeper than 2 levels** in payload

---

## 3. Event Versioning Strategy

### 3.1 Versioning Rules

UTOP uses **additive versioning** as the primary strategy:

| Change Type | Version Impact | Backward Compatible |
|-------------|---------------|-------------------|
| Add optional field to payload | Patch (1.0 → 1.1) | ✅ Yes — consumers ignore unknown fields |
| Add required field to payload | Minor (1.0 → 2.0) | ❌ No — breaking change |
| Remove field from payload | Minor (1.0 → 2.0) | ❌ No — breaking change |
| Rename field | Minor (1.0 → 2.0) | ❌ No — breaking change |
| Change field type | Minor (1.0 → 2.0) | ❌ No — breaking change |
| Add new event type entirely | No version change | ✅ Yes — new consumers opt in |
| Deprecate event type | Minor (1.0 → 2.0) | ✅ Yes with notice period |

### 3.2 Breaking Change Protocol

When a breaking change is required:

```
Step 1: Publish NEW event version alongside OLD
  → BookingConfirmedIntegrationEvent v1.0 continues
  → BookingConfirmedIntegrationEvent v2.0 introduced
  → Both published simultaneously

Step 2: Consumers migrate to v2.0 at their own pace
  → Each consumer declares which version it consumes
  → Old consumers continue on v1.0

Step 3: Deprecation notice (minimum 30 days before removal)
  → v1.0 deprecated; marked in schema registry
  → All consumers notified via event governance log

Step 4: Remove v1.0 publishing
  → Only after all known consumers migrated
  → Documented in ADR (breaking change = new ADR entry)
```

### 3.3 Consumer Responsibility

```
Consumers MUST:
  - Ignore unknown fields (tolerant reader pattern)
  - Declare which event version they consume
  - Handle missing optional fields gracefully (null-safe)
  - Not assume payload field order

Consumers MUST NOT:
  - Fail on unknown fields
  - Assume all fields always present
  - Hard-code payload structure without schema reference
```

---

## 4. Event Ownership

### 4.1 Ownership Rule

**Every event type has exactly one publisher (owner). No exceptions.**

A consumer may never publish an event owned by another context.

### 4.2 Event Ownership Register

| Event Type | Owner Context | Allowed Consumers |
|------------|--------------|-------------------|
| BookingCreatedIntegrationEvent | BookingContext | Analytics |
| BookingConfirmedIntegrationEvent | BookingContext | ResourceAllocation, Notification, Analytics, CostSplitting |
| BookingCancelledIntegrationEvent | BookingContext | ResourceAllocation, Notification, Analytics, CostSplitting |
| BookingAmendedIntegrationEvent | BookingContext | ResourceAllocation, Notification, Analytics |
| BookingEscalatedIntegrationEvent | BookingContext | Notification (manager), Analytics |
| BookingCompletedIntegrationEvent | BookingContext | Analytics, CostSplitting |
| ResourceAllocatedIntegrationEvent | ResourceAllocationContext | Notification, Analytics |
| ResourceConflictDetectedIntegrationEvent | ResourceAllocationContext | Notification (manager alert) |
| AllocationOverriddenIntegrationEvent | ResourceAllocationContext | Analytics, Audit |
| ResourceReleasedIntegrationEvent | ResourceAllocationContext | Analytics |
| AccommodationBookedIntegrationEvent | AccommodationContext | Notification, Analytics |
| AccommodationCancelledIntegrationEvent | AccommodationContext | Notification, Analytics |
| PilgrimageConfirmedIntegrationEvent | PilgrimageContext | Notification, Analytics, GroupManagement |
| PilgrimageComplianceCheckedIntegrationEvent | PilgrimageContext | Notification (manager if failed) |
| PilgrimageStartedIntegrationEvent | PilgrimageContext | Analytics |
| PilgrimageCompletedIntegrationEvent | PilgrimageContext | Analytics |
| GroupCreatedIntegrationEvent | GroupManagementContext | Notification, Analytics |
| GroupMemberJoinedIntegrationEvent | GroupManagementContext | CostSplitting, Notification |
| GroupMemberLeftIntegrationEvent | GroupManagementContext | CostSplitting, Notification |
| GroupConfirmedIntegrationEvent | GroupManagementContext | Notification, Analytics |
| GroupCancelledIntegrationEvent | GroupManagementContext | Notification, Analytics, CostSplitting |
| CostShareCalculatedIntegrationEvent | CostSplittingContext | Notification, Analytics |
| CostShareRecalculatedIntegrationEvent | CostSplittingContext | Notification, Analytics |
| CostShareDisputedIntegrationEvent | CostSplittingContext | Notification (manager), Analytics |
| CostSettlementCompleteIntegrationEvent | CostSplittingContext | Analytics, Notification |
| PaymentConfirmedIntegrationEvent | CostSplittingContext | Notification, Analytics |
| NotificationSentIntegrationEvent | NotificationContext | Analytics |
| NotificationFailedIntegrationEvent | NotificationContext | Analytics |
| RecommendationGeneratedIntegrationEvent | AIRecommendationContext | Notification (manager), Analytics |
| RecommendationAcceptedIntegrationEvent | AIRecommendationContext | Analytics |
| UserLoggedInIntegrationEvent | IdentityContext | Analytics, Audit |
| UserLoggedOutIntegrationEvent | IdentityContext | Analytics, Audit |
| UserRoleChangedIntegrationEvent | IdentityContext | Audit |

### 4.3 Ownership Enforcement Rules

```
1. A context may only publish events it owns
2. A context may consume any event (subject to authorization)
3. Analytics may consume ALL events (see Section 4.4)
4. A new event type requires an entry in this register before implementation
5. Ownership changes require an Architecture Change Request (ACR)
6. No event may be published without a registered owner
```

### 4.4 Analytics Special Rule

Analytics consumes all events but:

```
Analytics MUST:
  - Consume events as read-only projections
  - Never re-publish events it received
  - Never use analytics projections as operational source-of-truth
  - Never feed analytics data back into transactional workflows

Analytics MUST NOT:
  - Become a shadow operational domain
  - Own any aggregate state
  - Publish integration events (receives only)
  - Store data that operational contexts depend on for decisions
```

---

## 5. PII Handling in Events

### 5.1 PII Classification

| Data Category | Examples | Event Policy |
|---------------|---------|-------------|
| **Direct PII** | Full name, passport number, DOB, phone, email | NEVER in event payload |
| **Indirect PII** | Operator ID, user ID, session ID | Allowed (opaque reference only) |
| **Financial PII** | Card number, account number | NEVER in event payload |
| **Pseudonymous** | Booking ID, group ID, resource ID | Allowed (no re-identification possible) |
| **Aggregate data** | Count, total amount, category | Allowed |

### 5.2 PII Redaction Rules

```
FORBIDDEN in any integration event payload:
  - passenger.name (first or last)
  - passenger.passport_number
  - passenger.date_of_birth
  - passenger.phone_number
  - passenger.email_address
  - payment.card_number
  - payment.account_number
  - user.password_hash
  - any biometric data

ALLOWED (reference only):
  - booking_id (pseudonymous; no PII revealed)
  - operator_id (opaque system reference)
  - user_id (opaque system reference)
  - passenger_count (aggregate; no individual identification)
```

### 5.3 Right to Erasure (GDPR/DPDP Compliance)

```
If a user exercises right to erasure:
  - PII is removed from operational stores
  - Events in outbox/inbox already published: immutable (cannot alter)
  - PII was never in event payload: no event remediation needed
  - Audit logs: PII replaced with tombstone marker

This is why PII-free events are mandatory:
  Published events cannot be recalled.
  If PII were in events, erasure would be impossible.
```

### 5.4 PII Audit Log

Any access to PII (even by authorized personnel) is logged to the immutable audit trail. Events do not carry PII, but the audit log records who accessed what and when.

---

## 6. Retention Policy

### 6.1 Outbox Retention (Source of Truth for Replay)

| Event Category | Outbox Retention | Rationale |
|----------------|-----------------|-----------|
| Booking lifecycle events | 24 months | Business/compliance requirement |
| Financial events (cost, payment) | 84 months (7 years) | Financial audit requirement |
| Allocation and resource events | 24 months | Operational audit |
| Notification events | 3 months | No replay value; delivery confirmation only |
| Analytics events | 24 months | Projection rebuild |
| Identity/auth events | 24 months | Security audit |
| AI recommendation events | 12 months | Model audit trail |

**Immutability guarantee:**
Outbox events are append-only. No update or delete permitted on published events. Archival (to cold storage) permitted after retention window. Archived events are not available for live replay but preserved for compliance.

### 6.2 Inbox Retention (Deduplication Window)

| Consumer Type | Inbox Retention | Rationale |
|---------------|----------------|-----------|
| Operational consumers | 7 days | Covers retry and redelivery windows |
| Projection consumers | 24 hours | Short-lived; replay rebuilds from outbox |
| Notification consumers | 3 days | Retry window |
| Audit consumers | 30 days | Compliance verification |

### 6.3 Replay Eligibility

| Event Category | Replay Eligible | Restriction |
|----------------|----------------|-------------|
| Booking lifecycle | ✅ Yes | Per-aggregate ordered; operational consumer deduplicates |
| Financial events | ✅ Yes | Financial audit; requires explicit ops authorization |
| Allocation events | ✅ Yes | For projection rebuild |
| Notification events | ❌ No | Replay would re-send notifications to users |
| Identity/auth events | ✅ Yes (projection only) | Cannot replay login/logout into operational auth flow |
| AI recommendation events | ✅ Yes (projection only) | Cannot replay recommendations into live decision flows |

**Replay authorization:**
Financial event replay requires explicit sign-off from ops. All replays are logged to audit trail with: who authorized, what was replayed, when, and for what purpose.

---

## 7. RabbitMQ Topology

### 7.1 Exchange Configuration

```
Exchange name: utop.domain.events
Exchange type: topic
Durable: true
Auto-delete: false

Dead Letter Exchange: utop.domain.events.dlx
  Type: direct
  Durable: true
  Purpose: Poison message landing zone
```

### 7.2 Routing Key Convention

```
Pattern: {source_context}.{aggregate_type}.{event_type}

Examples:
  booking.booking.BookingConfirmedIntegrationEvent
  resource.allocation.ResourceAllocatedIntegrationEvent
  group.group.GroupMemberJoinedIntegrationEvent
  cost.ledger.CostShareCalculatedIntegrationEvent
  notification.notification.NotificationFailedIntegrationEvent
  pilgrimage.pilgrimagegroup.PilgrimageConfirmedIntegrationEvent
```

### 7.3 Queue Bindings

```
Queue: utop.resource-allocation.inbox
  Binds: booking.booking.BookingConfirmedIntegrationEvent
  Binds: booking.booking.BookingCancelledIntegrationEvent
  Binds: booking.booking.BookingAmendedIntegrationEvent
  DLQ: utop.resource-allocation.dlq

Queue: utop.notification.inbox
  Binds: booking.booking.* (all booking events)
  Binds: resource.allocation.*
  Binds: pilgrimage.pilgrimagegroup.*
  Binds: group.group.*
  Binds: cost.ledger.*
  DLQ: utop.notification.dlq

Queue: utop.cost-splitting.inbox
  Binds: booking.booking.BookingConfirmedIntegrationEvent
  Binds: booking.booking.BookingCancelledIntegrationEvent
  Binds: booking.booking.BookingCompletedIntegrationEvent
  Binds: group.group.GroupMemberJoinedIntegrationEvent
  Binds: group.group.GroupMemberLeftIntegrationEvent
  Binds: group.group.GroupCancelledIntegrationEvent
  DLQ: utop.cost-splitting.dlq

Queue: utop.analytics.inbox
  Binds: #  (all events)
  DLQ: utop.analytics.dlq

Queue: utop.audit.inbox
  Binds: identity.user.*
  Binds: resource.allocation.AllocationOverriddenIntegrationEvent
  Binds: cost.ledger.CostShareDisputedIntegrationEvent
  DLQ: utop.audit.dlq

Queue: utop.group-management.inbox
  Binds: pilgrimage.pilgrimagegroup.PilgrimageConfirmedIntegrationEvent
  DLQ: utop.group-management.dlq
```

### 7.4 Dead Letter Queue Strategy

```
If message fails after max retries (3 attempts in inbox lease):
  → Routed to context-specific DLQ
  → DLQ retains message for 30 days
  → Ops alert triggered
  → Manual inspection and replay tooling available

DLQ messages:
  - Preserved with full envelope intact
  - Original routing key preserved
  - Failure reason appended as header
  - Replay from DLQ requires explicit ops authorization
```

---

## 8. Implementation Rules

### 8.1 Publishing (Producer Side)

```csharp
// Rule 1: Domain event fires inside aggregate
booking.Confirm(correlationId);
// → booking.DomainEvents contains BookingConfirmed

// Rule 2: Application handler maps domain event to integration event
public class BookingConfirmedDomainEventHandler
{
    public async Task Handle(BookingConfirmed domainEvent)
    {
        var integrationEvent = new BookingConfirmedIntegrationEvent
        {
            Envelope = EventEnvelope.Create(
                eventType: "BookingConfirmedIntegrationEvent",
                eventVersion: "1.0",
                sourceContext: "BookingContext",
                aggregateType: "Booking",
                aggregateId: domainEvent.BookingId,
                correlationId: domainEvent.CorrelationId,
                causationId: domainEvent.CommandId,
                occurredAt: domainEvent.OccurredAt
            ),
            Payload = new BookingConfirmedPayload
            {
                BookingId = domainEvent.BookingId,
                TravelMode = domainEvent.Mode,
                // ... no PII, no domain types
            }
        };

        // Rule 3: Persist to outbox IN SAME TRANSACTION as aggregate
        await _outboxRepository.SaveAsync(integrationEvent);
        // Outbox publisher picks up asynchronously
    }
}

// Rule 4: Never publish directly to RabbitMQ from aggregate or handler
// WRONG:
await _messageBus.PublishAsync(integrationEvent);  // Bypasses outbox

// CORRECT:
await _outboxRepository.SaveAsync(integrationEvent);  // Outbox guarantees delivery
```

### 8.2 Consuming (Consumer Side)

```csharp
// Rule 1: Check inbox before processing
public class BookingConfirmedHandler : IIntegrationEventHandler<BookingConfirmedIntegrationEvent>
{
    public async Task HandleAsync(BookingConfirmedIntegrationEvent evt)
    {
        // Step 1: Inbox deduplication
        if (await _inbox.AlreadyProcessedAsync(evt.Envelope.EventId))
            return;  // Idempotent; already handled

        // Step 2: Mark as processing (claim lease)
        await _inbox.MarkProcessingAsync(evt.Envelope.EventId, evt);

        try
        {
            // Step 3: Execute handler logic
            await _allocationSaga.StartAsync(evt.Payload.BookingId, evt.Envelope.CorrelationId);

            // Step 4: Mark as processed
            await _inbox.MarkProcessedAsync(evt.Envelope.EventId);
        }
        catch (Exception ex)
        {
            // Step 5: Mark as failed; lease reclaim job will retry
            await _inbox.MarkFailedAsync(evt.Envelope.EventId, ex.Message);
            throw;  // Re-throw for RabbitMQ NACK
        }
    }
}

// Rule 2: Tolerate unknown fields (tolerant reader)
// JSON deserialization must use:
//   JsonSerializerOptions { UnmappedMemberHandling = Skip }
// Never fail on unknown payload fields
```

---

## 9. Event Governance Process

### 9.1 Adding a New Event

```
1. Identify owning context
2. Add entry to Section 4.2 (Ownership Register)
3. Define payload (Section 2.3 rules)
4. Classify PII (Section 5)
5. Define retention (Section 6)
6. Add RabbitMQ routing key (Section 7.2)
7. Add consumer queue bindings (Section 7.3)
8. Implement domain event → integration event mapping
9. Write consumer inbox handler
10. Write producer + consumer tests
```

### 9.2 Modifying an Existing Event (Breaking Change)

```
1. Determine if change is breaking (Section 3.1)
2. If breaking: raise Architecture Change Request (ACR)
3. ACR documents: reason, impact, migration path, deprecation timeline
4. Publish new version alongside old (Section 3.2)
5. Update Ownership Register with version notes
6. Notify all consumers (documented in ACR)
7. Run 30-day parallel publishing window
8. Remove old version after all consumers migrated
9. Archive old schema definition (do not delete)
```

---

## 10. Document Sign-Off

| Attribute | Value |
|-----------|-------|
| Version | 1.0 |
| Status | BASELINE LOCKED |
| Classification | Binding Event Contract |
| Next Artifact | Context Ownership Matrix (08) |
| Amendment Process | Architecture Change Request required |

---

**End of Event Contract Governance**
