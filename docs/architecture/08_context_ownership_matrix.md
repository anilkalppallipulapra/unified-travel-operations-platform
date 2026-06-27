# Context Ownership Matrix
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0
**Status:** BASELINE LOCKED
**Phase:** Phase 3 — Architectural Stabilization (Pre-LLD)
**Classification:** Project Internal — Binding Context Governance

---

## Purpose

This document formally governs what each bounded context owns, references, publishes, consumes, and is forbidden from doing. It prevents:

- Cross-context data ownership disputes
- Analytics becoming a shadow operational domain
- AIRecommendation making autonomous decisions
- Contexts accumulating forbidden dependencies
- Source-of-truth ambiguity during implementation

Without this, LLD will silently reintroduce coupling.

---

## Governance Rules

Before reading per-context definitions, these rules apply universally:

| Rule | Statement |
|------|-----------|
| **Single Source of Truth** | Every data domain has exactly one owning context. No other context may mutate that data. |
| **Read via Event** | A context needing another context's data receives it via integration events or read-model projections. Never via direct database query across schemas. |
| **No Cross-Schema Queries** | No SQL JOIN across context schemas (e.g., `utop_booking` JOIN `utop_resource`). |
| **Publish Don't Share** | A context exposes its state by publishing events. It does not expose its repository or domain model. |
| **No Shared DTOs** | No shared DTO packages across bounded contexts (e.g., `CommonBookingDto`, `SharedContracts`, `BookingInfo`). Each context defines its own DTOs. Shared DTOs are the most common way bounded-context independence is silently destroyed. Integration events are the only sanctioned cross-context data contract. |
| **Forbidden = Hard Rule** | Items listed as FORBIDDEN are architectural violations. Not guidelines. Not preferences. |

---

## 1. Booking Context

**Schema:** `utop_booking`

### Owns (Source of Truth)
- Booking lifecycle and status
- Booking identity (BookingId)
- Itinerary and journey route
- Passenger manifest
- Booking price and currency
- Travel category assignment
- Operator assignment per booking

### May Reference

**Opaque Identity References** (no internals; reference only):
- Group existence (GroupId — opaque; group internals not accessible)
- Pilgrimage existence (PilgrimageId — opaque; pilgrimage internals not accessible)

**External Service Adapters** (via port interface; stub → real):
- Resource availability (via IBookingProvider — not ResourceAllocation domain model)
- Prayer schedule (via IPrayerTimeProvider — for pilgrimage leg validation only)

**Infrastructure Services** (read-only; no domain coupling):
- Locale/translations (via ILocalizationService — rendering only)

### Publishes
- BookingCreatedIntegrationEvent
- BookingConfirmedIntegrationEvent
- BookingCancelledIntegrationEvent
- BookingAmendedIntegrationEvent
- BookingEscalatedIntegrationEvent
- BookingCompletedIntegrationEvent

### Consumes
- None from other contexts directly
- Receives external availability data via IBookingProvider adapter (stub → real)

### FORBIDDEN
- Reading from `utop_resource`, `utop_group`, `utop_cost`, `utop_pilgrimage` schemas
- Owning cost calculation logic
- Owning resource allocation decisions
- Publishing events owned by other contexts
- Calling GroupManagement or CostSplitting services directly

---

## 2. Accommodation Context

**Schema:** `utop_accommodation`

### Owns
- Accommodation booking lifecycle
- Room assignments
- Ancillary service bookings (transfers, meals, excursions)
- Accommodation pricing

### May Reference
- Booking existence (BookingId — opaque reference)
- Locale/translations (read-only)
- Sacred site proximity data for pilgrimage (via PilgrimageContext service interface — read-only)

### Publishes
- AccommodationBookedIntegrationEvent
- AccommodationCancelledIntegrationEvent
- AccommodationAmendedIntegrationEvent

### Consumes
- PilgrimageConfirmedIntegrationEvent (to link accommodation to pilgrimage group)
- BookingCancelledIntegrationEvent (to release accommodation hold)

### FORBIDDEN
- Owning pilgrimage compliance logic
- Reading from `utop_pilgrimage` schema directly
- Making allocation decisions

---

## 3. ResourceAllocation Context

**Schema:** `utop_resource`

### Owns
- Resource inventory (buses, trains, aircraft, guides, drivers)
- Resource availability windows
- Allocation decisions (AllocationDecision aggregate)
- Allocation strategy and priority rules
- Conflict detection and escalation

### May Reference

**Via Event Consumption** (no direct query):
- Booking status (via BookingConfirmedIntegrationEvent payload — not Booking domain)

**Opaque Identity References:**
- Manager identity for override recording (UserId — opaque reference)

