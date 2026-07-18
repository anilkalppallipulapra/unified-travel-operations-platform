using UTOP.Booking.Domain.Entities;
using UTOP.Booking.Domain.Events;
using UTOP.Booking.Domain.Exceptions;
using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;
using UTOP.Shared;
using UTOP.Shared.Time;

namespace UTOP.Booking.Domain.Aggregates;

public sealed class Booking : AggregateRoot
{
    public BookingId BookingId { get; private set; } = null!;
    public TravelMode Mode { get; private set; }
    public JourneyRoute Route { get; private set; } = null!;
    public PassengerCount Passengers { get; private set; }
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
    /// BK-INV-007: forbidden once Completed (guard added here — original LLD draft omitted it).
    /// Full equality is enforced at Confirm() time.
    /// Idempotent on duplicate passenger Id.
    /// </summary>
    public void AddPassenger(Passenger passenger)
    {
        // BK-INV-007
        if (Status == BookingStatus.Completed)
            throw new BookingAlreadyCompletedException(BookingId);

        if (_passengers.Any(p => p.Id == passenger.Id))
            return;

        if (_passengers.Count >= Passengers.Total)
            throw new PassengerCountMismatchException(BookingId, Passengers.Total, _passengers.Count + 1);

        _passengers.Add(passenger);
    }

    /// <summary>
    /// Removes a passenger from the manifest.
    /// Does NOT reduce Passengers (PassengerCount) — capacity/pricing commitment stays fixed;
    /// removing a passenger just means the manifest is incomplete again until re-added.
    /// Reducing party size is out of scope here — it is a pricing/cost concern
    /// (see BK-INV-005 discussion and UTOP-LLD-BK-02, deferred to CostSplitting).
    /// BK-INV-007: forbidden once Completed. Idempotent if passenger not present.
    /// </summary>
    public void RemovePassenger(Guid passengerId)
    {
        // BK-INV-007
        if (Status == BookingStatus.Completed)
            throw new BookingAlreadyCompletedException(BookingId);

        var passenger = _passengers.FirstOrDefault(p => p.Id == passengerId);
        if (passenger is null)
            return; // idempotent — mirrors AddPassenger's duplicate-Id idempotency

        _passengers.Remove(passenger);
    }
}
