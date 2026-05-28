# Aggregate Invariant Specification
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0
**Status:** LOCKED
**Phase:** Phase 3 — Architectural Stabilization (Pre-LLD)
**Classification:** Project Internal — Binding Implementation Contract

---

## Purpose

This document formalizes all aggregate invariants for every bounded context in UTOP.

An **invariant** is a rule that must **always** be true for an aggregate to be in a valid state. Invariants are:

- **Enforced unconditionally** by the aggregate itself (not the application layer)
- **Test oracles** — every invariant becomes a mandatory unit test
- **Validation contracts** — every command handler validates against these before persistence
- **Audit material** — any invariant violation is a critical system error

### Invariant Categories

| Category | Definition |
|----------|-----------|
| **Hard Invariant** | Must always be true; no exceptions; violation = domain exception |
| **Temporal Invariant** | Time-dependent rule; must be validated against system clock at mutation time |
| **Cross-Aggregate Assumption** | Rule that depends on state of another aggregate; validated via domain service or read model |
| **Compensation Invariant** | Rule that governs valid rollback/compensation states during saga failure |
| **Concurrency Invariant** | Rule that governs behavior under concurrent mutations |

### Enforcement Rule

> **Every invariant must be enforced inside the aggregate method that mutates state.**
> No invariant may be enforced solely in the application layer.
> Application layer may perform pre-validation but aggregate is the final authority.

---

## 1. Booking Aggregate

### 1.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| BK-INV-001 | BookingId is immutable after creation | `BookingId` is set only in factory method; no setter |
| BK-INV-002 | TravelMode is immutable after creation | `Mode` has no mutating method |
| BK-INV-003 | Currency is immutable after booking confirmation | `Amend()` rejects price changes with different currency |
| BK-INV-004 | A confirmed booking must have at least one adult passenger | `Confirm()` validates `_passengers.Count(p => p.Type == Adult) >= 1` |
| BK-INV-005 | PassengerCount.Total must equal active passenger entities count | Enforced on `AddPassenger()` and `RemovePassenger()` |
| BK-INV-006 | Booking in `Cancelled` status cannot transition to any status except `Refunded` | `Confirm()`, `Complete()`, `Allocate()` throw if status is `Cancelled` |
| BK-INV-007 | Booking in `Completed` status is immutable | All mutation methods throw `BookingAlreadyCompletedException` if `Completed` |
| BK-INV-008 | Pilgrimage bookings require `PilgrimageId` association before `Confirmed` | `Confirm()` validates `PilgrimageId != null` when `Category == Religious` |
| BK-INV-009 | Group bookings require `GroupId` association before `Confirmed` | `Confirm()` validates `GroupId != null` when `Category == Group` |
| BK-INV-010 | TotalPrice must be greater than zero | Factory method rejects `price.Amount <= 0` |
| BK-INV-011 | Route Origin and Destination must differ | Factory method rejects `Route.Origin == Route.Destination` |
| BK-INV-012 | OperatorId must not be null or empty | Factory method enforces non-null operator identity |
| BK-INV-013 | Category is immutable after `Confirmed` | `ChangeCategory()` throws if status is not `Draft` |
| BK-INV-014 | A booking may only have one active itinerary | `Itinerary` is replaced atomically in `Amend()`, never appended |

### 1.2 Temporal Invariants

| ID | Invariant | Clock Reference |
|----|-----------|-----------------|
| BK-TINV-001 | Departure time must be in the future at time of creation | Validated against `DateTime.UtcNow` in factory method |
| BK-TINV-002 | Amendments are forbidden within 2 hours of scheduled departure | `Amend()` validates `Itinerary.DepartureTime - UtcNow > 2 hours` |
| BK-TINV-003 | Cancellation refund window is time-bound per policy | `Cancel()` records timestamp; refund calculation uses cancellation policy rules |
| BK-TINV-004 | Booking confirmation must occur before departure | `Confirm()` rejects if departure has already passed |
| BK-TINV-005 | For pilgrimage bookings, departure must allow prayer schedule compliance window | Validated by `PilgrimageSaga` before `Confirm()` is called |

### 1.3 Cross-Aggregate Assumptions

