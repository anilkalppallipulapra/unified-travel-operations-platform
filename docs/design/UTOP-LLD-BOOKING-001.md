# Low-Level Design — Booking Context
**Document ID**: UTOP-LLD-BOOKING-001  
**Version**: 1.4.0  
**Status**: Baselined  
**Context**: Booking  
**Schema**: `utop_booking`  
**Depends on**: UTOP-ARCH-002, UTOP-ARCH-003, UTOP-ARCH-004, UTOP-ARCH-005, UTOP-ARCH-007, UTOP-ARCH-008, UTOP-ARCH-009, UTOP-ARCH-010

---

## Changelog

| Version | Changes |
|---|---|
| 1.0.0 | Initial draft — aggregate, entities, value objects, domain events, all five command handlers, queries, port interfaces, PostgreSQL schema, EF Core configuration (BookingConfiguration, ItineraryConfiguration), integration events, RabbitMQ topology, Shared Kernel usage, test strategy, domain exceptions, open items |
| 1.1.0 | Self-review corrections: BookingId.Generate() accepts DateOnly from IClock — no internal DateTime.UtcNow; EF Core Money mapping changed from HasConversion with anonymous type to OwnsOne; AggregateRoot and DomainEvent base class temporal fields corrected to DateTimeOffset; DomainEvent base class CorrelationId corrected to Shared Kernel struct; PassengerDetail DTO defined; AmendBookingCommandHandler and CompleteBookingCommandHandler added (were missing); ResourceAllocatedEventHandler added for inbound integration event; GetBookingsByOperatorQueryHandler added; passenger count completeness check added to Confirm(); BookingStatus initial state corrected from Draft to PendingValidation per ARCH-005; Base class corrections section added |
| 1.2.0 | PassengerConfiguration added — maps all columns of utop_booking.passengers table; Typo corrected: ReturnsBokingId → ReturnsBookingId |
| 1.3.0 | StartJourneyCommand and StartJourneyCommandHandler added — closes missing application path for MarkInTransit(); CompleteBookingCommand documented as requiring prior StartJourney; two tests added for StartJourney and Complete invalid state; Itinerary entity extended with DepartureCity, DepartureCountry, ArrivalCity, ArrivalCountry properties; ItineraryConfiguration updated to map all four columns; CreateBookingCommand and AmendBookingCommand updated to carry city and country fields |
| 1.4.0 | Corrections discovered during implementation (`feature/implementation`, tracked in `PENDING-LLD-CORRECTIONS.md`): `IBookingReadRepository` relocated from `Domain.Repositories` (§9.1) to `Application.Queries` (§7.5) — original placement inverted the Domain→Application dependency direction, since its return type `BookingReadModel` is an Application-layer type; `RemovePassenger()` added to the aggregate (§4.1) — required by BK-INV-005's "enforced on AddPassenger() and RemovePassenger()" wording but missing from the original code sample; does not reduce `PassengerCount`, party-size re-pricing stays deferred to CostSplitting per UTOP-LLD-BK-02; `AddPassenger()` now explicitly guards against `Completed` status per BK-INV-007, closing a gap where the original sample enforced this guard on some mutation methods but not all; namespace `UTOP.SharedKernel` renamed to `UTOP.Shared` throughout, matching the rename now canonical in ARCH-010 |

---

## Corrections to Prior Architecture Artifacts

These corrections supersede UTOP-ARCH-003 (Domain Models) on the listed points. The stabilization artifacts (ARCH-009, ARCH-010) are authoritative where they conflict with ARCH-003.

| Item | ARCH-003 (superseded) | Correct (this document) | Authority |
|---|---|---|---|
| `AggregateRoot.CreatedAt` / `UpdatedAt` | `DateTime` | `DateTimeOffset` | ARCH-009 §2.3 |
| `DomainEvent.OccurredAt` | `DateTime` | `DateTimeOffset` | ARCH-009 §2.3 |
| `CorrelationId` type | `string` with prefix format | `record struct` wrapping `Guid` | ARCH-010 §5.4 |
| `Money.Currency` | `string` | `Currency` enum | ARCH-010 §5.1 |
| Direct `DateTime.UtcNow` in domain | Present in ARCH-003 | Replaced with `IClock.UtcNow` | ARCH-009 §3 |
| Initial booking status | `Draft` (ARCH-003) | `PendingValidation` (ARCH-005) | ARCH-005 §1.2 |

---

## 1. Context Responsibility

The Booking context owns the full lifecycle of a travel booking from creation through completion or cancellation. It is the single source of truth for booking identity, status, itinerary, passenger manifest, price, travel category, and operator assignment.

It does not calculate costs across group members — that belongs to CostSplitting. It does not make resource allocation decisions — that belongs to ResourceAllocation. It does not determine pilgrimage compliance — that belongs to Pilgrimage. It creates the booking record, drives it through its state machine, and publishes the integration events that allow downstream contexts to act.

Group and Pilgrimage associations are opaque identity references. The Booking context knows a `GroupId` or `PilgrimageId` exists but cannot query those schemas. Association validity is confirmed through service interfaces — not direct database access.

---

## 2. Solution Structure

```
UTOP.Booking/
├── Domain/
│   ├── Aggregates/
│   │   └── Booking.cs
│   ├── Entities/
│   │   ├── Itinerary.cs
│   │   └── Passenger.cs
│   ├── ValueObjects/
│   │   ├── BookingId.cs
│   │   ├── JourneyRoute.cs
│   │   ├── BookingStatus.cs       (enum)
│   │   ├── TravelMode.cs          (enum)
│   │   ├── TravelCategory.cs      (enum)
│   │   └── PassengerType.cs       (enum)
│   ├── Events/
│   │   ├── BookingCreated.cs
│   │   ├── BookingConfirmed.cs
│   │   ├── BookingAmended.cs
│   │   ├── BookingCancelled.cs
│   │   ├── BookingEscalated.cs
│   │   └── BookingCompleted.cs
│   ├── Exceptions/
│   │   └── (all listed in §12)
│   ├── Services/
│   │   └── (none in this context — all logic belongs in aggregate)
│   └── Repositories/
│       ├── IBookingRepository.cs
│       └── IBookingReadRepository.cs
├── Application/
│   ├── Commands/
│   │   ├── CreateBookingCommand.cs + CreateBookingCommandHandler.cs
│   │   ├── ConfirmBookingCommand.cs + ConfirmBookingCommandHandler.cs
│   │   ├── AmendBookingCommand.cs + AmendBookingCommandHandler.cs
│   │   ├── CancelBookingCommand.cs + CancelBookingCommandHandler.cs
│   │   ├── StartJourneyCommand.cs + StartJourneyCommandHandler.cs
│   │   └── CompleteBookingCommand.cs + CompleteBookingCommandHandler.cs
│   ├── Queries/
│   │   ├── GetBookingByIdQuery.cs + GetBookingByIdQueryHandler.cs
│   │   └── GetBookingsByOperatorQuery.cs + GetBookingsByOperatorQueryHandler.cs
│   ├── EventHandlers/
│   │   └── ResourceAllocatedEventHandler.cs   (inbound integration event)
│   └── Ports/
│       ├── IAvailabilityProvider.cs
│       ├── IPrayerTimeProvider.cs
│       └── IGroupExistenceValidator.cs
├── Infrastructure/
│   ├── Persistence/
│   │   ├── BookingDbContext.cs
│   │   ├── BookingRepository.cs
│   │   ├── BookingReadRepository.cs
│   │   ├── Migrations/
│   │   └── Configurations/
│   │       ├── BookingConfiguration.cs
│   │       ├── ItineraryConfiguration.cs
│   │       └── PassengerConfiguration.cs
│   ├── Messaging/
│   │   ├── BookingEventPublisher.cs
│   │   └── OutboxProcessor.cs
│   └── Adapters/
│       ├── StubAvailabilityProvider.cs
│       ├── StubPrayerTimeProvider.cs
│       └── StubGroupExistenceValidator.cs
```

