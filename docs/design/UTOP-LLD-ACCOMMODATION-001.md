# Low-Level Design — Accommodation Context
**Document ID**: UTOP-LLD-ACCOMMODATION-001
**Version**: 1.2.0
**Status**: Baselined — corrected against implementation discoveries (2026-08-01 session). Ready for continued implementation.
**Context**: Accommodation
**Schema**: `utop_accommodation`
**Depends on**: UTOP-ARCH-001, UTOP-ARCH-003, UTOP-ARCH-004, UTOP-ARCH-005, UTOP-ARCH-006, UTOP-ARCH-007, UTOP-ARCH-008, UTOP-ARCH-009, UTOP-ARCH-010

---

## Changelog

| Version | Changes |
|---|---|
| 1.0.0 | Initial draft. **First full design of this aggregate** — no prior ARCH artifact (ADR-001, Domain Models §12, Aggregate Invariants, State Machines) carried anything beyond a one-line summary for Accommodation. This document originates the aggregate shape, state machine, invariants, and guest-data model, all previously undefined. Flagged for a review cycle before baseline, same discipline BOOKING-001 applied. |
| 1.1.0 | Review cycle 1: `LinkToPilgrimage` now takes `IClock` (no direct system-clock call in the domain); stopped stating `AccommodationAmendedIntegrationEvent` is published until ARCH-007 admits it; `Amend()` now rejects a new check-in already in the past (AC-TINV-004); `Create()` now validates `propertyExternalReference` and `Property.Code` (AC-INV-015, AC-INV-016); `Room` gained a `ProviderRoomReference` business identity with duplicate rejection on `AddRoom` (AC-INV-018) and duplicate-occupant rejection within a room (AC-INV-017); full EF Core configurations and PostgreSQL DDL added (§7); concurrency and persistence-mapping tests added (§10.2–10.3); UTC/`TIMESTAMPTZ`/`DATE` storage stated explicitly (§7.1). Cancellation cutoff (`UTOP-LLD-ACM-01`) and the two ARCH-007 governance gaps (`UTOP-LLD-ACM-02`, `UTOP-LLD-ACM-06`) remain open by decision — see §12. |
| 1.1.1 | `UTOP-LLD-ACM-07` closed — confirmed against the real Shared Kernel `Location` record (`Code`, `Type: LocationType`, `DisplayName?`). §7.2/§7.3 corrected to map all three fields instead of the placeholder that only mapped `Code`. |
| 1.2.0 | Implementation-discovered corrections (session 2026-08-01): `DateRange` does not exist in Shared Kernel (confirmed against the real file listing) — `Stay` replaced throughout (§3.1, §5, §7) with plain `CheckInDate`/`CheckOutDate` (`DateOnly`) properties and a computed `Nights` property, matching the raw-field precedent `UTOP.Booking`'s own `Itinerary` already established; property renamed `CheckInDate`/`CheckOutDate` (not `CheckIn`/`CheckOut`) specifically to avoid a name collision with the `CheckIn()`/`CheckOut()` methods (C# forbids a property and method sharing a name) — see Corrections table for the resulting `<Verb>Date` property / `<Verb>()` method convention; §6.3 inbound event handlers now use locally-owned contract types (`BookingCancelledInboundEvent`, `PilgrimageConfirmedInboundEvent` in `Infrastructure/Messaging/`) instead of implying direct import of Booking's/Pilgrimage's internal event types, per ARCH-008 bounded-context isolation; `PilgrimageConfirmedInboundEvent`'s shape is explicitly flagged speculative pending the (not-yet-written) Pilgrimage LLD. |

---

## Corrections to Prior Architecture Artifacts