| ID | Assumption | Validation Strategy |
|----|------------|-------------------|
| BK-CINV-001 | Allocation cannot exist for a `Cancelled` booking | `ResourceAllocationSaga` listens for `BookingCancelled` and releases resource |
| BK-CINV-002 | CostLedger settlement requires booking in `Confirmed` or later status | `CostSplittingContext` validates booking status before ledger creation |
| BK-CINV-003 | A group booking must reference a valid, active Group | Validated by `GroupManagementContext` service before `GroupId` is assigned |
| BK-CINV-004 | A pilgrimage booking must reference a valid PilgrimageGroup | Validated by `PilgrimageContext` service before `PilgrimageId` is assigned |

### 1.4 Concurrency Invariants

| ID | Invariant | Mechanism |
|----|-----------|-----------|
| BK-CONC-001 | Concurrent amendments to the same booking are rejected | Optimistic concurrency via `RowVersion`/`xmin` in PostgreSQL |
| BK-CONC-002 | Confirm and Cancel cannot execute simultaneously on same booking | Optimistic lock; last-writer-loses is rejected |

### 1.5 Compensation Invariants

| ID | Invariant | Compensation |
|----|-----------|-------------|
| BK-COMP-001 | If saga fails after `Confirmed`, booking reverts to `Escalated` (not `Draft`) | Saga compensating step: `EscalateBooking()` |
| BK-COMP-002 | A refunded booking's TotalPrice is immutable | `Refund()` records refund amount in separate ledger entry, does not mutate `TotalPrice` |

---

## 2. AllocationDecision Aggregate

### 2.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| AD-INV-001 | An allocation decision must reference a valid BookingId | Factory method rejects null/empty `bookingId` |
| AD-INV-002 | An allocation decision must reference a valid ResourceId | Factory method rejects null/empty `resourceId` |
| AD-INV-003 | PriorityScore must be between 0 and 100 inclusive | Factory method rejects out-of-range values |
| AD-INV-004 | DecisionRationale must not be null or empty | Factory method enforces; overrides require justification |
| AD-INV-005 | A `ManuallyOverridden` decision must record the overriding manager's ID | `OverrideByManager()` rejects null `managerId` |
| AD-INV-006 | Override justification must not be empty | `OverrideByManager()` rejects null/empty `justification` |
| AD-INV-007 | An `EscalatedToManager` decision cannot be auto-resolved | Only manager action can resolve escalation |
| AD-INV-008 | Resource capacity must satisfy booking passenger count | `Allocate()` validates `resource.Capacity >= booking.Passengers.Total` |
| AD-INV-009 | A released resource allocation cannot be re-allocated without a new decision | `Released` status is terminal; new `AllocationDecision` must be created |

### 2.2 Temporal Invariants

| ID | Invariant | Clock Reference |
|----|-----------|-----------------|
| AD-TINV-001 | Allocation must occur before booking departure | Validated against `Itinerary.DepartureTime` |
| AD-TINV-002 | Escalated allocations auto-notify manager if unresolved after 30 minutes | Scheduled job checks escalation age |

### 2.3 Cross-Aggregate Assumptions

| ID | Assumption | Validation Strategy |
|----|------------|-------------------|
| AD-CINV-001 | Resource being allocated must be in `Active` status | `ResourceAllocationSaga` queries resource status before allocation |
| AD-CINV-002 | Booking must be in `Confirmed` status before allocation | `BookingConfirmed` event triggers allocation; status checked |
| AD-CINV-003 | No two confirmed allocations may reference the same resource for overlapping date ranges | Checked during allocation; conflict triggers escalation |

### 2.4 Concurrency Invariants

| ID | Invariant | Mechanism |
|----|-----------|-----------|
| AD-CONC-001 | Two concurrent allocation attempts for the same resource are serialized | Pessimistic lock on resource record during allocation check |
| AD-CONC-002 | Manager override and auto-allocation cannot execute simultaneously | Optimistic concurrency on `AllocationDecision` |

---

## 3. CostLedger Aggregate