---

## 3. Base Class Corrections

These corrections apply platform-wide. Defined here because Booking is the first LLD artifact. All subsequent context LLDs inherit these definitions.

```csharp
namespace UTOP.SharedKernel;

// CORRECTION: CreatedAt and UpdatedAt changed from DateTime to DateTimeOffset (ARCH-009)
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }   // UTC DateTimeOffset
    public DateTimeOffset UpdatedAt { get; protected set; }   // UTC DateTimeOffset

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

// CORRECTION: OccurredAt changed from DateTime to DateTimeOffset (ARCH-009)
// CORRECTION: CorrelationId changed from string to CorrelationId struct (ARCH-010)
public abstract record DomainEvent(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    DateTimeOffset OccurredAt);

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public override bool Equals(object? obj) => obj is Entity other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}
```

---

## 4. Aggregate Design — Booking

### 4.1 Booking (Aggregate Root)

```csharp
namespace UTOP.Booking.Domain.Aggregates;

public sealed class Booking : AggregateRoot
{
    public BookingId BookingId { get; private set; } = null!;
    public TravelMode Mode { get; private set; }
    public JourneyRoute Route { get; private set; } = null!;
    public PassengerCount Passengers { get; private set; } = null!;
    public BookingStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = null!;
    public TravelCategory Category { get; private set; }
    public string OperatorId { get; private set; } = null!;
    public Itinerary Itinerary { get; private set; } = null!;
    public string? GroupId { get; private set; }
    public string? PilgrimageId { get; private set; }
    public int AmendmentVersion { get; private set; }

    private readonly List<Passenger> _passengers = new();
    public IReadOnlyList<Passenger> PassengerList => _passengers.AsReadOnly();

    private Booking() { } // EF Core constructor

    /// <summary>
    /// Factory. Creates a booking in PendingValidation status (ARCH-005 §1.2).
    /// IClock is injected so departure validation uses a testable time source (ARCH-009 §3).
    /// BookingId generation is delegated the current date from clock — never calls DateTime.UtcNow directly.
    /// </summary>
    public static Booking Create(
        TravelMode mode,
        JourneyRoute route,
        PassengerCount passengers,
        TravelCategory category,
        string operatorId,
        Money price,
        Itinerary itinerary,
        CorrelationId correlationId,
        IClock clock)
    {
        // BK-INV-010
        if (price.Amount <= 0)
            throw new BookingPriceMustBePositiveException();

        // BK-INV-011
        if (route.Origin.Code == route.Destination.Code)
            throw new BookingRouteOriginEqualsDestinationException(route.Origin.Code);

        // BK-INV-012
        if (string.IsNullOrWhiteSpace(operatorId))
            throw new BookingOperatorIdRequiredException();

        // BK-TINV-001 — uses IClock, not DateTime.UtcNow
        if (itinerary.DepartureUtc <= clock.UtcNow)
            throw new BookingDepartureAlreadyPassedException(itinerary.DepartureUtc, clock.UtcNow);

        var now = clock.UtcNow;

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            // Pass date from clock — BookingId.Generate never calls DateTime.UtcNow internally
            BookingId = BookingId.Generate(mode, DateOnly.FromDateTime(now.UtcDateTime)),
            Mode = mode,
            Route = route,
            Passengers = passengers,
            Status = BookingStatus.PendingValidation,
            TotalPrice = price,
            Category = category,
            OperatorId = operatorId,
            Itinerary = itinerary,
            AmendmentVersion = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        booking.AddDomainEvent(new BookingCreated(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: booking.Id.ToString(),
            AggregateType: nameof(Booking),
            BookingId: booking.BookingId,
            Mode: mode,
            Route: route,
            TotalPrice: price,
            Category: category,
            OperatorId: operatorId,
            OccurredAt: now));

        return booking;
    }

    /// <summary>
    /// Confirms the booking.
    /// BK-INV-004: At least one adult passenger must be present.
    /// BK-INV-005: Passenger manifest count must match PassengerCount.Total.
    /// BK-INV-008: Religious category requires PilgrimageId.
    /// BK-INV-009: Group category requires GroupId.
    /// BK-TINV-004: Departure must not have passed.
    /// Transitions: PENDING_VALIDATION → CONFIRMED
    /// </summary>
    public void Confirm(CorrelationId correlationId, IClock clock)
    {
        if (Status != BookingStatus.PendingValidation)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.Confirmed);

        // BK-TINV-004
        if (Itinerary.DepartureUtc <= clock.UtcNow)
            throw new BookingDepartureAlreadyPassedException(Itinerary.DepartureUtc, clock.UtcNow);

        // BK-INV-004
        if (!_passengers.Any(p => p.Type == PassengerType.Adult))
            throw new BookingRequiresAdultPassengerException(BookingId);

        // BK-INV-005: full manifest must be present at confirm time
        if (_passengers.Count != Passengers.Total)
            throw new PassengerCountMismatchException(BookingId, Passengers.Total, _passengers.Count);

        // BK-INV-008
        if (Category == TravelCategory.Religious && PilgrimageId is null)
            throw new PilgrimageBookingRequiresPilgrimageAssociationException(BookingId);

        // BK-INV-009
        if (Category == TravelCategory.Group && GroupId is null)
            throw new GroupBookingRequiresGroupAssociationException(BookingId);

        var now = clock.UtcNow;
        Status = BookingStatus.Confirmed;
        UpdatedAt = now;

        AddDomainEvent(new BookingConfirmed(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(Booking),
            BookingId: BookingId,
            Category: Category,
            TotalPrice: TotalPrice,
            Passengers: Passengers,
            OccurredAt: now));
    }

    /// <summary>
    /// Amends itinerary and/or price of a confirmed booking.
    /// BK-INV-003: Currency must not change after confirmation.
    /// BK-TINV-002: Forbidden within 2 hours of departure.
    /// BK-INV-014: Itinerary is replaced atomically, never partially mutated.
    /// Transitions: CONFIRMED → CONFIRMED (state does not change; version increments)
    /// </summary>
    public void Amend(
        Itinerary newItinerary,
        Money newPrice,
        CorrelationId correlationId,
        IClock clock)
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.Confirmed);

        // BK-TINV-002
        var timeUntilDeparture = Itinerary.DepartureUtc - clock.UtcNow;
        if (timeUntilDeparture <= TimeSpan.FromHours(2))
            throw new BookingAmendmentWindowExpiredException(BookingId, Itinerary.DepartureUtc, clock.UtcNow);

        // BK-INV-003
        if (newPrice.Currency != TotalPrice.Currency)
            throw new CurrencyImmutableAfterConfirmationException(BookingId, TotalPrice.Currency, newPrice.Currency);

        var previousItinerary = Itinerary;
        var previousPrice = TotalPrice;
        var now = clock.UtcNow;

        Itinerary = newItinerary;    // BK-INV-014: atomic replacement
        TotalPrice = newPrice;
        AmendmentVersion++;
        UpdatedAt = now;

        AddDomainEvent(new BookingAmended(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(Booking),
            BookingId: BookingId,
            AmendmentVersion: AmendmentVersion,
            PreviousItinerary: previousItinerary,
            NewItinerary: newItinerary,
            PreviousPrice: previousPrice,
            NewPrice: newPrice,
            OccurredAt: now));
    }

    /// <summary>
    /// Cancels the booking. Idempotent if already Cancelled or Refunded.
    /// Cannot cancel a Completed booking (BK-INV-007).
    /// Cannot cancel a booking InTransit.
    /// Transitions: PENDING_VALIDATION | CONFIRMED | ALLOCATED → CANCELLED
    /// </summary>
    public void Cancel(string reason, CorrelationId correlationId, IClock clock)
    {
        // Idempotent (ARCH-005 §1.4)
        if (Status is BookingStatus.Cancelled or BookingStatus.Refunded)
            return;

        // BK-INV-007
        if (Status == BookingStatus.Completed)
            throw new BookingAlreadyCompletedException(BookingId);

        if (Status == BookingStatus.InTransit)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.Cancelled);

        var now = clock.UtcNow;
        Status = BookingStatus.Cancelled;
        UpdatedAt = now;

        AddDomainEvent(new BookingCancelled(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(Booking),
            BookingId: BookingId,
            Reason: reason,
            CancelledAt: now,
            OccurredAt: now));
    }

    /// <summary>
    /// Escalates when availability validation fails.
    /// Transitions: PENDING_VALIDATION → ESCALATED
    /// </summary>
    public void Escalate(string reason, CorrelationId correlationId, IClock clock)
    {
        if (Status != BookingStatus.PendingValidation)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.Escalated);

        var now = clock.UtcNow;
        Status = BookingStatus.Escalated;
        UpdatedAt = now;

        AddDomainEvent(new BookingEscalated(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(Booking),
            BookingId: BookingId,
            Reason: reason,
            OccurredAt: now));
    }

    /// <summary>
    /// Called by ResourceAllocatedEventHandler when ResourceAllocation context confirms allocation.
    /// Booking context does not own the ResourceAllocated event — it receives notification via
    /// integration event and advances its own state.
    /// Transitions: CONFIRMED → ALLOCATED
    /// </summary>
    public void MarkAllocated(CorrelationId correlationId, IClock clock)
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.Allocated);

        Status = BookingStatus.Allocated;
        UpdatedAt = clock.UtcNow;
        // No domain event emitted here — ResourceAllocation owns that event
    }

    /// <summary>
    /// Marks booking in transit after departure.
    /// Transitions: ALLOCATED → IN_TRANSIT
    /// </summary>
    public void MarkInTransit(CorrelationId correlationId, IClock clock)
    {
        if (Status != BookingStatus.Allocated)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.InTransit);

        Status = BookingStatus.InTransit;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Completes the booking. Terminal state — no further mutations permitted.
    /// Transitions: IN_TRANSIT → COMPLETED
    /// </summary>
    public void Complete(CorrelationId correlationId, IClock clock)
    {
        if (Status != BookingStatus.InTransit)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.Completed);

        var now = clock.UtcNow;
        Status = BookingStatus.Completed;
        UpdatedAt = now;

        AddDomainEvent(new BookingCompleted(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId,
            AggregateId: Id.ToString(),
            AggregateType: nameof(Booking),
            BookingId: BookingId,
            OccurredAt: now));
    }

    /// <summary>
    /// Associates a pilgrimage group. Must occur before Confirm() for Religious category (BK-INV-008).
    /// Only valid in PendingValidation status.
    /// </summary>
    public void AssociatePilgrimage(string pilgrimageId)
    {
        if (string.IsNullOrWhiteSpace(pilgrimageId))
            throw new PilgrimageBookingRequiresPilgrimageAssociationException(BookingId);
        if (Status != BookingStatus.PendingValidation)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.PendingValidation);
        PilgrimageId = pilgrimageId;
    }

    /// <summary>
    /// Associates a group. Must occur before Confirm() for Group category (BK-INV-009).
    /// Only valid in PendingValidation status.
    /// </summary>
    public void AssociateGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new GroupBookingRequiresGroupAssociationException(BookingId);
        if (Status != BookingStatus.PendingValidation)
            throw new InvalidBookingStateTransitionException(BookingId, Status, BookingStatus.PendingValidation);
        GroupId = groupId;
    }

    /// <summary>
    /// Adds a passenger to the manifest.
    /// BK-INV-005: _passengers.Count may not exceed PassengerCount.Total.
    /// BK-INV-007: throws if Completed (all mutation methods must enforce this —
    /// the original code sample only enforced it on some methods; corrected here).
    /// Full equality is enforced at Confirm() time.
    /// Idempotent on duplicate passenger Id.
    /// </summary>
    public void AddPassenger(Passenger passenger)
    {
        if (Status == BookingStatus.Completed)
            throw new BookingAlreadyCompletedException(BookingId);

        if (_passengers.Any(p => p.Id == passenger.Id))
            return;

        if (_passengers.Count >= Passengers.Total)
            throw new PassengerCountMismatchException(BookingId, Passengers.Total, _passengers.Count + 1);

        _passengers.Add(passenger);
    }

    /// <summary>
    /// Removes a passenger from the manifest. BK-INV-005 requires this guard to exist
    /// alongside AddPassenger() — it was missing from the original code sample entirely.
    /// BK-INV-007: throws if Completed.
    /// Does NOT reduce PassengerCount — PassengerCount is the booked party size set at
    /// Create()/Amend() time; removing a manifested passenger is a manifest change, not
    /// a re-pricing event. Reducing party size and any associated refund is a
    /// CostSplitting concern, deferred per UTOP-LLD-BK-02. Idempotent if the passenger
    /// is not present.
    /// </summary>
    public void RemovePassenger(Guid passengerId)
    {
        if (Status == BookingStatus.Completed)
            throw new BookingAlreadyCompletedException(BookingId);

        _passengers.RemoveAll(p => p.Id == passengerId);
    }
}
```