### Publishes
- ResourceAllocatedIntegrationEvent
- ResourceConflictDetectedIntegrationEvent
- AllocationOverriddenIntegrationEvent
- ResourceReleasedIntegrationEvent

### Consumes
- BookingConfirmedIntegrationEvent (triggers allocation saga)
- BookingCancelledIntegrationEvent (triggers resource release)
- BookingAmendedIntegrationEvent (triggers re-evaluation)

### FORBIDDEN
- Owning booking lifecycle
- Modifying booking status directly
- Reading from `utop_booking` schema directly
- Making pilgrimage compliance decisions
- Owning cost calculations related to resource use

---

## 4. Pilgrimage Context

**Schema:** `utop_pilgrimage`

### Owns
- Pilgrimage group lifecycle
- Sacred site definitions and access rules
- Prayer schedule compliance checks
- Guide assignment to pilgrimage group
- Group cohesion enforcement
- Compliance check results

### May Reference

**Via Event Consumption:**
- Booking existence per pilgrim (BookingId — via BookingConfirmedIntegrationEvent)
- Accommodation confirmation near sacred sites (via AccommodationBookedIntegrationEvent)

**External Service Adapters** (via port interface; stub → real):
- Prayer times (via IPrayerTimeProvider — external stub)
- Guide resource availability (via IResourceAvailabilityService — read-only snapshot)

**Infrastructure Services:**
- Locale/translations (read-only)

### Publishes
- PilgrimageConfirmedIntegrationEvent
- PilgrimageComplianceCheckedIntegrationEvent
- PilgrimageStartedIntegrationEvent
- PilgrimageCompletedIntegrationEvent
- PilgrimageCancelledIntegrationEvent

### Consumes
- BookingConfirmedIntegrationEvent (to register pilgrim bookings)
- AccommodationBookedIntegrationEvent (to verify sacred site proximity)
- ResourceAllocatedIntegrationEvent (to confirm guide allocation)

### FORBIDDEN
- Owning booking lifecycle
- Owning accommodation booking logic
- Reading from `utop_booking` or `utop_accommodation` schemas directly
- Making financial decisions (cost, refund, split)

---

## 5. GroupManagement Context

**Schema:** `utop_group`

### Owns
- Group lifecycle and status
- Group membership (members, roles, coordinator)
- Travel dates for the group
- Group cohesion configuration

### May Reference
- Booking existence per member (BookingId — opaque; via event)
- Pilgrimage association (PilgrimageId — opaque reference)
- Locale/translations (read-only)

### Publishes
- GroupCreatedIntegrationEvent
- GroupMemberJoinedIntegrationEvent
- GroupMemberLeftIntegrationEvent
- GroupConfirmedIntegrationEvent
- GroupCancelledIntegrationEvent

### Consumes
- BookingConfirmedIntegrationEvent (to associate member booking with group)
- PilgrimageConfirmedIntegrationEvent (to link group to pilgrimage)

### FORBIDDEN
- Owning cost calculation logic
- Reading from `utop_cost` schema directly
- Owning booking lifecycle
- Publishing cost or financial events

---

## 6. CostSplitting Context

**Schema:** `utop_cost`

### Owns
- Cost ledger lifecycle
- Cost share calculation per member
- Payment tracking per member
- Refund calculation
- Dispute resolution records
- Cancellation policy snapshots (stored at ledger creation)

### May Reference

**Via Event Consumption:**
- Group membership count (via GroupMemberJoinedIntegrationEvent / GroupMemberLeftIntegrationEvent)
- Booking total price (via BookingConfirmedIntegrationEvent payload only)

**Opaque Identity References:**
- Member identity (UserId — opaque reference; no Identity internals)

### Publishes
- CostShareCalculatedIntegrationEvent
- CostShareRecalculatedIntegrationEvent
- CostShareDisputedIntegrationEvent
- CostSettlementCompleteIntegrationEvent
- PaymentConfirmedIntegrationEvent

### Consumes
- BookingConfirmedIntegrationEvent (to create ledger; extract total price)
- BookingCancelledIntegrationEvent (to trigger refund calculation)
- BookingCompletedIntegrationEvent (to settle ledger)
- GroupMemberJoinedIntegrationEvent (to recalculate shares)
- GroupMemberLeftIntegrationEvent (to recalculate shares; compute refund)
- GroupCancelledIntegrationEvent (to close ledger; process refunds)

### FORBIDDEN
- Owning booking lifecycle
- Owning group membership
- Reading from `utop_booking` or `utop_group` schemas directly
- Making allocation or resource decisions
- Storing full booking details (only BookingId + price from event payload)

---

## 7. Notifications Context

**Schema:** `utop_notification`

### Owns
- Notification lifecycle (creation, delivery, retry, failure)
- Notification templates
- Delivery status per notification
- User notification preferences
- Quiet hours configuration per user