### 3.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| CL-INV-001 | Sum of all CostShare amounts must equal TotalCost | Enforced after every `CalculateShares()` and `RecalculateForMemberChange()` |
| CL-INV-002 | Rounding difference must be allocated; sum must never drift from TotalCost | Rounding adjustment applied to first member deterministically |
| CL-INV-003 | MemberCount must equal active CostShare count | Enforced on every recalculation |
| CL-INV-004 | TotalCost is immutable after ledger enters `Settled` status | `Settled` ledger throws on any mutation attempt |
| CL-INV-005 | A CostShare cannot go negative | `RecordPayment()` rejects overpayment that results in negative outstanding |
| CL-INV-006 | PaidAmount cannot exceed Amount for any CostShare | `RecordPayment()` rejects excess payment; triggers refund workflow instead |
| CL-INV-007 | A `Disputed` ledger cannot be settled without manager resolution | `Settle()` throws if status is `Disputed` |
| CL-INV-008 | Refund calculation must use the cancellation policy active at booking time | Policy snapshot stored at ledger creation; not re-fetched |
| CL-INV-009 | Currency must be consistent across all CostShares | All shares use ledger's currency; no mixed-currency shares |
| CL-INV-010 | MemberCount must be at least 1 | Factory method rejects `memberCount < 1` |

### 3.2 Temporal Invariants

| ID | Invariant | Clock Reference |
|----|-----------|-----------------|
| CL-TINV-001 | Refund amount decreases as departure date approaches (per policy) | `CalculateRefundForDeparture()` uses `DateOnly.FromDateTime(UtcNow)` |
| CL-TINV-002 | Refunds after departure are prohibited | `CalculateRefundForDeparture()` returns `Money.Zero` if departure has passed |
| CL-TINV-003 | Payment deadline is enforced; overdue triggers notification | Scheduled job checks payment deadlines |

### 3.3 Cross-Aggregate Assumptions

| ID | Assumption | Validation Strategy |
|----|------------|-------------------|
| CL-CINV-001 | CostLedger can only be created for a booking in `Confirmed` or later status | `CostSplittingContext` validates booking status via read model |
| CL-CINV-002 | Group member joining/leaving triggers mandatory recalculation | `GroupMemberJoined`/`GroupMemberLeft` events consumed; recalculation is mandatory |
| CL-CINV-003 | A settled ledger is closed even if group membership changes post-settlement | Settled ledger is immutable; new amendment ledger created instead |

---

## 4. PilgrimageGroup Aggregate

### 4.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| PG-INV-001 | A PilgrimageGroup must have at least one pilgrim booking before `Confirmed` | `Confirm()` validates `_pilgrimBookingIds.Count >= 1` |
| PG-INV-002 | A PilgrimageGroup must have a qualified guide assigned before `Confirmed` | `Confirm()` validates `GuideId != null` |
| PG-INV-003 | A PilgrimageGroup must pass compliance check before `Confirmed` | `Confirm()` validates `LastComplianceCheck != null && LastComplianceCheck.Passed` |
| PG-INV-004 | Sacred site visits must not overlap with prayer windows | Enforced in `RunComplianceCheck()` |
| PG-INV-005 | PilgrimageType is immutable after creation | No mutation method; set only in factory |
| PG-INV-006 | Religion is immutable after creation | No mutation method; set only in factory |
| PG-INV-007 | Strict cohesion groups cannot separate pilgrims | `RemovePilgrim()` throws if `CohesionLevel == Strict` and pilgrimage is `InProgress` |
| PG-INV-008 | Compliance check result must be current (not older than 24 hours) before departure | `Confirm()` validates `LastComplianceCheck.CheckedAt > UtcNow - 24h` |
| PG-INV-009 | Multi-leg journeys must be sequential; no overlapping legs | `AddLeg()` validates no time overlap with existing legs |

### 4.2 Temporal Invariants

| ID | Invariant | Clock Reference |
|----|-----------|-----------------|
| PG-TINV-001 | Sacred site hours must be valid on planned visit date | Validated against site's operating calendar during compliance check |
| PG-TINV-002 | Prayer times must be fetched for the specific date and location of each leg | `IPrayerTimeProvider` called per leg per date |
| PG-TINV-003 | Transport schedule must have minimum 30-minute buffer around prayer times | Enforced in compliance check |

### 4.3 Cross-Aggregate Assumptions

| ID | Assumption | Validation Strategy |
|----|------------|-------------------|
| PG-CINV-001 | Each pilgrim booking must be in `Confirmed` status | Validated via `BookingContext` read model before pilgrimage confirmation |
| PG-CINV-002 | Guide must be an active resource in `ResourceAllocationContext` | Guide resource status validated before assignment |
| PG-CINV-003 | Accommodation must be confirmed near sacred sites before pilgrimage confirmation | `AccommodationContext` confirmation checked |