### 4.2 BookingStatus

```csharp
namespace UTOP.Booking.Domain.ValueObjects;

// Note: ARCH-003 uses Draft as initial state. ARCH-005 (state machine) is authoritative.
// PendingValidation is the correct initial state — Draft is pre-command, not a persisted status.
public enum BookingStatus
{
    PendingValidation,  // Created; awaiting availability confirmation
    Confirmed,          // Availability confirmed; resource allocation pending
    Allocated,          // Resource assigned by ResourceAllocation context
    InTransit,          // Journey started
    Completed,          // Journey complete — terminal
    Cancelled,          // Cancelled — leads to Refunded
    Refunded,           // Refund processed — terminal
    Escalated           // Availability failed; awaiting manager decision
}
```

### 4.3 BookingId

```csharp
namespace UTOP.Booking.Domain.ValueObjects;

/// <summary>
/// Human-readable booking identifier.
/// Format: {MODE_PREFIX}-{YYYYMMDD}-{4-char hex suffix}
/// Examples: FLT-20250601-A3F9, BUS-20250601-C7D2
/// Immutable after creation.
/// IMPORTANT: Generate() accepts DateOnly from IClock.UtcNow — never calls DateTime.UtcNow internally.
/// </summary>
public sealed record BookingId(string Value)
{
    public static BookingId Generate(TravelMode mode, DateOnly date)
    {
        var prefix = mode switch
        {
            TravelMode.Flight => "FLT",
            TravelMode.Bus    => "BUS",
            TravelMode.Train  => "TRN",
            TravelMode.Ferry  => "FRY",
            TravelMode.Coach  => "CCH",
            _                 => "BKG"
        };
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return new BookingId($"{prefix}-{date:yyyyMMdd}-{suffix}");
    }

    public override string ToString() => Value;
}
```