### May Reference

**Opaque Identity References:**
- Recipient identity (UserId — opaque reference; no Identity internals)

**Infrastructure Services:**
- Locale/translations for template rendering (via ILocalizationService — read-only; rendering only)

**Own Schema** (not cross-context):
- User notification preferences (stored in utop_notification schema; set by user via API)

### Publishes
- NotificationSentIntegrationEvent
- NotificationFailedIntegrationEvent

### Consumes (triggers for notification creation)
- BookingConfirmedIntegrationEvent
- BookingCancelledIntegrationEvent
- BookingAmendedIntegrationEvent
- BookingEscalatedIntegrationEvent
- ResourceConflictDetectedIntegrationEvent
- AllocationOverriddenIntegrationEvent
- PilgrimageComplianceCheckedIntegrationEvent (if failed — manager alert)
- PilgrimageConfirmedIntegrationEvent
- GroupMemberJoinedIntegrationEvent
- GroupMemberLeftIntegrationEvent
- GroupCancelledIntegrationEvent
- CostShareCalculatedIntegrationEvent
- CostShareRecalculatedIntegrationEvent
- CostShareDisputedIntegrationEvent
- PaymentConfirmedIntegrationEvent
- RecommendationGeneratedIntegrationEvent (manager alert for low-confidence)

### FORBIDDEN
- Owning any business domain logic
- Making decisions about booking, allocation, or cost
- Storing operational data (only delivery-relevant data)
- Re-publishing domain events it received
- Becoming a workflow coordinator

---

## 8. KnowledgeBase Context

**Schema:** `utop_knowledge`

### Owns
- Knowledge module content and lifecycle
- Learning path definitions
- Completion records per user
- Contextual help mappings (action → module)

### May Reference
- User identity (UserId — opaque reference)
- Locale/translations (read-only)

### Publishes
- KnowledgeModuleViewedIntegrationEvent
- KnowledgeModuleCompletedIntegrationEvent

### Consumes
- None (standalone; no operational event dependencies)

### FORBIDDEN
- Owning operational workflow data
- Influencing booking or allocation decisions
- Reading from any operational context schema

---

## 9. Analytics Context

**Schema:** `utop_analytics`

### Owns
- Read-model projections (pre-aggregated metrics)
- Dashboard data snapshots
- Report definitions and generated reports
- Projection versioning

### May Reference
- All integration events (consumes all; projection-only)

### Publishes
- Nothing. Analytics context publishes NO integration events.

### Consumes
- ALL integration events (via `utop.analytics.inbox` queue bound to `#`)

### FORBIDDEN — CRITICAL GOVERNANCE

```
Analytics MUST NOT:
  - Own any aggregate state
  - Publish integration events of any kind
  - Become source-of-truth for ANY operational data
  - Feed projection data back into transactional workflows
  - Store operational data that other contexts depend on
  - Rebuild operational state from events (projection only)
  - Accept writes from operational contexts (read-model only)

Analytics projections are:
  - Derived data (not authoritative)
  - Eventually consistent (not real-time operational truth)
  - Rebuild-safe (can be dropped and rebuilt from outbox)
  - Never used as input to business decisions in operational contexts
```

**Event Evolution Tolerance:**

Analytics consumes all events. When event schemas evolve (new fields added, versions incremented), Analytics projections MUST tolerate additive changes without handler failure. Projection handlers must apply the tolerant reader pattern: unknown fields are ignored; missing optional fields default to null. A schema evolution in any publisher context must never break Analytics projection consumers.

**Anti-dumping-ground enforcement:**

If any feature request proposes storing data "in analytics because it's easier," that is an architectural violation. The owning context must own the data. Analytics receives it via events.

---

## 10. AIRecommendation Context

**Schema:** `utop_ai`

### Owns
- Recommendation lifecycle (generated, accepted, rejected, expired)
- Model metadata (name, version, confidence)
- Input context snapshots (immutable at recommendation creation)
- Recommendation decision log

### May Reference

**Via Event Consumption:**
- Booking data (via BookingConfirmedIntegrationEvent — for pricing context snapshot)
- Resource conflict data (via ResourceConflictDetectedIntegrationEvent — for allocation context snapshot)

**Read-Only Projections:**
- Historical analytics projections (via Analytics read API — advisory context only; never operational input)

**External Service Adapters:**
- Resource availability snapshot (via IResourceAvailabilityService — point-in-time read; not live operational state)

**Opaque Identity References:**
- Manager identity for acceptance/rejection recording (UserId — opaque reference)

### Publishes
- RecommendationGeneratedIntegrationEvent
- RecommendationAcceptedIntegrationEvent
- RecommendationRejectedIntegrationEvent

### Consumes
- BookingConfirmedIntegrationEvent (pricing recommendation trigger)
- ResourceConflictDetectedIntegrationEvent (allocation recommendation trigger)