---

## 5. Group Aggregate

### 5.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| GR-INV-001 | A Group must have exactly one Coordinator | `AddMember()` enforces single coordinator; `RemoveMember()` rejects coordinator removal |
| GR-INV-002 | Coordinator cannot be removed without designating a replacement | `TransferCoordinator()` must be called before removing current coordinator |
| GR-INV-003 | Duplicate members (same UserId) are rejected | `AddMember()` checks for existing active member with same UserId |
| GR-INV-004 | A `Cancelled` group cannot accept new members | `AddMember()` throws if group status is `Cancelled` |
| GR-INV-005 | A `Completed` group is immutable | All mutation methods throw if status is `Completed` |
| GR-INV-006 | GroupName must not be null or empty | Factory method enforces |
| GR-INV-007 | TravelDates start must be before end | Factory method enforces via `DateRange` invariant |

### 5.2 Cross-Aggregate Assumptions

| ID | Assumption | Validation Strategy |
|----|------------|-------------------|
| GR-CINV-001 | All group member bookings must be in `Confirmed` status before group `Confirmed` | `GroupManagementContext` validates via read model |
| GR-CINV-002 | `CostLedger` must exist for group before payment collection | `CostSplittingContext` event consumed; ledger existence checked |

---

## 6. Notification Aggregate

### 6.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| NT-INV-001 | RecipientId must not be null or empty | Factory method enforces |
| NT-INV-002 | TemplateId must reference a valid template | Validated by `NotificationService` before aggregate creation |
| NT-INV-003 | RetryCount cannot exceed MaxRetries | `MarkFailed()` sets `PermanentlyFailed` when `RetryCount >= MaxRetries` |
| NT-INV-004 | A `PermanentlyFailed` notification cannot be retried | `CanRetry` property enforces; scheduler checks before retry |
| NT-INV-005 | A `Sent` notification is immutable | `MarkFailed()` throws if status is `Sent` |
| NT-INV-006 | Channel must match recipient's registered preferences | Validated before notification creation |
| NT-INV-007 | PII must not be stored in notification content | Template rendering must mask sensitive data |

### 6.2 Temporal Invariants

| ID | Invariant | Clock Reference |
|----|-----------|-----------------|
| NT-TINV-001 | Notifications in quiet hours are queued, not sent | Checked against user's quiet hours preference and timezone |
| NT-TINV-002 | Retry intervals follow exponential backoff (1min, 5min, 15min) | Scheduler enforces; retry timestamp stored |
| NT-TINV-003 | Notifications older than 24 hours without delivery are permanently failed | Scheduled cleanup job enforces |

---

## 7. User Aggregate (Identity Context)

### 7.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| US-INV-001 | Email must be unique across all users | Enforced at repository level (unique index) |
| US-INV-002 | Password is never stored in plain text | `SetPassword()` always applies Bcrypt hash |
| US-INV-003 | A locked user cannot authenticate | `RecordSuccessfulLogin()` throws if `IsLocked` |
| US-INV-004 | FailedLoginAttempts cannot be decremented except by successful login | Only `RecordSuccessfulLogin()` resets counter |
| US-INV-005 | Role changes must be recorded with changing admin's ID | `ChangeRole()` rejects null `changedByAdminId` |
| US-INV-006 | A `Deactivated` user cannot be reactivated without admin action | Status transitions enforced explicitly |
| US-INV-007 | PreferredLocale must be a supported locale code | Validated against `LocalizationContext` supported locales |

---

## 8. Resource Aggregate

### 8.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| RS-INV-001 | Resource capacity must be positive | Factory method rejects `capacity <= 0` |
| RS-INV-002 | A `Decommissioned` resource cannot be allocated | `IsAvailableFor()` returns false for decommissioned resources |
| RS-INV-003 | A resource in `Maintenance` status cannot be allocated | `IsAvailableFor()` returns false during maintenance window |
| RS-INV-004 | Availability blocks cannot overlap | `Block()` validates no overlap with existing blocks |
| RS-INV-005 | ResourceCode is immutable after creation | No mutation method |