### 4.4 JourneyRoute

```csharp
namespace UTOP.Booking.Domain.ValueObjects;

/// <summary>
/// Immutable route definition.
/// Origin and Destination are Location records from Shared Kernel.
/// BK-INV-011 (Origin.Code != Destination.Code) enforced at Booking.Create().
/// </summary>
public sealed record JourneyRoute(
    Location Origin,
    Location Destination,
    RouteType Type);

public enum RouteType { OneWay, RoundTrip, MultiLeg }
public enum TravelMode { Flight, Bus, Train, Ferry, Coach }
public enum TravelCategory { Standard, Group, Religious, Corporate }
```

---

## 5. Entity Design

### 5.1 Itinerary

```csharp
namespace UTOP.Booking.Domain.Entities;

/// <summary>
/// Travel schedule for one booking leg.
/// Always replaced atomically on amendment — never mutated in place (BK-INV-014).
/// DepartureUtc and ArrivalUtc are UTC DateTimeOffset per ARCH-009 §2.
/// </summary>
public sealed class Itinerary : Entity
{
    public DateTimeOffset DepartureUtc { get; private set; }
    public DateTimeOffset ArrivalUtc { get; private set; }
    public Location DeparturePoint { get; private set; } = null!;   // carries airport/stop Code
    public string DepartureCity { get; private set; } = null!;       // city name — separate from Location.Code
    public string DepartureCountry { get; private set; } = null!;    // ISO 3166-1 alpha-2
    public Location ArrivalPoint { get; private set; } = null!;      // carries airport/stop Code
    public string ArrivalCity { get; private set; } = null!;
    public string ArrivalCountry { get; private set; } = null!;
    public string? CarrierReference { get; private set; }
    public string? ServiceClass { get; private set; }

    private Itinerary() { }

    public static Itinerary Create(
        DateTimeOffset departureUtc,
        DateTimeOffset arrivalUtc,
        Location departurePoint,
        string departureCity,
        string departureCountry,
        Location arrivalPoint,
        string arrivalCity,
        string arrivalCountry,
        string? carrierReference = null,
        string? serviceClass = null)
    {
        if (arrivalUtc <= departureUtc)
            throw new InvalidItineraryScheduleException(departureUtc, arrivalUtc);
        if (string.IsNullOrWhiteSpace(departureCity)) throw new ArgumentException("Departure city required.");
        if (string.IsNullOrWhiteSpace(departureCountry)) throw new ArgumentException("Departure country required.");
        if (string.IsNullOrWhiteSpace(arrivalCity)) throw new ArgumentException("Arrival city required.");
        if (string.IsNullOrWhiteSpace(arrivalCountry)) throw new ArgumentException("Arrival country required.");

        return new Itinerary
        {
            Id = Guid.NewGuid(),
            DepartureUtc = departureUtc,
            ArrivalUtc = arrivalUtc,
            DeparturePoint = departurePoint,
            DepartureCity = departureCity,
            DepartureCountry = departureCountry,
            ArrivalPoint = arrivalPoint,
            ArrivalCity = arrivalCity,
            ArrivalCountry = arrivalCountry,
            CarrierReference = carrierReference,
            ServiceClass = serviceClass
        };
    }

    public TimeSpan Duration => ArrivalUtc - DepartureUtc;
}
```

### 5.2 Passenger

```csharp
namespace UTOP.Booking.Domain.Entities;

/// <summary>
/// Individual traveller on a booking.
/// PII fields (FirstName, LastName, DocumentNumber) are encrypted at rest.
/// Encryption is handled at the infrastructure layer via EF Core value converter.
/// See open item UTOP-LLD-BK-01.
/// </summary>
public sealed class Passenger : Entity
{
    public string FirstName { get; private set; } = null!;        // PII — encrypted at rest
    public string LastName { get; private set; } = null!;         // PII — encrypted at rest
    public PassengerType Type { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string? DocumentNumber { get; private set; }           // PII — encrypted at rest
    public string? Nationality { get; private set; }              // ISO 3166-1 alpha-2

    private Passenger() { }

    public static Passenger Create(
        string firstName,
        string lastName,
        PassengerType type,
        DateOnly dateOfBirth,
        string? documentNumber = null,
        string? nationality = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        return new Passenger
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Type = type,
            DateOfBirth = dateOfBirth,
            DocumentNumber = documentNumber,
            Nationality = nationality
        };
    }

    public string FullName => $"{FirstName} {LastName}";
}

public enum PassengerType { Adult, Child, Infant }
```

---

## 6. Domain Events

All events inherit from `DomainEvent`. `OccurredAt` is `DateTimeOffset` UTC. `CorrelationId` is the Shared Kernel struct.

```csharp
namespace UTOP.Booking.Domain.Events;

public sealed record BookingCreated(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    TravelMode Mode,
    JourneyRoute Route,
    Money TotalPrice,
    TravelCategory Category,
    string OperatorId,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingConfirmed(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    TravelCategory Category,
    Money TotalPrice,
    PassengerCount Passengers,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingAmended(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    int AmendmentVersion,
    Itinerary PreviousItinerary,
    Itinerary NewItinerary,
    Money PreviousPrice,
    Money NewPrice,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingCancelled(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    string Reason,
    DateTimeOffset CancelledAt,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingEscalated(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingCompleted(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);
```

---

## 7. Application Layer

### 7.1 Supporting Types

```csharp
namespace UTOP.Booking.Application.Commands;

/// <summary>
/// Passenger data carried in ConfirmBookingCommand.
/// This is an application-layer DTO — not a domain entity.
/// It is translated to a Passenger entity inside the command handler.
/// </summary>
public sealed record PassengerDetail(
    string FirstName,
    string LastName,
    PassengerType Type,
    DateOnly DateOfBirth,
    string? DocumentNumber,
    string? Nationality);
```

### 7.2 Commands

```csharp
namespace UTOP.Booking.Application.Commands;

public sealed record CreateBookingCommand(
    TravelMode Mode,
    JourneyRoute Route,
    PassengerCount Passengers,
    TravelCategory Category,
    string OperatorId,
    Money Price,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    Location DeparturePoint,
    string DepartureCity,
    string DepartureCountry,
    Location ArrivalPoint,
    string ArrivalCity,
    string ArrivalCountry,
    string? CarrierReference,
    string? ServiceClass,
    string? GroupId,
    string? PilgrimageId,
    CorrelationId CorrelationId);

public sealed record ConfirmBookingCommand(
    BookingId BookingId,
    IReadOnlyList<PassengerDetail> Passengers,
    CorrelationId CorrelationId);

public sealed record AmendBookingCommand(
    BookingId BookingId,
    DateTimeOffset NewDepartureUtc,
    DateTimeOffset NewArrivalUtc,
    Location NewDeparturePoint,
    string NewDepartureCity,
    string NewDepartureCountry,
    Location NewArrivalPoint,
    string NewArrivalCity,
    string NewArrivalCountry,
    Money NewPrice,
    string? NewCarrierReference,
    string? NewServiceClass,
    CorrelationId CorrelationId);

public sealed record CancelBookingCommand(
    BookingId BookingId,
    string Reason,
    CorrelationId CorrelationId);

public sealed record StartJourneyCommand(
    BookingId BookingId,
    CorrelationId CorrelationId);

public sealed record CompleteBookingCommand(
    BookingId BookingId,
    CorrelationId CorrelationId);
```