### FORBIDDEN — ADVISORY ONLY ENFORCEMENT

```
AIRecommendation MUST NOT:
  - Make autonomous operational decisions
  - Directly mutate booking, allocation, or cost state
  - Trigger sagas without manager approval
  - Publish events that operational contexts act on without human review
  - Store training data (model training is external)
  - Own model infrastructure

Every recommendation requires explicit manager acceptance before
any operational context acts on it. AIRecommendation is advisory.
It informs; it does not decide.
```

---

## 11. Identity Context

**Schema:** `utop_identity`

### Owns
- User accounts and credentials
- Role assignments (RBAC)
- Session lifecycle
- Login/logout audit records
- Account lockout state

### May Reference
- Locale preference (stored in own schema; locale code only — not Localization internals)

### Publishes
- UserLoggedInIntegrationEvent
- UserLoggedOutIntegrationEvent
- UserRoleChangedIntegrationEvent

### Consumes
- None

### FORBIDDEN
- Owning booking, group, or cost data
- Making business domain decisions
- Storing PII beyond what is strictly necessary for authentication

---

## 12. Localization Context

**Schema:** `utop_localization`

### Owns
- Supported locale definitions
- Translation key-value pairs per locale
- RTL/LTR configuration per locale
- Date, time, and currency format rules per locale
- Timezone mapping per locale

### May Reference
- Nothing from operational contexts

### Publishes
- TranslationUpdatedIntegrationEvent (consumed by contexts caching translations)

### Consumes
- None from operational contexts

### FORBIDDEN
- Owning user preferences (Identity owns preferred locale code)
- Owning prayer schedule data (Pilgrimage owns)
- Owning timezone business rules (operational contexts apply locale timezone rules)
- Making operational decisions

---

## 13. TravelCategory Context

**Schema:** `utop_category`

### Owns
- Category rule definitions (Personal, Leisure, Religious, Group)
- Category-specific constraint configurations
- Category validation rules

### May Reference
- Locale/translations (read-only)

### Publishes
- CategoryRuleUpdatedIntegrationEvent

### Consumes
- None

### FORBIDDEN
- Owning booking lifecycle
- Making booking decisions
- Directly enforcing rules in other contexts (provides rules; contexts apply them)

---

## 14. Source-of-Truth Declaration

| Data Domain | Owning Context | All Others |
|-------------|---------------|------------|
| Booking lifecycle and status | BookingContext | Read via events only |
| Journey itinerary | BookingContext | Read via events only |
| Passenger manifest | BookingContext | Read via events only |
| Resource inventory | ResourceAllocationContext | Read via events only |
| Allocation decisions | ResourceAllocationContext | Read via events only |
| Accommodation bookings | AccommodationContext | Read via events only |
| Pilgrimage compliance | PilgrimageContext | Read via events only |
| Sacred site definitions | PilgrimageContext | Read via events only |
| Group membership | GroupManagementContext | Read via events only |
| Cost ledger | CostSplittingContext | Read via events only |
| Payment status | CostSplittingContext | Read via events only |
| Notification delivery | NotificationContext | Read via events only |
| Knowledge modules | KnowledgeBaseContext | Read via events only |
| Analytics projections | AnalyticsContext | Read via API (never operational input) |
| AI recommendations | AIRecommendationContext | Read via events only; advisory only |
| User accounts and roles | IdentityContext | Read via JWT claims in API layer |
| Translations | LocalizationContext | Read via service interface (cached) |
| Category rules | TravelCategoryContext | Read via service interface |

---

## 15. Forbidden Dependency Map

The following dependencies are **architectural violations** regardless of implementation convenience:

```
BookingContext        ──X──► utop_resource (direct DB query)
BookingContext        ──X──► utop_group (direct DB query)
BookingContext        ──X──► utop_cost (direct DB query)
ResourceAllocation   ──X──► utop_booking (direct DB query)
CostSplitting        ──X──► utop_booking (direct DB query)
CostSplitting        ──X──► utop_group (direct DB query)
Analytics            ──X──► any operational context (write path)
Analytics            ──X──► publishing any integration event
AIRecommendation     ──X──► mutating any aggregate directly
AIRecommendation     ──X──► triggering sagas autonomously
Notifications        ──X──► owning any business domain logic
Any context          ──X──► querying another context's schema
Any context          ──X──► importing another context's domain model
```

---

## 16. Document Sign-Off

| Attribute | Value |
|-----------|-------|
| Version | 1.0 |
| Status | BASELINE LOCKED |
| Classification | Binding Context Governance |
| Next Artifact | Temporal Semantics (09) |
| Amendment Process | Architecture Change Request required |

---

**End of Context Ownership Matrix**