---

## 9. Recommendation Aggregate (AIRecommendation Context)

### 9.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| RC-INV-001 | ConfidenceScore must be between 0.0 and 1.0 inclusive | Factory method enforces range |
| RC-INV-002 | An `Expired` recommendation cannot be accepted or rejected | `Accept()` and `Reject()` throw if status is `Expired` |
| RC-INV-003 | ModelName and ModelVersion must not be null | Factory method enforces; enables traceability |
| RC-INV-004 | InputContext must be stored as a snapshot | Snapshot taken at creation time; never updated |
| RC-INV-005 | Recommendations expire after 60 minutes if not reviewed | Scheduled job marks `Expired`; prevents stale decisions |
| RC-INV-006 | Low-confidence recommendations (< 0.5) must be flagged | Factory method sets `RequiresManagerReview = true` if confidence < 0.5 |

---

## 10. LocaleConfiguration Aggregate

### 10.1 Hard Invariants

| ID | Invariant | Enforcement Point |
|----|-----------|-------------------|
| LC-INV-001 | LocaleCode must follow IETF format (e.g., en-US, ar-SA) | Factory method validates against regex pattern |
| LC-INV-002 | A deactivated locale must fall back to en-US | `Translate()` falls back to en-US locale if locale is inactive |
| LC-INV-003 | Translation keys must be non-empty | `UpdateTranslation()` rejects empty key |
| LC-INV-004 | Supported locales: en-US, ar-SA, hi-IN, fr-FR only (Phase 3) | Factory method rejects unsupported locale codes |
| LC-INV-005 | RTL flag is immutable after locale creation | Set based on locale code; Arabic/Hebrew/Urdu always RTL |

---

## 11. Invariant Enforcement Summary

### Implementation Rules

```csharp
// Rule 1: Every invariant violation throws a typed domain exception
// WRONG:
if (status == BookingStatus.Cancelled)
    return Result.Failure("Cannot confirm cancelled booking");

// CORRECT:
if (Status == BookingStatus.Cancelled)
    throw new InvalidBookingStateTransitionException(
        BookingId.Value, Status, BookingStatus.Confirmed);

// Rule 2: Invariants checked before state mutation
public void Confirm(string correlationId)
{
    // All invariant checks FIRST
    if (Status != BookingStatus.Draft && Status != BookingStatus.PendingValidation)
        throw new InvalidBookingStateTransitionException(BookingId.Value, Status, BookingStatus.Confirmed);
    if (!_passengers.Any(p => p.Type == PassengerType.Adult))
        throw new BookingRequiresAdultPassengerException(BookingId.Value);
    if (Category == TravelCategory.Religious && PilgrimageId == null)
        throw new PilgrimageBookingRequiresPilgrimageAssociationException(BookingId.Value);
    if (Category == TravelCategory.Group && GroupId == null)
        throw new GroupBookingRequiresGroupAssociationException(BookingId.Value);
    if (Itinerary.DepartureTime <= DateTime.UtcNow)
        throw new BookingDepartureAlreadyPassedException(BookingId.Value, Itinerary.DepartureTime);

    // THEN mutate
    Status = BookingStatus.Confirmed;
    UpdatedAt = DateTime.UtcNow;

    // THEN publish event
    AddDomainEvent(new BookingConfirmed(...));
}

// Rule 3: Every invariant has at least one unit test
// Test naming convention:
// [AggregateName]_[InvariantId]_[Condition]_[ExpectedOutcome]
// Example:
// Booking_BKINV006_WhenCancelled_ConfirmThrowsInvalidStateTransition()
// Booking_BKINV004_WhenNoAdultPassenger_ConfirmThrowsRequiresAdultPassenger()
```

### Test Coverage Requirement

**Every invariant in this document must have:**
1. A positive test (valid state passes)
2. A negative test (violation throws correct domain exception)
3. A boundary test (edge condition — e.g., exactly 0 passengers, exactly 100 priority score)

---

## 12. Cross-Invariant Dependency Map