### 7.3 Command Handlers

```csharp
namespace UTOP.Booking.Application.Commands;

public sealed class CreateBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IGroupExistenceValidator _groupValidator;
    private readonly IClock _clock;

    public CreateBookingCommandHandler(
        IBookingRepository repository,
        IGroupExistenceValidator groupValidator,
        IClock clock)
    {
        _repository = repository;
        _groupValidator = groupValidator;
        _clock = clock;
    }

    public async Task<BookingId> HandleAsync(CreateBookingCommand cmd, CancellationToken ct = default)
    {
        // Idempotency: return existing if same key already exists (ARCH-005 §1.4)
        var existing = await _repository.FindByIdempotencyKeyAsync(
            cmd.OperatorId, cmd.Mode, cmd.Route, cmd.DepartureUtc, ct);
        if (existing is not null)
            return existing.BookingId;

        // Validate group exists before association (BK-CINV-003)
        if (cmd.Category == TravelCategory.Group && cmd.GroupId is not null)
            await _groupValidator.ValidateGroupExistsAsync(cmd.GroupId, ct);

        var itinerary = Itinerary.Create(
            cmd.DepartureUtc,
            cmd.ArrivalUtc,
            cmd.DeparturePoint,
            cmd.DepartureCity,
            cmd.DepartureCountry,
            cmd.ArrivalPoint,
            cmd.ArrivalCity,
            cmd.ArrivalCountry,
            cmd.CarrierReference,
            cmd.ServiceClass);

        var booking = Booking.Create(
            cmd.Mode, cmd.Route, cmd.Passengers,
            cmd.Category, cmd.OperatorId, cmd.Price,
            itinerary, cmd.CorrelationId, _clock);

        if (cmd.GroupId is not null)
            booking.AssociateGroup(cmd.GroupId);

        if (cmd.PilgrimageId is not null)
            booking.AssociatePilgrimage(cmd.PilgrimageId);

        await _repository.SaveAsync(booking, ct);
        return booking.BookingId;
    }
}

public sealed class ConfirmBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IAvailabilityProvider _availability;
    private readonly IClock _clock;

    public ConfirmBookingCommandHandler(
        IBookingRepository repository,
        IAvailabilityProvider availability,
        IClock clock)
    {
        _repository = repository;
        _availability = availability;
        _clock = clock;
    }

    public async Task HandleAsync(ConfirmBookingCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new BookingNotFoundException(cmd.BookingId);

        foreach (var p in cmd.Passengers)
            booking.AddPassenger(Passenger.Create(
                p.FirstName, p.LastName, p.Type, p.DateOfBirth,
                p.DocumentNumber, p.Nationality));

        var available = await _availability.CheckAvailabilityAsync(
            booking.Route, booking.Itinerary.DepartureUtc, booking.Passengers, ct);

        if (!available)
        {
            booking.Escalate("Availability check failed.", cmd.CorrelationId, _clock);
            await _repository.SaveAsync(booking, ct);
            return;
        }

        booking.Confirm(cmd.CorrelationId, _clock);
        await _repository.SaveAsync(booking, ct);
    }
}

public sealed class AmendBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IClock _clock;

    public AmendBookingCommandHandler(IBookingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(AmendBookingCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new BookingNotFoundException(cmd.BookingId);

        var newItinerary = Itinerary.Create(
            cmd.NewDepartureUtc, cmd.NewArrivalUtc,
            cmd.NewDeparturePoint, cmd.NewDepartureCity, cmd.NewDepartureCountry,
            cmd.NewArrivalPoint, cmd.NewArrivalCity, cmd.NewArrivalCountry,
            cmd.NewCarrierReference, cmd.NewServiceClass);

        booking.Amend(newItinerary, cmd.NewPrice, cmd.CorrelationId, _clock);
        await _repository.SaveAsync(booking, ct);
    }
}

public sealed class CancelBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IClock _clock;

    public CancelBookingCommandHandler(IBookingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(CancelBookingCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new BookingNotFoundException(cmd.BookingId);

        booking.Cancel(cmd.Reason, cmd.CorrelationId, _clock);
        await _repository.SaveAsync(booking, ct);
    }
}

/// <summary>
/// Handles StartJourneyCommand — transitions Booking from Allocated → InTransit.
/// This command is triggered by the departure scheduler when DepartureUtc is reached,
/// or manually by an operator confirming physical departure.
/// CompleteBookingCommand is only valid AFTER this command has succeeded.
/// </summary>
public sealed class StartJourneyCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IClock _clock;

    public StartJourneyCommandHandler(IBookingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(StartJourneyCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new BookingNotFoundException(cmd.BookingId);

        booking.MarkInTransit(cmd.CorrelationId, _clock);
        await _repository.SaveAsync(booking, ct);
    }
}

public sealed class CompleteBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IClock _clock;

    public CompleteBookingCommandHandler(IBookingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(CompleteBookingCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new BookingNotFoundException(cmd.BookingId);

        // Booking must be InTransit before Complete() is called.
        // StartJourneyCommand must have been processed first.
        booking.Complete(cmd.CorrelationId, _clock);
        await _repository.SaveAsync(booking, ct);
    }
}
```

### 7.4 Inbound Integration Event Handler

```csharp
namespace UTOP.Booking.Application.EventHandlers;

/// <summary>
/// Handles ResourceAllocatedIntegrationEvent published by ResourceAllocation context.
/// Advances Booking status from Confirmed → Allocated.
/// Booking context does not own this event — it reacts to it.
/// Cross-schema query forbidden (ARCH-008). Only BookingId is used.
/// </summary>
public sealed class ResourceAllocatedEventHandler
{
    private readonly IBookingRepository _repository;
    private readonly IClock _clock;

    public ResourceAllocatedEventHandler(IBookingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(
        ResourceAllocatedIntegrationEvent evt,
        CancellationToken ct = default)
    {
        var bookingId = new BookingId(evt.BookingId);
        var booking = await _repository.GetByIdAsync(bookingId, ct);

        if (booking is null)
        {
            // Log warning — event may have arrived before booking persistence in edge case
            return;
        }

        var correlationId = CorrelationId.From(evt.CorrelationId);
        booking.MarkAllocated(correlationId, _clock);
        await _repository.SaveAsync(booking, ct);
    }
}

/// <summary>
/// Integration event shape received from ResourceAllocation context.
/// Defined here as a consumer DTO — not shared from ResourceAllocation.
/// No shared DTOs across contexts (ARCH-008 governance rules).
/// </summary>
public sealed record ResourceAllocatedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string ResourceId,
    DateTimeOffset OccurredAt);
```

### 7.5 Queries