| Item | Prior artifact | Correct (this document) | Authority |
|---|---|---|---|
| Aggregate root name | `Accommodation` (ARCH-001 bounded-context table) vs `AccommodationBooking` (ARCH-003 §12 summary) — the two prior artifacts disagree with each other | `AccommodationBooking` | This is a *reservation* of lodging tied to a travel booking, not the physical property. `Accommodation` alone would wrongly imply this context owns the hotel/room catalog. ARCH-006's `IAccommodationProvider` port (search/confirm against an external provider) confirms the catalog is external — this context owns the reservation lifecycle only, exactly the same relationship `Booking` has to journeys. `AccommodationBooking` is adopted as the correct, unambiguous name. |
| `CreatedAt`/`UpdatedAt` type | `DateTime` (ARCH-003) | `DateTimeOffset` | ARCH-009 §2.3 — same base-class correction BOOKING-001 already applied platform-wide. |
| `CorrelationId` type | Not modeled for this context previously | `CorrelationId` struct (Shared Kernel) | ARCH-010 §5.4 |
| Concurrency field | Not modeled for this context previously | `long Version`, optimistic concurrency | ARCH-006 §5.2 — reusing the resolved pattern rather than reopening BOOKING-001's open item (`UTOP-LLD-BK-05`) a second time. |
| `AccommodationAmendedIntegrationEvent` | ARCH-008 §2 lists it under "Publishes" | **Not currently a registered event.** ARCH-007 §4.2 (Event Ownership Register) contains only `AccommodationBookedIntegrationEvent` and `AccommodationCancelledIntegrationEvent` for this context — no amended entry exists. Two prior artifacts disagree with each other, same class of conflict as the aggregate-name issue above. | ARCH-007 §4.3 rule 4: *"A new event type requires an entry in this register before implementation."* Treated here as **not yet authorized** — see Open Item UTOP-LLD-ACM-02. |
| Pilgrimage as a consumer of `AccommodationBookedIntegrationEvent` | ARCH-008 §4 states Pilgrimage consumes this event "to verify sacred site proximity" — a hard saga dependency | ARCH-007 §4.2's "Allowed Consumers" column for `AccommodationBookedIntegrationEvent` lists only **Notification, Analytics** — Pilgrimage is absent | A third artifact disagreement. Pilgrimage's saga step depends on this consumption working, so this isn't cosmetic — it's a missing authorization the Architecture Board needs to close. See Open Item UTOP-LLD-ACM-06. |
| `Stay` (`DateRange`) | §3.1/§5/§7 originally typed `Stay` as `DateRange`, assumed to exist in Shared Kernel — **partially correct**: `DateRange` was in fact ratified in ARCH-010 §5.2, just never implemented as an actual file in `UTOP.Shared`. Discovered mid-implementation and initially misdiagnosed as "never existed." | `DateRange` retired from ARCH-010 (v1.0.2) rather than implemented to match — the only two contexts to reach implementation (Booking's `Itinerary`, Accommodation) both ended up not needing it. `Stay` replaced with plain `CheckInDate`/`CheckOutDate` (`DateOnly`) properties and a computed `Nights` property. | Architect decision: keep the already-committed implementation (`feature/implementation` commit `97f6838`) rather than revert working code to match a type nothing else uses. See ARCH-010 §5.2 amendment log. |
| Property naming: `CheckInDate`/`CheckOutDate` vs. `CheckIn()`/`CheckOut()` | Not applicable — didn't exist as an issue before `Stay` was decomposed into date properties | Property named `<Verb>Date`, method retained as `<Verb>()` — e.g. `CheckInDate` (property) / `CheckIn()` (method) | C# forbids a property and a method sharing a name (CS0102) — this combination surfaced only once `Stay.Start`/`Stay.End` became direct `CheckIn`/`CheckOut` properties, colliding with the existing `CheckIn()`/`CheckOut()` state-transition methods. Adopted here as a standing convention for any future context with a similar date-plus-verb pairing. |
| Inbound integration event types (§6.3) | Handler pseudocode referenced `BookingCancelledIntegrationEvent`/`PilgrimageConfirmedIntegrationEvent` directly, implying these are importable, shared types | Each inbound message gets a locally-owned contract in `Infrastructure/Messaging/` (`BookingCancelledInboundEvent`, `PilgrimageConfirmedInboundEvent`), decoupled from how the producing context internally models the same event | A consuming context importing a producing context's internal event type would cross the Application/Infrastructure boundary and violate ARCH-008 bounded-context isolation. Standard anti-corruption-layer pattern — worth stating explicitly in a future ARCH artifact so subsequent LLDs' handler pseudocode doesn't repeat the implication. Not yet verified whether Booking's own inbound handlers (if any) already follow this pattern — flag for future check, not asserted here. |

---

## 1. Context Responsibility

The Accommodation context owns the full lifecycle of a lodging reservation: creation, room assignment, ancillary service booking, pricing, check-in, check-out, and cancellation.

It does **not** own the property or room catalog — search and confirmation against real hotel inventory happens through `IAccommodationProvider`, an external port (ARCH-006), the same relationship `Booking` has with `IBookingProvider`. It does **not** make resource allocation decisions — that is ResourceAllocation's job. It does **not** determine pilgrimage compliance, sacred site access rules, or own any logic in `utop_pilgrimage` — ARCH-008 §2 explicitly forbids both. Where an accommodation reservation matters to a pilgrimage, the relationship runs *from* Pilgrimage: Pilgrimage consumes `AccommodationBookedIntegrationEvent` to verify site proximity externally (ARCH-008 §4), not the other way around.

**Booking association (per ARCH-008 §2 "May Reference"):** every `AccommodationBooking` carries a mandatory `BookingId` — an opaque reference to the `Booking` aggregate it belongs to. Both entry points into this context (operator adding accommodation to an active booking; the Pilgrimage saga's `BookAccommodationNearSite` step, which runs *after* `BookMultiLegJourney` per ADR-004) already have a confirmed `Booking` in hand by the time an accommodation reservation is created. `BookingId` is therefore required at `Create()`, not optional.

**Pilgrimage association is asymmetric and passive.** ARCH-008 §2 lists `PilgrimageConfirmedIntegrationEvent` as something this context *consumes*, "to link accommodation to pilgrimage group." That link is a read-only correlation field (`LinkedPilgrimageId`), populated by an inbound event handler after the fact — it carries no business logic, gates no invariant, and is never queried back into `utop_pilgrimage`. This context does not decide pilgrimage compliance; it just remembers which pilgrimage group asked, so downstream reads (analytics, notifications) don't have to guess.

**Sacred site proximity** — ARCH-008 §2 also lists a read-only reference to "sacred site proximity data for pilgrimage (via PilgrimageContext service interface)". This context queries that interface at booking time *only* when the associated booking is pilgrimage-category, to validate the selected property before confirming — it never reads Pilgrimage's schema directly.

---

## 2. Solution Structure

```
UTOP.Accommodation/
├── Domain/
│   ├── Aggregates/
│   │   └── AccommodationBooking.cs
│   ├── Entities/
│   │   ├── Room.cs
│   │   ├── Occupant.cs
│   │   └── AncillaryService.cs
│   ├── ValueObjects/
│   │   ├── AccommodationBookingId.cs
│   │   ├── AccommodationBookingStatus.cs   (enum)
│   │   ├── RoomType.cs                     (enum)
│   │   ├── OccupantType.cs                 (enum)
│   │   └── AncillaryServiceType.cs         (enum)
│   ├── Events/
│   │   ├── AccommodationBookingCreated.cs
│   │   ├── AccommodationBookingConfirmed.cs
│   │   ├── AccommodationBookingAmended.cs
│   │   ├── AccommodationBookingCancelled.cs
│   │   ├── AccommodationGuestCheckedIn.cs
│   │   ├── AccommodationGuestCheckedOut.cs
│   │   └── AccommodationNoShowRecorded.cs
│   ├── Exceptions/
│   │   └── (all listed in §12)
│   ├── Services/
│   │   └── (none — all logic belongs in the aggregate)
│   └── Repositories/
│       ├── IAccommodationBookingRepository.cs
│       └── IAccommodationBookingReadRepository.cs
├── Application/
│   ├── Commands/
│   │   ├── CreateAccommodationBookingCommand.cs + Handler
│   │   ├── ConfirmAccommodationBookingCommand.cs + Handler
│   │   ├── AmendAccommodationBookingCommand.cs + Handler
│   │   ├── CancelAccommodationBookingCommand.cs + Handler
│   │   ├── CheckInCommand.cs + Handler
│   │   ├── CheckOutCommand.cs + Handler
│   │   └── RecordNoShowCommand.cs + Handler
│   ├── Queries/
│   │   ├── GetAccommodationBookingByIdQuery.cs + Handler
│   │   └── GetAccommodationBookingsByBookingIdQuery.cs + Handler
│   ├── EventHandlers/
│   │   ├── BookingCancelledEventHandler.cs         (inbound — releases the hold)
│   │   └── PilgrimageConfirmedEventHandler.cs      (inbound — sets LinkedPilgrimageId only)
│   └── Ports/
│       ├── IAccommodationProvider.cs               (ARCH-006, reused verbatim)
│       └── ISacredSiteProximityProvider.cs         (read-only; Pilgrimage service interface)
├── Infrastructure/
│   ├── Persistence/
│   │   ├── AccommodationDbContext.cs
│   │   ├── AccommodationBookingRepository.cs
│   │   ├── AccommodationBookingReadRepository.cs
│   │   ├── Migrations/
│   │   └── Configurations/
│   │       ├── AccommodationBookingConfiguration.cs
│   │       ├── RoomConfiguration.cs
│   │       ├── OccupantConfiguration.cs
│   │       └── AncillaryServiceConfiguration.cs
│   ├── Messaging/
│   │   ├── AccommodationEventPublisher.cs
│   │   └── OutboxProcessor.cs                       (shared platform pattern — UTOP-LLD-BK-04)
│   └── Adapters/
│       ├── StubAccommodationProvider.cs
│       └── StubSacredSiteProximityProvider.cs
```

---

## 3. Aggregate Design — AccommodationBooking

### 3.1 AccommodationBooking (Aggregate Root)

```csharp
namespace UTOP.Accommodation.Domain.Aggregates;

public sealed class AccommodationBooking : AggregateRoot
{
    public AccommodationBookingId AccommodationBookingId { get; private set; } = null!;
    public string BookingId { get; private set; } = null!;          // mandatory opaque reference — ARCH-008 §2
    public string? LinkedPilgrimageId { get; private set; }         // optional, read-only correlation — set only via event
    public Location Property { get; private set; } = null!;         // Shared Kernel; external property location
    public string PropertyExternalReference { get; private set; } = null!;  // IAccommodationProvider's identifier
    public DateOnly CheckInDate { get; private set; }                // Shared Kernel has no DateRange type — see Corrections table
    public DateOnly CheckOutDate { get; private set; }
    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
    public Money TotalPrice { get; private set; } = null!;
    public AccommodationBookingStatus Status { get; private set; }
    public string PrimaryGuestName { get; private set; } = null!;
    public int AmendmentVersion { get; private set; }
    public long Version { get; private set; }                       // optimistic concurrency — ARCH-006 §5.2

    private readonly List<Room> _rooms = new();
    public IReadOnlyList<Room> Rooms => _rooms.AsReadOnly();
    private readonly List<AncillaryService> _ancillaryServices = new();
    public IReadOnlyList<AncillaryService> AncillaryServices => _ancillaryServices.AsReadOnly();

    private AccommodationBooking() { } // EF Core constructor

    /// <summary>
    /// Factory. Creates a reservation in Requested status.
    /// AC-INV-001: price must be positive.
    /// AC-INV-002: stay must be at least one night.
    /// AC-INV-003: BookingId is mandatory.
    /// AC-TINV-001: check-in must not already have passed.
    /// IClock is injected — never calls DateTime.UtcNow directly (ARCH-009 §3).
    /// </summary>
    public static AccommodationBooking Create(
        string bookingId,
        Location property,
        string propertyExternalReference,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        Money price,
        string primaryGuestName,
        CorrelationId correlationId,
        IClock clock)
    {
        // AC-INV-003
        if (string.IsNullOrWhiteSpace(bookingId))
            throw new AccommodationBookingIdRequiredException();

        // AC-INV-015 — external provider identifier is mandatory; without it, later
        // Confirm/Cancel calls against IAccommodationProvider have nothing to reference.
        if (string.IsNullOrWhiteSpace(propertyExternalReference))
            throw new PropertyExternalReferenceRequiredException();

        // AC-INV-016 — property must carry a valid identity before persisting a
        // reservation against it (prevents an accommodation with no resolvable property).
        if (property is null || string.IsNullOrWhiteSpace(property.Code))
            throw new InvalidPropertyIdentityException();

        // AC-INV-001
        if (price.Amount <= 0)
            throw new AccommodationPriceMustBePositiveException();

        // AC-INV-002
        if (checkOutDate.DayNumber - checkInDate.DayNumber < 1)
            throw new InvalidStayDurationException(checkInDate, checkOutDate);

        if (string.IsNullOrWhiteSpace(primaryGuestName))
            throw new ArgumentException("Primary guest name is required.", nameof(primaryGuestName));

        // AC-TINV-001
        var checkInUtc = checkInDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (checkInUtc <= clock.UtcNow.UtcDateTime)
            throw new AccommodationCheckInAlreadyPassedException(checkInDate, clock.UtcNow);

        var now = clock.UtcNow;

        var booking = new AccommodationBooking
        {
            Id = Guid.NewGuid(),
            AccommodationBookingId = AccommodationBookingId.Generate(DateOnly.FromDateTime(now.UtcDateTime)),
            BookingId = bookingId,
            Property = property,
            PropertyExternalReference = propertyExternalReference,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            TotalPrice = price,
            PrimaryGuestName = primaryGuestName,
            Status = AccommodationBookingStatus.Requested,
            AmendmentVersion = 0,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        booking.AddDomainEvent(new AccommodationBookingCreated(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: booking.Id.ToString(),
            AggregateType: nameof(AccommodationBooking),
            AccommodationBookingId: booking.AccommodationBookingId,
            BookingId: bookingId,
            Property: property,
            CheckInDate: checkInDate,
            CheckOutDate: checkOutDate,
            TotalPrice: price,
            OccurredAt: now));

        return booking;
    }

    /// <summary>
    /// Confirms the reservation once the external provider has confirmed availability.
    /// AC-INV-004: at least one room must be assigned.
    /// AC-INV-005: at least one occupant across all rooms.
    /// Transitions: REQUESTED → CONFIRMED
    /// </summary>
    public void Confirm(CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.Requested)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.Confirmed);

        // AC-INV-004
        if (_rooms.Count == 0)
            throw new AccommodationRequiresRoomException(AccommodationBookingId);

        // AC-INV-005
        if (_rooms.Sum(r => r.OccupantCount) == 0)
            throw new AccommodationRequiresOccupantException(AccommodationBookingId);

        var now = clock.UtcNow;
        Status = AccommodationBookingStatus.Confirmed;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationBookingConfirmed(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(AccommodationBooking),
            AccommodationBookingId: AccommodationBookingId,
            BookingId: BookingId,
            TotalPrice: TotalPrice,
            OccurredAt: now));
    }

    /// <summary>
    /// Amends stay dates and/or price of a confirmed reservation.
    /// AC-INV-006: currency must not change after confirmation.
    /// AC-TINV-004: new check-in must not already be in the past (mirrors AC-TINV-001 on Create).
    /// AC-INV-014: stay is replaced atomically, never partially mutated.
    /// Transitions: CONFIRMED → CONFIRMED (state unchanged; version increments)
    /// </summary>
    public void Amend(DateOnly newCheckInDate, DateOnly newCheckOutDate, Money newPrice, CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.Confirmed)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.Confirmed);

        if (newCheckOutDate.DayNumber - newCheckInDate.DayNumber < 1)
            throw new InvalidStayDurationException(newCheckInDate, newCheckOutDate);

        // AC-TINV-004
        var newCheckInUtc = newCheckInDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (newCheckInUtc <= clock.UtcNow.UtcDateTime)
            throw new AccommodationCheckInAlreadyPassedException(newCheckInDate, clock.UtcNow);

        // AC-INV-006
        if (newPrice.Currency != TotalPrice.Currency)
            throw new AccommodationCurrencyImmutableAfterConfirmationException(AccommodationBookingId, TotalPrice.Currency, newPrice.Currency);

        var previousCheckInDate = CheckInDate;
        var previousCheckOutDate = CheckOutDate;
        var previousPrice = TotalPrice;
        var now = clock.UtcNow;

        CheckInDate = newCheckInDate;      // AC-INV-014: atomic replacement
        CheckOutDate = newCheckOutDate;
        TotalPrice = newPrice;
        AmendmentVersion++;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationBookingAmended(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(AccommodationBooking),
            AccommodationBookingId: AccommodationBookingId,
            AmendmentVersion: AmendmentVersion,
            PreviousCheckInDate: previousCheckInDate,
            PreviousCheckOutDate: previousCheckOutDate,
            NewCheckInDate: newCheckInDate,
            NewCheckOutDate: newCheckOutDate,
            PreviousPrice: previousPrice,
            NewPrice: newPrice,
            OccurredAt: now));
    }

    /// <summary>
    /// Cancels the reservation. Idempotent if already Cancelled.
    /// Cannot cancel once CheckedIn, CheckedOut, or NoShow (AC-INV-007).
    /// AC-TINV-002: forbidden within the configurable cancellation cutoff before check-in.
    /// Transitions: REQUESTED | CONFIRMED → CANCELLED
    /// </summary>
    public void Cancel(string reason, TimeSpan cancellationCutoff, CorrelationId correlationId, IClock clock)
    {
        // Idempotent
        if (Status == AccommodationBookingStatus.Cancelled)
            return;

        // AC-INV-007
        if (Status is AccommodationBookingStatus.CheckedIn or AccommodationBookingStatus.CheckedOut or AccommodationBookingStatus.NoShow)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.Cancelled);

        // AC-TINV-002 — cutoff is a configurable operational default (ARCH decision precedent: BOOKING-001 Decision 6).
        // Actual cutoff hours value is an open item — see §12.
        if (Status == AccommodationBookingStatus.Confirmed)
        {
            var checkInUtc = new DateTimeOffset(CheckInDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var timeUntilCheckIn = checkInUtc - clock.UtcNow;
            if (timeUntilCheckIn <= cancellationCutoff)
                throw new AccommodationCancellationWindowExpiredException(AccommodationBookingId, checkInUtc, clock.UtcNow);
        }

        var now = clock.UtcNow;
        Status = AccommodationBookingStatus.Cancelled;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationBookingCancelled(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(AccommodationBooking),
            AccommodationBookingId: AccommodationBookingId,
            Reason: reason,
            OccurredAt: now));
    }

    /// <summary>
    /// Records guest check-in.
    /// AC-TINV-003: cannot check in before the stay's check-in date.
    /// Property/method naming: CheckInDate (property) vs CheckIn() (method) — deliberately
    /// distinct names, since C# forbids a property and method sharing a name (CS0102).
    /// Standing convention going forward for any context with a similar date-plus-verb pairing.
    /// Transitions: CONFIRMED → CHECKED_IN
    /// </summary>
    public void CheckIn(CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.Confirmed)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.CheckedIn);

        var now = clock.UtcNow;
        var checkInDateUtc = new DateTimeOffset(CheckInDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // AC-TINV-003
        if (now < checkInDateUtc)
            throw new AccommodationCheckInTooEarlyException(AccommodationBookingId, checkInDateUtc, now);

        Status = AccommodationBookingStatus.CheckedIn;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationGuestCheckedIn(
            Guid.NewGuid(), correlationId, Id.ToString(), nameof(AccommodationBooking), AccommodationBookingId, now));
    }

    /// <summary>
    /// Records guest check-out. Terminal.
    /// Transitions: CHECKED_IN → CHECKED_OUT
    /// </summary>
    public void CheckOut(CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.CheckedIn)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.CheckedOut);

        var now = clock.UtcNow;
        Status = AccommodationBookingStatus.CheckedOut;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationGuestCheckedOut(
            Guid.NewGuid(), correlationId, Id.ToString(), nameof(AccommodationBooking), AccommodationBookingId, now));
    }

    /// <summary>
    /// Records a no-show — guest never checked in after the stay's check-in date passed.
    /// Operator/manager-triggered, not automatic (no background clock check inside the domain).
    /// Transitions: CONFIRMED → NO_SHOW
    /// </summary>
    public void RecordNoShow(CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.Confirmed)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.NoShow);

        var now = clock.UtcNow;
        var checkInDateUtc = new DateTimeOffset(CheckInDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        if (now <= checkInDateUtc)
            throw new AccommodationCheckInTooEarlyException(AccommodationBookingId, checkInDateUtc, now);

        Status = AccommodationBookingStatus.NoShow;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationNoShowRecorded(
            Guid.NewGuid(), correlationId, Id.ToString(), nameof(AccommodationBooking), AccommodationBookingId, now));
    }

    /// <summary>
    /// Sets the pilgrimage correlation reference. Called only by PilgrimageConfirmedEventHandler.
    /// Not a business rule — passive read-only correlation per ARCH-008 §2.
    /// IClock injected per ARCH-009 — this aggregate never calls the system clock directly,
    /// even for a non-invariant-bearing timestamp like this one.
    /// </summary>
    public void LinkToPilgrimage(string pilgrimageId, IClock clock)
    {
        LinkedPilgrimageId = pilgrimageId;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Adds a room to the reservation. Idempotent on duplicate internal Id;
    /// AC-INV-018 rejects a second room carrying the same ProviderRoomReference
    /// (the same physical room cannot be assigned twice to one reservation).
    /// AC-INV-009: room rate must be positive.
    /// </summary>
    public void AddRoom(Room room)
    {
        if (_rooms.Any(r => r.Id == room.Id))
            return;

        if (_rooms.Any(r => r.ProviderRoomReference == room.ProviderRoomReference))
            throw new DuplicateRoomException(room.ProviderRoomReference);

        _rooms.Add(room);
    }

    /// <summary>
    /// Adds an ancillary service. AC-INV-008: quantity ≥ 1, price ≥ 0 (complimentary services allowed).
    /// </summary>
    public void AddAncillaryService(AncillaryService service)
    {
        if (_ancillaryServices.Any(s => s.Id == service.Id))
            return;

        _ancillaryServices.Add(service);
    }
}
```

### 3.2 AccommodationBookingStatus

```csharp
namespace UTOP.Accommodation.Domain.ValueObjects;

/// <summary>
/// First full state machine defined for this context — no prior ARCH-005 entry existed.
/// Follows the same discipline as the Booking machine: guard clauses first, every
/// forbidden transition throws a specific exception, idempotency required on Cancel.
/// </summary>
public enum AccommodationBookingStatus
{
    Requested,   // Created; awaiting external provider confirmation
    Confirmed,   // Provider confirmed; awaiting stay
    CheckedIn,   // Guest has arrived
    CheckedOut,  // Stay complete — terminal
    Cancelled,   // Cancelled before or after confirmation — terminal
    NoShow       // Guest never arrived — terminal
}
```

**State diagram:**

```
Requested ──Confirm()──▶ Confirmed ──CheckIn()──▶ CheckedIn ──CheckOut()──▶ CheckedOut (terminal)
    │                        │
    │                        ├──RecordNoShow()──▶ NoShow (terminal)
    │                        │
    Cancel()                Cancel()
    │                        │
    ▼                        ▼
Cancelled (terminal)   Cancelled (terminal)
```

**Forbidden transitions** (all others not listed above throw `InvalidAccommodationStateTransitionException`):
- Any mutation from `CheckedOut`, `Cancelled`, or `NoShow` (terminal states)
- `CheckIn()` from `Requested` (must be `Confirmed` first)
- `CheckOut()` from anything other than `CheckedIn`
- `RecordNoShow()` from anything other than `Confirmed`

### 3.3 AccommodationBookingId

```csharp
namespace UTOP.Accommodation.Domain.ValueObjects;

/// <summary>
/// Format: ACM-{YYYYMMDD}-{4-char hex suffix}. Example: ACM-20260715-F2A9.
/// Immutable after creation. Generate() accepts DateOnly from IClock.UtcNow —
/// never calls DateTime.UtcNow internally (ARCH-009 §3, same discipline as BookingId).
/// </summary>
public sealed record AccommodationBookingId(string Value)
{
    public static AccommodationBookingId Generate(DateOnly date)
    {
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return new AccommodationBookingId($"ACM-{date:yyyyMMdd}-{suffix}");
    }

    public override string ToString() => Value;
}
```

---

## 4. Entity Design

### 4.1 Room

```csharp
namespace UTOP.Accommodation.Domain.Entities;

/// <summary>
/// A room line-item within the reservation. Rate is per-night; total room cost is
/// derived (RatePerNight × Nights), never stored redundantly.
/// AC-INV-009: RatePerNight must be positive.
/// </summary>
public sealed class Room : Entity
{
    public RoomType Type { get; private set; }
    public Money RatePerNight { get; private set; } = null!;
    public string ProviderRoomReference { get; private set; } = null!;  // business identity — the actual room the provider assigned; distinct from the internal entity Id
    private readonly List<Occupant> _occupants = new();
    public IReadOnlyList<Occupant> Occupants => _occupants.AsReadOnly();
    public int OccupantCount => _occupants.Count;

    private Room() { }

    public static Room Create(RoomType type, Money ratePerNight, string providerRoomReference)
    {
        if (ratePerNight.Amount <= 0)
            throw new InvalidRoomRateException(ratePerNight);
        if (string.IsNullOrWhiteSpace(providerRoomReference))
            throw new ArgumentException("Provider room reference is required.", nameof(providerRoomReference));

        return new Room { Id = Guid.NewGuid(), Type = type, RatePerNight = ratePerNight, ProviderRoomReference = providerRoomReference };
    }

    /// <summary>
    /// AC-INV-017: rejects a duplicate occupant within the same room, keyed on
    /// Name + OccupantType (this context has no document number to key on — see
    /// the lightweight-Occupant decision in §4.2).
    /// </summary>
    public void AddOccupant(Occupant occupant)
    {
        if (_occupants.Any(o => o.Id == occupant.Id))
            return;

        if (_occupants.Any(o => o.Name.Equals(occupant.Name, StringComparison.OrdinalIgnoreCase) && o.Type == occupant.Type))
            throw new DuplicateOccupantException(occupant.Name);

        _occupants.Add(occupant);
    }
}

public enum RoomType { Single, Double, Twin, Suite, Family }
```

### 4.2 Occupant

```csharp
namespace UTOP.Accommodation.Domain.Entities;

/// <summary>
/// Deliberately lightweight — name and type only, no document number or date of birth.
/// Architecture decision: Booking's Passenger entity carries travel-document PII because
/// it needs it for the journey itself; a hotel guest register does not need that here,
/// and duplicating Booking's PII-encryption debt (UTOP-LLD-BK-01) without a concrete
/// requirement would be over-engineering. If a future requirement needs document capture
/// for hotel check-in, extend this entity then — don't carry the cost now on spec.
/// </summary>
public sealed class Occupant : Entity
{
    public string Name { get; private set; } = null!;
    public OccupantType Type { get; private set; }

    private Occupant() { }

    public static Occupant Create(string name, OccupantType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Occupant name is required.", nameof(name));

        return new Occupant { Id = Guid.NewGuid(), Name = name, Type = type };
    }
}

public enum OccupantType { Adult, Child }
```

### 4.3 AncillaryService

```csharp
namespace UTOP.Accommodation.Domain.Entities;

/// <summary>
/// Transfers, meals, excursions — per ARCH-001's context description.
/// AC-INV-008: Quantity ≥ 1; Price ≥ 0 (complimentary services, e.g. breakfast, are valid).
/// </summary>
public sealed class AncillaryService : Entity
{
    public AncillaryServiceType Type { get; private set; }
    public string Description { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int Quantity { get; private set; }

    private AncillaryService() { }

    public static AncillaryService Create(AncillaryServiceType type, string description, Money price, int quantity)
    {
        if (quantity < 1)
            throw new InvalidAncillaryServiceException("Quantity must be at least 1.");
        if (price.Amount < 0)
            throw new InvalidAncillaryServiceException("Price cannot be negative.");
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return new AncillaryService { Id = Guid.NewGuid(), Type = type, Description = description, Price = price, Quantity = quantity };
    }

    public Money LineTotal => Price.Multiply(Quantity);
}

public enum AncillaryServiceType { Transfer, Meal, Excursion, SpaTreatment, Other }
```

---

## 5. Domain Events

All events inherit from `DomainEvent` (Shared Kernel). `OccurredAt` is `DateTimeOffset` UTC. `CorrelationId` is the Shared Kernel struct.

```csharp
namespace UTOP.Accommodation.Domain.Events;

public sealed record AccommodationBookingCreated(
    Guid EventId, CorrelationId CorrelationId, string AggregateId, string AggregateType,
    AccommodationBookingId AccommodationBookingId, string BookingId, Location Property,
    DateOnly CheckInDate, DateOnly CheckOutDate, Money TotalPrice, DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationBookingConfirmed(
    Guid EventId, CorrelationId CorrelationId, string AggregateId, string AggregateType,
    AccommodationBookingId AccommodationBookingId, string BookingId, Money TotalPrice,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationBookingAmended(
    Guid EventId, CorrelationId CorrelationId, string AggregateId, string AggregateType,
    AccommodationBookingId AccommodationBookingId, int AmendmentVersion,
    DateOnly PreviousCheckInDate, DateOnly PreviousCheckOutDate,
    DateOnly NewCheckInDate, DateOnly NewCheckOutDate, Money PreviousPrice, Money NewPrice,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationBookingCancelled(
    Guid EventId, CorrelationId CorrelationId, string AggregateId, string AggregateType,
    AccommodationBookingId AccommodationBookingId, string Reason, DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationGuestCheckedIn(
    Guid EventId, CorrelationId CorrelationId, string AggregateId, string AggregateType,
    AccommodationBookingId AccommodationBookingId, DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationGuestCheckedOut(
    Guid EventId, CorrelationId CorrelationId, string AggregateId, string AggregateType,
    AccommodationBookingId AccommodationBookingId, DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationNoShowRecorded(
    Guid EventId, CorrelationId CorrelationId, string AggregateId, string AggregateType,
    AccommodationBookingId AccommodationBookingId, DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);
```

---

## 6. Application Layer

### 6.1 Commands and Handlers

```
CreateAccommodationBookingCommand(BookingId, PropertySearchCriteria, Stay, GuestName, CorrelationId)
  → CreateAccommodationBookingCommandHandler
      1. Call IAccommodationProvider.SearchAccommodationsAsync — get property + rate
      2. If associated Booking is pilgrimage-category (checked via BookingConfirmedIntegrationEvent
         payload cached at consume time — never a live query into utop_booking): call
         ISacredSiteProximityProvider to validate proximity before proceeding
      3. AccommodationBooking.Create(...)
      4. IAccommodationBookingRepository.SaveAsync()
      5. Publish AccommodationBookingCreated (domain-internal only — not on the integration
         event register; ARCH-007 §4.2 registers only Booked and Cancelled for this
         context, see Corrections table and Open Item UTOP-LLD-ACM-02)

ConfirmAccommodationBookingCommand(AccommodationBookingId, RoomSelections, CorrelationId, ExpectedVersion)
  → ConfirmAccommodationBookingCommandHandler
      1. Load aggregate; check ExpectedVersion (ARCH-006 §5.2)
      2. AddRoom() for each selection (RoomSelections now carry ProviderRoomReference,
         per Room's updated Create signature — see §4.1), AddOccupant() for each guest
      3. Call IAccommodationProvider.ConfirmAccommodationAsync
      4. booking.Confirm()
      5. Persist; publish AccommodationBookedIntegrationEvent (outbound, registered in ARCH-007 §4.2)

AmendAccommodationBookingCommand(AccommodationBookingId, NewCheckInDate, NewCheckOutDate, NewPrice, CorrelationId, ExpectedVersion)
  → AmendAccommodationBookingCommandHandler
      1. Load; check version; booking.Amend(...)
      2. Persist domain event only. Do NOT publish an integration event for this
         transition until AccommodationAmendedIntegrationEvent is admitted to ARCH-007's
         register (Open Item UTOP-LLD-ACM-02) — publishing it today would be an
         unauthorized event per ARCH-007 §4.3 rule 4.

CancelAccommodationBookingCommand(AccommodationBookingId, Reason, CorrelationId, ExpectedVersion)
  → CancelAccommodationBookingCommandHandler
      1. Load; check version; booking.Cancel(reason, cancellationCutoff, ...)
         (cancellationCutoff injected from configuration — see §13 open item)
      2. Persist; publish AccommodationCancelledIntegrationEvent (outbound, sanctioned)

CheckInCommand(AccommodationBookingId, CorrelationId, ExpectedVersion)
  → CheckInCommandHandler → booking.CheckIn(...) → persist
      (domain-internal event only — see §11 open item on whether this needs a new
      integration event for Notifications)

CheckOutCommand(AccommodationBookingId, CorrelationId, ExpectedVersion)
  → CheckOutCommandHandler → booking.CheckOut(...) → persist

RecordNoShowCommand(AccommodationBookingId, CorrelationId, ExpectedVersion)
  → RecordNoShowCommandHandler → booking.RecordNoShow(...) → persist
```

### 6.2 Queries

```
GetAccommodationBookingByIdQuery(AccommodationBookingId) → GetAccommodationBookingByIdQueryHandler
GetAccommodationBookingsByBookingIdQuery(BookingId)       → GetAccommodationBookingsByBookingIdQueryHandler
```

### 6.3 Inbound Event Handlers

**Local contract ownership — corrected per implementation discovery (see Corrections table).**
A consuming context must not import a producing context's internal event type — doing so
would violate ARCH-008 bounded-context isolation and cross the Application/Infrastructure
layer boundary. `UTOP.Accommodation` defines its own local contract for each inbound message
shape, decoupled from how Booking (or, eventually, Pilgrimage) internally models the same
event — standard anti-corruption-layer pattern. These local contracts live in
`Infrastructure/Messaging/AccommodationInboundEvents.cs`.

```csharp
namespace UTOP.Accommodation.Infrastructure.Messaging;

/// <summary>Local shape of Booking's outbound event — not a shared type with UTOP.Booking.</summary>
public sealed record BookingCancelledInboundEvent(string BookingId, CorrelationId CorrelationId);

/// <summary>
/// Local shape of Pilgrimage's outbound event. SPECULATIVE — UTOP.Pilgrimage does not exist
/// as an implemented context yet and no Pilgrimage LLD exists to confirm this shape against.
/// Confirm and correct when the Pilgrimage LLD is written (see Open Items).
/// </summary>
public sealed record PilgrimageConfirmedInboundEvent(
    string PilgrimageId, IReadOnlyList<string> PilgrimBookingIds, CorrelationId CorrelationId);
```

```csharp
namespace UTOP.Accommodation.Application.EventHandlers;

/// <summary>
/// Releases the accommodation hold when the associated travel booking is cancelled.
/// Consumed per ARCH-008 §2. If no matching AccommodationBooking is found (e.g. one
/// was never created for this BookingId), the handler is a no-op.
/// </summary>
public sealed class BookingCancelledEventHandler
{
    public async Task HandleAsync(BookingCancelledInboundEvent evt, CancellationToken ct)
    {
        var bookings = await _readRepository.GetByBookingIdAsync(evt.BookingId, ct);
        foreach (var booking in bookings.Where(b => b.Status is AccommodationBookingStatus.Requested or AccommodationBookingStatus.Confirmed))
        {
            booking.Cancel("Associated booking was cancelled.", TimeSpan.Zero, evt.CorrelationId, _clock);
            await _repository.SaveAsync(booking, ct);
        }
    }
}

/// <summary>
/// Sets the passive pilgrimage correlation reference. No business logic — per ARCH-008 §2,
/// this context never owns pilgrimage compliance decisions.
/// </summary>
public sealed class PilgrimageConfirmedEventHandler
{
    public async Task HandleAsync(PilgrimageConfirmedInboundEvent evt, CancellationToken ct)
    {
        var bookings = await _readRepository.GetByBookingIdsAsync(evt.PilgrimBookingIds, ct);
        foreach (var booking in bookings)
        {
            booking.LinkToPilgrimage(evt.PilgrimageId, _clock);
            await _repository.SaveAsync(booking, ct);
        }
    }
}
```

### 6.4 Ports

```csharp
namespace UTOP.Accommodation.Application.Ports;

// Reused verbatim from ARCH-006 — no changes needed.
public interface IAccommodationProvider
{
    Task<IEnumerable<AccommodationOption>> SearchAccommodationsAsync(AccommodationSearchRequest request);
    Task<AccommodationConfirmation> ConfirmAccommodationAsync(AccommodationRequest request);
}

// New — exposes Pilgrimage's read-only proximity service per ARCH-008 §2.
// This is a service-interface call, not a schema read — respects the bounded-context boundary.
public interface ISacredSiteProximityProvider
{
    Task<bool> IsWithinAcceptableProximityAsync(Location property, string sacredSiteId, CancellationToken ct = default);
}
```

---

## 7. Infrastructure — Persistence

### 7.1 Storage Rules (ARCH-009 alignment)

All timestamp columns (`created_at`, `updated_at`) are `TIMESTAMPTZ`, storing UTC — no exceptions, per ARCH-009's `DateTime` ban and UTC-storage mandate. `CheckInDate`/`CheckOutDate` are plain `DateOnly` properties (Shared Kernel has no `DateRange` type — see Corrections table) mapped directly to two `DATE` columns (`check_in`, `check_out`) — dates only, no time-of-day component, since check-in/check-out are calendar days, not instants.

### 7.2 EF Core Configurations

```csharp
namespace UTOP.Accommodation.Infrastructure.Persistence.Configurations;

public sealed class AccommodationBookingConfiguration : IEntityTypeConfiguration<AccommodationBooking>
{
    public void Configure(EntityTypeBuilder<AccommodationBooking> builder)
    {
        builder.ToTable("accommodation_bookings", "utop_accommodation");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.AccommodationBookingId)
            .HasConversion(id => id.Value, value => new AccommodationBookingId(value))
            .HasColumnName("accommodation_booking_id")
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(b => b.AccommodationBookingId).IsUnique();

        builder.Property(b => b.BookingId).HasColumnName("booking_id").HasMaxLength(64).IsRequired();
        builder.HasIndex(b => b.BookingId);

        builder.Property(b => b.LinkedPilgrimageId).HasColumnName("linked_pilgrimage_id").HasMaxLength(64);
        builder.HasIndex(b => b.LinkedPilgrimageId);

        builder.Property(b => b.PropertyExternalReference).HasColumnName("property_external_reference").HasMaxLength(128).IsRequired();

        builder.OwnsOne(b => b.Property, p =>
        {
            p.Property(x => x.Code).HasColumnName("property_code").HasMaxLength(64).IsRequired();
            p.Property(x => x.Type).HasConversion<string>().HasColumnName("property_location_type").HasMaxLength(30).IsRequired();
            p.Property(x => x.DisplayName).HasColumnName("property_display_name").HasMaxLength(200);
        });

        // No DateRange in Shared Kernel — plain DateOnly properties, direct mapping
        // (not OwnsOne, since there's no owned type here anymore).
        builder.Property(b => b.CheckInDate).HasColumnName("check_in").HasColumnType("date").IsRequired();
        builder.Property(b => b.CheckOutDate).HasColumnName("check_out").HasColumnType("date").IsRequired();

        builder.OwnsOne(b => b.TotalPrice, m =>
        {
            m.Property(x => x.Amount).HasColumnName("total_price_amount").HasColumnType("numeric(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("total_price_currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.PrimaryGuestName).HasColumnName("primary_guest_name").HasMaxLength(200).IsRequired();
        builder.Property(b => b.AmendmentVersion).HasColumnName("amendment_version").IsRequired();

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.Property(b => b.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasMany<Room>("_rooms").WithOne().HasForeignKey("accommodation_booking_id").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany<AncillaryService>("_ancillaryServices").WithOne().HasForeignKey("accommodation_booking_id").OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(AccommodationBooking.Rooms))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(AccommodationBooking.AncillaryServices))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms", "utop_accommodation");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type).HasConversion<string>().HasColumnName("room_type").HasMaxLength(20).IsRequired();
        builder.Property(r => r.ProviderRoomReference).HasColumnName("provider_room_reference").HasMaxLength(128).IsRequired();

        builder.OwnsOne(r => r.RatePerNight, m =>
        {
            m.Property(x => x.Amount).HasColumnName("rate_per_night_amount").HasColumnType("numeric(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("rate_per_night_currency").HasMaxLength(3).IsRequired();
        });

        builder.HasMany<Occupant>("_occupants").WithOne().HasForeignKey("room_id").OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Room.Occupants))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class OccupantConfiguration : IEntityTypeConfiguration<Occupant>
{
    public void Configure(EntityTypeBuilder<Occupant> builder)
    {
        builder.ToTable("occupants", "utop_accommodation");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(o => o.Type).HasConversion<string>().HasColumnName("occupant_type").HasMaxLength(10).IsRequired();
    }
}

public sealed class AncillaryServiceConfiguration : IEntityTypeConfiguration<AncillaryService>
{
    public void Configure(EntityTypeBuilder<AncillaryService> builder)
    {
        builder.ToTable("ancillary_services", "utop_accommodation");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasColumnName("service_type").HasMaxLength(20).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(a => a.Quantity).HasColumnName("quantity").IsRequired();

        builder.OwnsOne(a => a.Price, m =>
        {
            m.Property(x => x.Amount).HasColumnName("price_amount").HasColumnType("numeric(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("price_currency").HasMaxLength(3).IsRequired();
        });
    }
}
```

### 7.3 PostgreSQL DDL

```sql
CREATE SCHEMA IF NOT EXISTS utop_accommodation;

CREATE TABLE utop_accommodation.accommodation_bookings (
    id                          UUID PRIMARY KEY,
    accommodation_booking_id    VARCHAR(32)     NOT NULL,
    booking_id                  VARCHAR(64)     NOT NULL,
    linked_pilgrimage_id        VARCHAR(64)     NULL,
    property_external_reference VARCHAR(128)    NOT NULL,
    property_code               VARCHAR(64)     NOT NULL,
    property_location_type     VARCHAR(30)     NOT NULL,
    property_display_name      VARCHAR(200)    NULL,
    check_in                    DATE            NOT NULL,
    check_out                   DATE            NOT NULL,
    total_price_amount          NUMERIC(18,2)   NOT NULL,
    total_price_currency        CHAR(3)         NOT NULL,
    status                      VARCHAR(20)     NOT NULL,
    primary_guest_name          VARCHAR(200)    NOT NULL,
    amendment_version           INT             NOT NULL DEFAULT 0,
    created_at                  TIMESTAMPTZ     NOT NULL,
    updated_at                  TIMESTAMPTZ     NOT NULL,
    version                     BIGINT          NOT NULL DEFAULT 0,

    CONSTRAINT ck_accommodation_bookings_stay CHECK (check_out > check_in),
    CONSTRAINT ck_accommodation_bookings_price CHECK (total_price_amount > 0),
    CONSTRAINT ck_accommodation_bookings_status CHECK (
        status IN ('Requested','Confirmed','CheckedIn','CheckedOut','Cancelled','NoShow')
    )
);

CREATE UNIQUE INDEX ux_accommodation_bookings_business_id
    ON utop_accommodation.accommodation_bookings (accommodation_booking_id);
CREATE INDEX ix_accommodation_bookings_booking_id
    ON utop_accommodation.accommodation_bookings (booking_id);
CREATE INDEX ix_accommodation_bookings_linked_pilgrimage_id
    ON utop_accommodation.accommodation_bookings (linked_pilgrimage_id)
    WHERE linked_pilgrimage_id IS NOT NULL;

CREATE TABLE utop_accommodation.rooms (
    id                       UUID PRIMARY KEY,
    accommodation_booking_id UUID            NOT NULL
        REFERENCES utop_accommodation.accommodation_bookings (id) ON DELETE CASCADE,
    room_type                VARCHAR(20)     NOT NULL,
    provider_room_reference  VARCHAR(128)    NOT NULL,
    rate_per_night_amount    NUMERIC(18,2)   NOT NULL,
    rate_per_night_currency  CHAR(3)         NOT NULL,

    CONSTRAINT ck_rooms_rate CHECK (rate_per_night_amount > 0),
    CONSTRAINT uq_rooms_provider_reference UNIQUE (accommodation_booking_id, provider_room_reference)
);

CREATE INDEX ix_rooms_accommodation_booking_id
    ON utop_accommodation.rooms (accommodation_booking_id);

CREATE TABLE utop_accommodation.occupants (
    id      UUID PRIMARY KEY,
    room_id UUID            NOT NULL
        REFERENCES utop_accommodation.rooms (id) ON DELETE CASCADE,
    name    VARCHAR(200)    NOT NULL,
    occupant_type VARCHAR(10) NOT NULL CHECK (occupant_type IN ('Adult','Child')),

    CONSTRAINT uq_occupants_name_type_per_room UNIQUE (room_id, name, occupant_type)
);

CREATE INDEX ix_occupants_room_id
    ON utop_accommodation.occupants (room_id);

CREATE TABLE utop_accommodation.ancillary_services (
    id                       UUID PRIMARY KEY,
    accommodation_booking_id UUID            NOT NULL
        REFERENCES utop_accommodation.accommodation_bookings (id) ON DELETE CASCADE,
    service_type             VARCHAR(20)     NOT NULL,
    description              VARCHAR(500)    NOT NULL,
    price_amount             NUMERIC(18,2)   NOT NULL,
    price_currency           CHAR(3)         NOT NULL,
    quantity                 INT             NOT NULL,

    CONSTRAINT ck_ancillary_services_price CHECK (price_amount >= 0),
    CONSTRAINT ck_ancillary_services_quantity CHECK (quantity >= 1)
);

CREATE INDEX ix_ancillary_services_accommodation_booking_id
    ON utop_accommodation.ancillary_services (accommodation_booking_id);

-- Outbox table — shared platform pattern (already defined once in UTOP-LLD-BK-04);
-- Accommodation reuses the same shape, scoped to this schema for physical co-location
-- with the aggregate that writes to it in the same transaction.
CREATE TABLE utop_accommodation.outbox_messages (
    id             UUID PRIMARY KEY,
    aggregate_id   VARCHAR(64)   NOT NULL,
    aggregate_type VARCHAR(100)  NOT NULL,
    event_type     VARCHAR(200)  NOT NULL,
    payload        JSONB         NOT NULL,
    occurred_at    TIMESTAMPTZ   NOT NULL,
    processed_at   TIMESTAMPTZ   NULL,

    CONSTRAINT ck_outbox_processed_after_occurred CHECK (processed_at IS NULL OR processed_at >= occurred_at)
);

CREATE INDEX ix_outbox_messages_unprocessed
    ON utop_accommodation.outbox_messages (occurred_at)
    WHERE processed_at IS NULL;
```

**Assumption flagged for confirmation:** `property_code` above assumes the Shared Kernel `Location` value object exposes a `Code` field as its identity (mirroring the IATA-style codes `JourneyRoute.Origin`/`Destination` use in BOOKING-001). This document hasn't re-read `Location`'s actual definition from the Shared Kernel source — confirm the field name matches before implementing `AccommodationBookingConfiguration.OwnsOne(b => b.Property, ...)` above.

---

## 8. Integration Events & RabbitMQ Topology

### 8.1 Events Published

```
Exchange: utop.events (existing, topic, durable)

Registered in ARCH-007 §4.2 (Event Ownership Register) — published as-is:
  accommodation.booked      (from AccommodationBookingConfirmed — event name kept as
                              "booked" to match the existing ARCH-007 register entry;
                              this is a deliberate naming exception, called out in the
                              Corrections table rather than silently renamed)
  accommodation.cancelled

NOT registered in ARCH-007 §4.2 — ARCH-008 §2 claims this context publishes it, but no
register entry exists (see Corrections table). Treated as unauthorized until the
Architecture Board adds the entry (ARCH-007 §9.1 process — open item UTOP-LLD-ACM-02):
  accommodation.amended

New — proposed by this document, not present in either ARCH-007 or ARCH-008, same
authorization gap applies (open item UTOP-LLD-ACM-02):
  accommodation.checked_in
  accommodation.checked_out
  accommodation.no_show
```

### 8.2 Events Consumed

```
BookingCancelledIntegrationEvent   → BookingCancelledEventHandler   → releases hold
PilgrimageConfirmedIntegrationEvent → PilgrimageConfirmedEventHandler → sets LinkedPilgrimageId
```

### 8.3 Queue Bindings

```
utop.accommodation.queue
  binds: booking.cancelled, pilgrimage.confirmed

Downstream consumers of accommodation.booked, per ARCH-007 §4.2 register (authoritative):
  Notifications → accommodation.booked
  Analytics     → accommodation.* (via utop.analytics.queue, existing wildcard binding)

Downstream consumer required by the saga but NOT in the ARCH-007 register (open item
UTOP-LLD-ACM-06 — Architecture Board must add before this binding can be wired):
  Pilgrimage    → accommodation.booked (verifies sacred site proximity — ARCH-008 §4)
```

---

## 9. Shared Kernel Usage

| Type | How used |
|---|---|
| `Money` | `TotalPrice`, `Room.RatePerNight`, `AncillaryService.Price`; mapped via `OwnsOne` |
| `Location` | `Property` |
| ~~`DateRange`~~ | **Does not exist in Shared Kernel** — discovered during implementation. `CheckInDate`/`CheckOutDate` are plain `DateOnly` properties with a computed `Nights` property instead. See Corrections table. |
| `CorrelationId` | Carried on every command and domain event |
| `IClock` | Injected into `Create()`, `Confirm()`, `Amend()`, `Cancel()`, `CheckIn()`, `CheckOut()`, `RecordNoShow()` |

No new Shared Kernel admission requests from this context — everything needed already exists (ARCH-010's 10-of-15 cap is unaffected).

---

## 10. Test Strategy

### 10.1 Unit Tests — Domain Layer

`FakeClock` used in every temporal test, per platform convention. `DateTime.UtcNow` never appears in test code.

```
AccommodationBooking_ACINV001_PriceIsZero_CreateThrows()
AccommodationBooking_ACINV001_PriceIsPositive_CreateSucceeds()

AccommodationBooking_ACINV002_SameDayStay_CreateThrows()
AccommodationBooking_ACINV002_OneNightStay_CreateSucceeds()

AccommodationBooking_ACINV003_BookingIdMissing_CreateThrows()
AccommodationBooking_ACINV015_PropertyExternalReferenceMissing_CreateThrows()
AccommodationBooking_ACINV016_PropertyCodeMissing_CreateThrows()

AccommodationBooking_ACTINV001_CheckInInPast_CreateThrows()
AccommodationBooking_ACTINV001_CheckInInFuture_CreateSucceeds()

AccommodationBooking_ACTINV004_AmendNewCheckInInPast_Throws()
AccommodationBooking_ACTINV004_AmendNewCheckInInFuture_Succeeds()

AccommodationBooking_ACINV004_NoRooms_ConfirmThrows()
AccommodationBooking_ACINV004_OneRoom_ConfirmSucceeds()

AccommodationBooking_ACINV018_DuplicateProviderRoomReference_AddRoomThrows()
Room_ACINV017_DuplicateOccupantNameAndType_AddOccupantThrows()

AccommodationBooking_ACINV005_NoOccupants_ConfirmThrows()

AccommodationBooking_ACINV006_AmendWithDifferentCurrency_Throws()
AccommodationBooking_ACINV006_AmendWithSameCurrency_Succeeds()

AccommodationBooking_ACTINV002_CancelWithinCutoff_Throws()
AccommodationBooking_ACTINV002_CancelBeyondCutoff_Succeeds()
AccommodationBooking_Cancel_AlreadyCancelled_IsIdempotent()

AccommodationBooking_ACINV007_CancelAfterCheckedOut_Throws()
AccommodationBooking_ACINV007_AnyMutationAfterNoShow_Throws()

AccommodationBooking_ACTINV003_CheckInBeforeStayDate_Throws()
AccommodationBooking_CheckIn_FromRequested_Throws()   // must be Confirmed first
AccommodationBooking_CheckOut_FromConfirmed_Throws()  // must be CheckedIn first
AccommodationBooking_RecordNoShow_FromCheckedIn_Throws()

AccommodationBooking_ACINV009_ZeroRoomRate_CreateRoomThrows()
AccommodationBooking_ACINV008_ZeroQuantityAncillaryService_Throws()
AccommodationBooking_ACINV008_ZeroPriceAncillaryService_Succeeds()  // complimentary services allowed

AccommodationBooking_ACCINV001_ConcurrentAmendSameVersion_SecondWriteRejected()

AccommodationBooking_StatusTransitions_AllForbiddenTransitionsThrow()
```

### 10.2 Integration Tests — Application Layer

```
CreateAccommodationBookingCommandHandler_ValidCommand_ReturnsAccommodationBookingId()
CreateAccommodationBookingCommandHandler_PilgrimageBookingOutOfProximity_ThrowsOrEscalates()
ConfirmAccommodationBookingCommandHandler_ProviderConfirms_StatusIsConfirmed()
CancelAccommodationBookingCommandHandler_AlreadyCancelled_IsIdempotent()
CheckInCommandHandler_Confirmed_StatusIsCheckedIn()
CheckOutCommandHandler_CheckedIn_StatusIsCheckedOut()
BookingCancelledEventHandler_ActiveAccommodation_CancelsIt()
BookingCancelledEventHandler_NoMatchingAccommodation_DoesNotThrow()
PilgrimageConfirmedEventHandler_ValidEvent_SetsLinkedPilgrimageId()
AmendAccommodationBookingCommandHandler_StaleExpectedVersion_ThrowsConcurrencyException()
CancelAccommodationBookingCommandHandler_StaleExpectedVersion_ThrowsConcurrencyException()
```

### 10.3 Persistence Mapping Tests

```
AccommodationBookingConfiguration_MoneyOwnedType_RoundTripsAmountAndCurrency()
AccommodationBookingConfiguration_LocationOwnedType_RoundTripsPropertyCode()
AccommodationBookingConfiguration_CheckInCheckOutDates_RoundTripAsDateColumns()
RoomConfiguration_MoneyOwnedType_RoundTripsRatePerNight()
AncillaryServiceConfiguration_MoneyOwnedType_RoundTripsPrice()
```

### 10.4 Stub Implementations

| Port | Stub | Behaviour |
|---|---|---|
| `IAccommodationProvider` | `StubAccommodationProvider` | Returns deterministic property + rate for any search |
| `ISacredSiteProximityProvider` | `StubSacredSiteProximityProvider` | Always returns `true` |
| `IClock` | `FakeClock` (Shared Kernel) | Deterministic; caller controls time |

---

## 11. Domain Exceptions

```csharp
// Namespace: UTOP.Accommodation.Domain.Exceptions
AccommodationBookingNotFoundException
AccommodationPriceMustBePositiveException
InvalidStayDurationException
AccommodationBookingIdRequiredException
PropertyExternalReferenceRequiredException
InvalidPropertyIdentityException
AccommodationCheckInAlreadyPassedException
AccommodationRequiresRoomException
AccommodationRequiresOccupantException
InvalidAccommodationStateTransitionException
AccommodationCancellationWindowExpiredException
AccommodationCheckInTooEarlyException
AccommodationCurrencyImmutableAfterConfirmationException
InvalidRoomRateException
InvalidAncillaryServiceException
DuplicateRoomException
DuplicateOccupantException
```

---

## 12. Open Items

| ID | Item | Severity | Resolution Path |
|---|---|---|---|
| UTOP-LLD-ACM-01 | Cancellation cutoff (`AC-TINV-002`) has no business-decided value — modeled as configurable, default left unset. **Decision: carried as an open item, to be addressed during implementation** (not a design blocker) | Medium | Configuration value set during implementation, externalized per Decision 6 precedent |
| UTOP-LLD-ACM-02 | Four events need admission through ARCH-007 §9.1 before they can be published: `accommodation.amended` (claimed by ARCH-008 but absent from the ARCH-007 register — a pre-existing gap, not something this document introduced), plus the three new events proposed here (`checked_in`, `checked_out`, `no_show`). **Decision: left open, to be resolved during the Pilgrimage LLD** | Medium | ARCH-007 §9.1 is a 9-step checklist (register entry → payload → PII classification → retention → routing key → queue bindings → mapping → inbox handler → tests) — no ACR needed, since ACRs are only required for breaking changes to *existing* events (§9.2) |
| UTOP-LLD-ACM-06 | Pilgrimage is required by ARCH-008 §4's saga dependency to consume `AccommodationBookedIntegrationEvent`, but ARCH-007 §4.2's Allowed Consumers column for that event lists only Notification and Analytics — Pilgrimage is unauthorized as written. **Decision: left open, to be resolved during the Pilgrimage LLD** | Medium | Architecture Board must add Pilgrimage to the register's Allowed Consumers column (ARCH-007 §4.2) |
| UTOP-LLD-ACM-03 | `ISacredSiteProximityProvider` — is this genuinely a synchronous call at `Create()` time, or should proximity validation happen asynchronously (mirroring how ResourceAllocation escalates rather than blocking)? Modeled here as synchronous/blocking for simplicity | Low | Confirm against Pilgrimage context's actual service-interface latency characteristics when that LLD is written |
| UTOP-LLD-ACM-04 | Refund amount on cancellation — this document records cancellation but does not calculate any refund, same boundary Booking drew (`UTOP-LLD-BK-02`) | Low | CostSplitting LLD |
| UTOP-LLD-ACM-05 | Outbox processor — shared platform pattern, not re-specified here | Low | Already tracked once in `UTOP-LLD-BK-04`; one processor serves all contexts |
| UTOP-LLD-LOCALTIME-01 | `LocalizedTime` type-system enforcement | Low | Carried forward from ARCH-009, still open |
| UTOP-LLD-ACM-08 | `PilgrimageConfirmedInboundEvent`'s shape (`PilgrimageId`, `IReadOnlyList<string> PilgrimBookingIds`, `CorrelationId`) is a best-guess implemented against, not confirmed against a real source — `UTOP.Pilgrimage` doesn't exist as an implemented context and no Pilgrimage LLD exists yet | Medium | Confirm against the actual Pilgrimage LLD once written; correct `AccommodationInboundEvents.cs` if the real shape differs |

---

*Document owner: UTOP Architecture Board*
*Baselined: Not yet — review cycle 1 fixes applied (Items 1–9 of the attached review); pending final verification confirming no new High/Medium findings, per the review's closure recommendation*
*Corrects: naming conflict between ARCH-001 and ARCH-003 §12, plus two ARCH-007/ARCH-008 event-governance conflicts, as detailed above*