```
Booking
  └── depends on → PilgrimageGroup (BK-INV-008)
  └── depends on → Group (BK-INV-009)
  └── depends on → Resource via AllocationDecision (BK-CINV-001)
  └── depends on → CostLedger (BK-CINV-002)

AllocationDecision
  └── depends on → Booking (AD-CINV-002)
  └── depends on → Resource (AD-INV-008)
  └── conflict check → AllocationDecision (AD-CINV-003)

CostLedger
  └── depends on → Booking (CL-CINV-001)
  └── depends on → Group members (CL-CINV-002)

PilgrimageGroup
  └── depends on → Booking (PG-CINV-001)
  └── depends on → Resource (Guide) (PG-CINV-002)
  └── depends on → Accommodation (PG-CINV-003)

Group
  └── depends on → Booking (GR-CINV-001)
  └── depends on → CostLedger (GR-CINV-002)

User
  └── depends on → Locale (US-INV-007)
```

---

## 13. Invariant Violation Catalog

All domain exceptions thrown by invariant violations:

```csharp
// Booking
BookingIdImmutableException
CurrencyImmutableAfterConfirmationException
BookingRequiresAdultPassengerException
PassengerCountMismatchException
InvalidBookingStateTransitionException
BookingAlreadyCompletedException
PilgrimageBookingRequiresPilgrimageAssociationException
GroupBookingRequiresGroupAssociationException
BookingPriceMustBePositiveException
BookingRouteOriginEqualsDestinationException
BookingOperatorIdRequiredException
BookingCategoryImmutableAfterConfirmationException
BookingAmendmentWindowExpiredException
BookingDepartureAlreadyPassedException

// AllocationDecision
AllocationBookingIdRequiredException
AllocationResourceIdRequiredException
AllocationPriorityScoreOutOfRangeException
AllocationRationaleRequiredException
AllocationManagerIdRequiredException
AllocationOverrideJustificationRequiredException
EscalatedAllocationCannotBeAutoResolvedException
AllocationResourceCapacityInsufficientException
ReleasedAllocationCannotBeReallocatedException

// CostLedger
CostShareSumMismatchException
CostShareNegativeAmountException
CostShareOverpaymentException
DisputedLedgerCannotBeSettledException
LedgerSettledIsImmutableException
MixedCurrencyInCostSharesException
CostLedgerRequiresAtLeastOneMemberException
RefundAfterDepartureProhibitedException

// PilgrimageGroup
PilgrimageRequiresPilgrimBeforeConfirmationException
PilgrimageRequiresGuideBeforeConfirmationException
PilgrimageComplianceCheckRequiredBeforeConfirmationException
PilgrimageComplianceCheckExpiredException
PrayerScheduleConflictException
SacredSiteAccessDeniedException
StrictCohesionGroupCannotSeparateException
PilgrimageLegOverlapException

// Group
GroupRequiresSingleCoordinatorException
GroupCoordinatorCannotBeRemovedWithoutReplacementException
DuplicateGroupMemberException
GroupCancelledCannotAcceptMembersException
GroupCompletedIsImmutableException

// Notification
NotificationRecipientRequiredException
NotificationMaxRetriesExceededException
NotificationAlreadySentIsImmutableException
NotificationPermanentlyFailedCannotRetryException
NotificationPIIInContentException

// User
UserEmailMustBeUniqueException
UserPasswordMustBeHashedException
LockedUserCannotAuthenticateException
UserDeactivatedCannotBeReactivatedWithoutAdminException
UnsupportedLocaleCodeException

// Resource
ResourceCapacityMustBePositiveException
DecommissionedResourceCannotBeAllocatedException
ResourceInMaintenanceCannotBeAllocatedException
ResourceAvailabilityBlocksOverlapException

// Recommendation
RecommendationConfidenceOutOfRangeException
ExpiredRecommendationCannotBeActedUponException
RecommendationModelNameRequiredException

// Localization
InvalidLocaleCodeFormatException
UnsupportedLocaleException
TranslationKeyCannotBeEmptyException
```

---

## 14. Document Sign-Off

| Attribute | Value |
|-----------|-------|
| Version | 1.0 |
| Status | LOCKED |
| Classification | Binding Implementation Contract |
| Test Coverage Requirement | 100% of listed invariants |
| Review | Architectural Stabilization Phase |
| Next Artifact | State Machine Definitions |

**This document is the implementation contract for all aggregate behavior.**
**Any code that violates an invariant listed here is a defect, not a design choice.**

---

**End of Aggregate Invariant Specification**