```csharp
namespace UTOP.Booking.Application.Queries;

public sealed record GetBookingByIdQuery(BookingId BookingId);

public sealed record GetBookingsByOperatorQuery(
    string OperatorId,
    int Page = 1,
    int PageSize = 20);

public sealed record BookingReadModel(
    string BookingId,
    string Status,
    string Mode,
    string Category,
    string OriginCity,
    string OriginCountry,
    string? OriginAirportCode,
    string DestinationCity,
    string DestinationCountry,
    string? DestinationAirportCode,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    decimal TotalAmount,
    string Currency,
    int Adults,
    int Children,
    int Infants,
    string OperatorId,
    string? GroupId,
    string? PilgrimageId,
    int AmendmentVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Relocated here from Domain.Repositories (was the original LLD's placement) — its
/// return type BookingReadModel is an Application-layer type, so declaring this
/// interface in Domain would invert the dependency direction. The read-side port
/// belongs beside the read model it returns.
/// </summary>
public interface IBookingReadRepository
{
    Task<BookingReadModel?> GetByIdAsync(BookingId id, CancellationToken ct = default);
    Task<IReadOnlyList<BookingReadModel>> GetByOperatorAsync(
        string operatorId, int page, int pageSize, CancellationToken ct = default);
}

public sealed class GetBookingByIdQueryHandler
{
    private readonly IBookingReadRepository _read;

    public GetBookingByIdQueryHandler(IBookingReadRepository read) => _read = read;

    public async Task<BookingReadModel?> HandleAsync(
        GetBookingByIdQuery query,
        CancellationToken ct = default)
        => await _read.GetByIdAsync(query.BookingId, ct);
}

public sealed class GetBookingsByOperatorQueryHandler
{
    private readonly IBookingReadRepository _read;

    public GetBookingsByOperatorQueryHandler(IBookingReadRepository read) => _read = read;

    public async Task<IReadOnlyList<BookingReadModel>> HandleAsync(
        GetBookingsByOperatorQuery query,
        CancellationToken ct = default)
        => await _read.GetByOperatorAsync(query.OperatorId, query.Page, query.PageSize, ct);
}
```

---

## 8. Port Interfaces

```csharp
namespace UTOP.Booking.Application.Ports;

/// <summary>
/// Checks seat or service availability for a given route and departure.
/// Initial implementation: StubAvailabilityProvider — always returns true.
/// Production: connects to internal Inventory context or external GDS.
/// Never crosses schema boundary directly — port is the boundary.
/// </summary>
public interface IAvailabilityProvider
{
    Task<bool> CheckAvailabilityAsync(
        JourneyRoute route,
        DateTimeOffset departureUtc,
        PassengerCount passengers,
        CancellationToken ct = default);
}

/// <summary>
/// Validates that a group with the given Id exists and is active.
/// Prevents Booking from querying utop_group schema (ARCH-008 FORBIDDEN).
/// Initial implementation: StubGroupExistenceValidator — always passes.
/// </summary>
public interface IGroupExistenceValidator
{
    Task ValidateGroupExistsAsync(string groupId, CancellationToken ct = default);
}

/// <summary>
/// Fetches prayer schedule for a given location and date.
/// Used during pilgrimage booking leg validation (BK-TINV-005).
/// Returns DailyPrayerSchedule from Shared Kernel (ARCH-009 §8.4).
/// Initial implementation: StubPrayerTimeProvider — returns pre-defined Mecca schedule.
/// Production: Aladhan API or pre-computed offline dataset.
/// </summary>
public interface IPrayerTimeProvider
{
    Task<DailyPrayerSchedule> GetScheduleAsync(
        GeoCoordinate location,
        DateOnly date,
        string calculationMethod = "UmmAlQura",
        CancellationToken ct = default);
}
```

---

## 9. Infrastructure Contracts

### 9.1 Repository Interfaces

```csharp
namespace UTOP.Booking.Domain.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(BookingId id, CancellationToken ct = default);
    Task SaveAsync(Booking booking, CancellationToken ct = default);

    /// <summary>
    /// Idempotency check for CreateBooking (ARCH-005 §1.4).
    /// Key: SHA-256 of (operatorId + mode + route.Origin.Code + route.Destination.Code + departureUtc ISO8601).
    /// </summary>
    Task<Booking?> FindByIdempotencyKeyAsync(
        string operatorId,
        TravelMode mode,
        JourneyRoute route,
        DateTimeOffset departureUtc,
        CancellationToken ct = default);
}
```

`IBookingReadRepository` is **not** declared here — see §7.5. Its return type, `BookingReadModel`, lives in `Application.Queries`; declaring the interface in `Domain.Repositories` would make the Domain layer depend on an Application-layer type, inverting the dependency direction Clean Architecture requires. The read-side port belongs next to the read model it returns.

### 9.2 PostgreSQL Schema

All timestamp columns are `TIMESTAMPTZ` (UTC) per ARCH-009 §2.2. No `TIMESTAMP WITHOUT TIME ZONE` anywhere.

```sql
-- Schema: utop_booking

CREATE TABLE utop_booking.bookings (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id          VARCHAR(20) NOT NULL,
    mode                VARCHAR(20) NOT NULL,
    status              VARCHAR(30) NOT NULL,
    category            VARCHAR(20) NOT NULL,
    operator_id         VARCHAR(100) NOT NULL,
    group_id            VARCHAR(100) NULL,
    pilgrimage_id       VARCHAR(100) NULL,
    total_amount        NUMERIC(18,4) NOT NULL,
    currency            VARCHAR(10) NOT NULL,       -- Currency enum name e.g. 'SAR', 'USD'
    amendment_version   INT NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL,
    updated_at          TIMESTAMPTZ NOT NULL,
    row_version         INT NOT NULL DEFAULT 0      -- Optimistic concurrency token (BK-CONC-001)
);

-- Unique constraint on booking_id to prevent duplicates
ALTER TABLE utop_booking.bookings ADD CONSTRAINT uq_bookings_booking_id UNIQUE (booking_id);

CREATE TABLE utop_booking.itineraries (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id          UUID NOT NULL REFERENCES utop_booking.bookings(id) ON DELETE CASCADE,
    departure_utc       TIMESTAMPTZ NOT NULL,
    arrival_utc         TIMESTAMPTZ NOT NULL,
    departure_city      VARCHAR(100) NOT NULL,
    departure_country   VARCHAR(3) NOT NULL,
    departure_airport   VARCHAR(10) NULL,
    arrival_city        VARCHAR(100) NOT NULL,
    arrival_country     VARCHAR(3) NOT NULL,
    arrival_airport     VARCHAR(10) NULL,
    carrier_reference   VARCHAR(20) NULL,
    service_class       VARCHAR(20) NULL,
    CONSTRAINT uq_itineraries_booking UNIQUE (booking_id)  -- one active itinerary per booking
);

CREATE TABLE utop_booking.passengers (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id          UUID NOT NULL REFERENCES utop_booking.bookings(id) ON DELETE CASCADE,
    first_name          TEXT NOT NULL,      -- AES-256 encrypted at rest (PII)
    last_name           TEXT NOT NULL,      -- AES-256 encrypted at rest (PII)
    passenger_type      VARCHAR(10) NOT NULL,
    date_of_birth       DATE NOT NULL,      -- DATE is timezone-neutral (ARCH-009 §2.1 exception)
    document_number     TEXT NULL,          -- AES-256 encrypted at rest (PII)
    nationality         VARCHAR(3) NULL
);

-- Outbox for reliable event publishing to RabbitMQ (ARCH-006 outbox pattern)
CREATE TABLE utop_booking.outbox_events (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id          UUID NOT NULL,
    event_type          VARCHAR(150) NOT NULL,
    payload             JSONB NOT NULL,
    correlation_id      UUID NOT NULL,
    occurred_at         TIMESTAMPTZ NOT NULL,
    published_at        TIMESTAMPTZ NULL,           -- NULL = unpublished
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Idempotency store for CreateBooking (ARCH-005 §1.4)
CREATE TABLE utop_booking.idempotency_keys (
    key_hash            CHAR(64) PRIMARY KEY,       -- SHA-256 hex of composite key
    booking_id          VARCHAR(20) NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Indexes
CREATE INDEX ix_bookings_operator      ON utop_booking.bookings(operator_id);
CREATE INDEX ix_bookings_status        ON utop_booking.bookings(status);
CREATE INDEX ix_bookings_group_id      ON utop_booking.bookings(group_id) WHERE group_id IS NOT NULL;
CREATE INDEX ix_bookings_pilgrimage_id ON utop_booking.bookings(pilgrimage_id) WHERE pilgrimage_id IS NOT NULL;
CREATE INDEX ix_itineraries_departure  ON utop_booking.itineraries(departure_utc);
CREATE INDEX ix_outbox_unpublished     ON utop_booking.outbox_events(created_at) WHERE published_at IS NULL;
```

### 9.3 EF Core Configuration

```csharp
namespace UTOP.Booking.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings", "utop_booking");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingId)
            .HasConversion(id => id.Value, v => new BookingId(v))
            .HasColumnName("booking_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.Mode)
            .HasConversion<string>()
            .HasColumnName("mode");

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasColumnName("status");

        builder.Property(b => b.Category)
            .HasConversion<string>()
            .HasColumnName("category");

        // Money uses OwnsOne — splits into two columns.
        // HasConversion with anonymous type does not work in EF Core.
        builder.OwnsOne(b => b.TotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasColumnType("NUMERIC(18,4)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasConversion<string>()
                .HasColumnName("currency")
                .IsRequired();
        });

        builder.Property(b => b.OperatorId).HasColumnName("operator_id").HasMaxLength(100);
        builder.Property(b => b.GroupId).HasColumnName("group_id").HasMaxLength(100);
        builder.Property(b => b.PilgrimageId).HasColumnName("pilgrimage_id").HasMaxLength(100);
        builder.Property(b => b.AmendmentVersion).HasColumnName("amendment_version");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        // Optimistic concurrency (BK-CONC-001)
        builder.Property<int>("row_version")
            .HasColumnName("row_version")
            .IsConcurrencyToken();

        builder.HasOne(b => b.Itinerary)
            .WithOne()
            .HasForeignKey<Itinerary>("booking_id")
            .IsRequired();

        builder.HasMany(b => b.PassengerList)
            .WithOne()
            .HasForeignKey("booking_id");

        builder.Ignore(b => b.DomainEvents);
    }
}

public sealed class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.ToTable("itineraries", "utop_booking");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DepartureUtc).HasColumnName("departure_utc").IsRequired();
        builder.Property(i => i.ArrivalUtc).HasColumnName("arrival_utc").IsRequired();
        builder.Property(i => i.DepartureCity).HasColumnName("departure_city").HasMaxLength(100).IsRequired();
        builder.Property(i => i.DepartureCountry).HasColumnName("departure_country").HasMaxLength(3).IsRequired();
        builder.Property(i => i.ArrivalCity).HasColumnName("arrival_city").HasMaxLength(100).IsRequired();
        builder.Property(i => i.ArrivalCountry).HasColumnName("arrival_country").HasMaxLength(3).IsRequired();
        builder.Property(i => i.CarrierReference).HasColumnName("carrier_reference").HasMaxLength(20);
        builder.Property(i => i.ServiceClass).HasColumnName("service_class").HasMaxLength(20);

        // Location owned types — Code maps to airport/stop code column.
        // City and country are separate Itinerary properties above; Location does not carry them.
        builder.OwnsOne(i => i.DeparturePoint, loc =>
        {
            loc.Property(l => l.Code).HasColumnName("departure_airport").HasMaxLength(10);
            loc.Ignore(l => l.Type);
            loc.Ignore(l => l.DisplayName);
        });

        builder.OwnsOne(i => i.ArrivalPoint, loc =>
        {
            loc.Property(l => l.Code).HasColumnName("arrival_airport").HasMaxLength(10);
            loc.Ignore(l => l.Type);
            loc.Ignore(l => l.DisplayName);
        });
    }
}

public sealed class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.ToTable("passengers", "utop_booking");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .IsRequired();

        builder.Property(p => p.Type)
            .HasConversion<string>()
            .HasColumnName("passenger_type")
            .IsRequired();

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth")
            .IsRequired();

        builder.Property(p => p.DocumentNumber)
            .HasColumnName("document_number");

        builder.Property(p => p.Nationality)
            .HasColumnName("nationality")
            .HasMaxLength(3);
    }
}

```

---

## 10. Integration Points

### 10.1 Integration Events Published

Domain events are translated to integration events at the `BookingEventPublisher` infrastructure boundary. Internal domain types do not leak into integration events.

```csharp
namespace UTOP.Booking.Infrastructure.Messaging;

// Routing key: booking.created
public sealed record BookingCreatedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Mode,
    string Category,
    string OriginCity,
    string OriginCountry,
    string? OriginAirportCode,
    string DestinationCity,
    string DestinationCountry,
    string? DestinationAirportCode,
    decimal TotalAmount,
    string Currency,
    int Adults,
    int Children,
    int Infants,
    string OperatorId,
    string? GroupId,
    string? PilgrimageId,
    DateTimeOffset DepartureUtc,
    DateTimeOffset OccurredAt);

// Routing key: booking.confirmed
public sealed record BookingConfirmedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Category,
    decimal TotalAmount,
    string Currency,
    int Adults,
    int Children,
    int Infants,
    DateTimeOffset DepartureUtc,
    DateTimeOffset OccurredAt);

// Routing key: booking.amended
public sealed record BookingAmendedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    int AmendmentVersion,
    DateTimeOffset NewDepartureUtc,
    DateTimeOffset NewArrivalUtc,
    decimal NewTotalAmount,
    string Currency,
    DateTimeOffset OccurredAt);

// Routing key: booking.cancelled
public sealed record BookingCancelledIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Reason,
    DateTimeOffset CancelledAt,
    DateTimeOffset OccurredAt);

// Routing key: booking.escalated
public sealed record BookingEscalatedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Reason,
    DateTimeOffset OccurredAt);

// Routing key: booking.completed
public sealed record BookingCompletedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    DateTimeOffset OccurredAt);
```

### 10.2 Events Consumed

```
Inbound integration event:
  ResourceAllocatedIntegrationEvent  (from ResourceAllocation context)
    → Handler: ResourceAllocatedEventHandler
    → Action: booking.MarkAllocated()
    → Subscribed via: utop.booking.queue (new queue binding: resource.allocated)
```

### 10.3 RabbitMQ Topology

```
Exchange: utop.events (topic, durable: true)

Published (routing keys):
  booking.created
  booking.confirmed
  booking.amended
  booking.cancelled
  booking.escalated
  booking.completed

Consumed (new binding on existing queue or dedicated queue):
  resource.allocated  →  utop.booking-allocation.queue

Downstream consumers of booking events (ARCH-007 cross-reference):
  ResourceAllocation  → booking.confirmed, booking.cancelled
  Notifications       → booking.*
  Analytics           → booking.* (via utop.analytics.queue)
  CostSplitting       → booking.confirmed
  Audit               → booking.* (via utop.audit.queue)
```

---

## 11. Shared Kernel Usage

| Type | How used |
|---|---|
| `Money` | `Booking.TotalPrice`; arithmetic in `Amend()`; mapped via `OwnsOne` in EF Core |
| `Location` | `JourneyRoute.Origin/Destination`; `Itinerary.DeparturePoint/ArrivalPoint` |
| `PassengerCount` | `Booking.Passengers`; count validated against `_passengers` list at `Confirm()` |
| `CorrelationId` | Carried on every command and every domain event |
| `IClock` | Injected into `Create()`, `Confirm()`, `Amend()`, `Cancel()`, `Escalate()`, `MarkAllocated()`, `MarkInTransit()`, `Complete()` |
| `DailyPrayerSchedule` | Consumed via `IPrayerTimeProvider` stub adapter for pilgrimage validation |
| `DateRange` | Not used directly — Booking uses `DateTimeOffset` pair on Itinerary |

No new Shared Kernel admission requests from the Booking context.

---

## 12. Test Strategy

### 12.1 Unit Tests — Domain Layer

`FakeClock` is used in every temporal test. `DateTime.UtcNow` never appears in test code.

```
// Naming: [Aggregate]_[InvariantId]_[Condition]_[ExpectedOutcome]

Booking_BKINV010_PriceIsZero_CreateThrows()
Booking_BKINV010_PriceIsPositive_CreateSucceeds()
Booking_BKINV010_PriceIsNegative_CreateThrows()

Booking_BKINV011_OriginEqualsDestination_CreateThrows()
Booking_BKINV011_OriginDiffersDestination_CreateSucceeds()

Booking_BKINV012_OperatorIdIsNull_CreateThrows()
Booking_BKINV012_OperatorIdIsEmpty_CreateThrows()

Booking_BKTINV001_DepartureInPast_CreateThrows()
Booking_BKTINV001_DepartureIsNow_CreateThrows()
Booking_BKTINV001_DepartureInFuture_CreateSucceeds()

Booking_BKINV004_NoAdultInManifest_ConfirmThrows()
Booking_BKINV004_OneAdultPresent_ConfirmSucceeds()

Booking_BKINV005_ManifestCountBelowPassengerCount_ConfirmThrows()
Booking_BKINV005_ManifestCountExceedsPassengerCount_AddPassengerThrows()
Booking_BKINV005_ManifestCountMatchesPassengerCount_ConfirmSucceeds()
Booking_BKINV005_RemovePassenger_NotPresent_IsIdempotent()
Booking_BKINV005_RemovePassenger_DoesNotReducePassengerCount()
Booking_BKINV007_StatusCompleted_AddPassengerThrows()
Booking_BKINV007_StatusCompleted_RemovePassengerThrows()

Booking_BKTINV004_DeparturePassedAtConfirm_ConfirmThrows()

Booking_BKINV008_ReligiousNoPilgrimageId_ConfirmThrows()
Booking_BKINV008_ReligiousWithPilgrimageId_ConfirmSucceeds()

Booking_BKINV009_GroupNoGroupId_ConfirmThrows()
Booking_BKINV009_GroupWithGroupId_ConfirmSucceeds()

Booking_BKINV006_StatusCancelled_ConfirmThrows()
Booking_BKINV006_StatusCancelled_CancelIsIdempotent()
Booking_BKINV006_StatusRefunded_CancelIsIdempotent()

Booking_BKINV007_StatusCompleted_AnyMutationThrows()

Booking_BKINV003_AmendWithDifferentCurrency_Throws()
Booking_BKINV003_AmendWithSameCurrency_Succeeds()

Booking_BKTINV002_AmendWithin2Hours_Throws()
Booking_BKTINV002_AmendExactly2HoursBefore_Throws()
Booking_BKTINV002_AmendBeyond2Hours_Succeeds()

Booking_BKINV014_AmendReplacesItineraryAtomically()

Booking_BKCONC001_ConcurrentAmendSameVersion_SecondWriteRejected()

Booking_StatusTransitions_AllForbiddenTransitionsThrow()
```

### 12.2 Integration Tests — Application Layer

```
CreateBookingCommandHandler_ValidCommand_ReturnsBookingId()
CreateBookingCommandHandler_DuplicateIdempotencyKey_ReturnsExistingId()
ConfirmBookingCommandHandler_AvailabilityFails_EscalatesBooking()
ConfirmBookingCommandHandler_AvailabilityConfirmed_StatusIsConfirmed()
AmendBookingCommandHandler_ValidAmendment_VersionIncremented()
CancelBookingCommandHandler_AlreadyCancelled_IsIdempotent()
StartJourneyCommandHandler_Allocated_StatusIsInTransit()
StartJourneyCommandHandler_NotAllocated_ThrowsInvalidStateTransition()
CompleteBookingCommandHandler_InTransit_StatusIsCompleted()
CompleteBookingCommandHandler_Allocated_ThrowsInvalidStateTransition()
ResourceAllocatedEventHandler_ValidEvent_StatusIsAllocated()
ResourceAllocatedEventHandler_BookingNotFound_DoesNotThrow()
```

### 12.3 Stub Implementations

| Port | Stub | Behaviour |
|---|---|---|
| `IAvailabilityProvider` | `StubAvailabilityProvider` | Always returns `true` |
| `IGroupExistenceValidator` | `StubGroupExistenceValidator` | Always passes (no exception) |
| `IPrayerTimeProvider` | `StubPrayerTimeProvider` | Returns pre-defined Mecca schedule for any input |
| `IClock` | `FakeClock` (Shared Kernel) | Deterministic; caller controls time |

---

## 13. Domain Exceptions

```csharp
// Namespace: UTOP.Booking.Domain.Exceptions
BookingNotFoundException
BookingPriceMustBePositiveException
BookingRouteOriginEqualsDestinationException
BookingOperatorIdRequiredException
BookingDepartureAlreadyPassedException
BookingRequiresAdultPassengerException
InvalidBookingStateTransitionException
BookingAlreadyCompletedException
BookingAmendmentWindowExpiredException
CurrencyImmutableAfterConfirmationException
PilgrimageBookingRequiresPilgrimageAssociationException
GroupBookingRequiresGroupAssociationException
PassengerCountMismatchException
InvalidItineraryScheduleException
```

---

## 14. Open Items

| ID | Item | Severity | Resolution Path |
|---|---|---|---|
| UTOP-LLD-BK-01 | PII encryption at rest for `first_name`, `last_name`, `document_number` | Medium | Shared infrastructure decision — AES-256 via EF Core value converter; key management LLD |
| UTOP-LLD-BK-02 | Refund amount derivation — `Cancel()` records timestamp but does not calculate refund | Low | CostSplitting LLD — refund policy is a CostSplitting concern per ARCH-008 |
| UTOP-LLD-BK-03 | Manager escalation resolution — confirm or reject an `Escalated` booking | Medium | Identity/Manager workflow LLD — requires manager-facing command handler |
| UTOP-LLD-BK-04 | Outbox processor — background service polling `outbox_events` and publishing to RabbitMQ | Medium | Shared infrastructure LLD — one processor pattern applies platform-wide |
| UTOP-LLD-BK-05 | `row_version` concurrency — EF Core `IsConcurrencyToken()` on integer requires explicit increment in `SaveAsync()`. Consider PostgreSQL `xmin` system column as alternative | Low | Implementation decision at coding phase |
| UTOP-LLD-LOCALTIME-01 | `LocalizedTime` type-system enforcement | Low | Localization LLD (carried from ARCH-009) |

---

*Document owner: UTOP Architecture Board*  
*Baselined: Phase 4 — Low-Level Design*  
*Supersedes: UTOP-ARCH-003 on the specific items listed in the corrections table*
