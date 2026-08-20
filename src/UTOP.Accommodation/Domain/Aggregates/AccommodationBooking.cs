using UTOP.Accommodation.Domain.Entities;
using UTOP.Accommodation.Domain.Events;
using UTOP.Accommodation.Domain.Exceptions;
using UTOP.Accommodation.Domain.ValueObjects;
using UTOP.Shared;
using UTOP.Shared.Domain.ValueObjects;
using UTOP.Shared.Time;

namespace UTOP.Accommodation.Domain.Aggregates;

public sealed class AccommodationBooking : AggregateRoot
{
    public AccommodationBookingId AccommodationBookingId { get; private set; } = null!;
    public string BookingId { get; private set; } = null!;
    public string? LinkedPilgrimageId { get; private set; }
    public Location Property { get; private set; } = null!;
    public string PropertyExternalReference { get; private set; } = null!;
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public Money TotalPrice { get; private set; } = null!;
    public AccommodationBookingStatus Status { get; private set; }
    public string PrimaryGuestName { get; private set; } = null!;
    public int AmendmentVersion { get; private set; }
    public long Version { get; private set; }

    private readonly List<Room> _rooms = new();
    public IReadOnlyList<Room> Rooms => _rooms.AsReadOnly();
    private readonly List<AncillaryService> _ancillaryServices = new();
    public IReadOnlyList<AncillaryService> AncillaryServices => _ancillaryServices.AsReadOnly();

    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;

    private AccommodationBooking() { }

    public static AccommodationBooking Create(
        string bookingId, Location property, string propertyExternalReference,
        DateOnly checkIn, DateOnly checkOut, Money price, string primaryGuestName,
        CorrelationId correlationId, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(bookingId))
            throw new AccommodationBookingIdRequiredException();
        if (string.IsNullOrWhiteSpace(propertyExternalReference))
            throw new PropertyExternalReferenceRequiredException();
        if (property is null || string.IsNullOrWhiteSpace(property.Code))
            throw new InvalidPropertyIdentityException();
        if (price.Amount <= 0)
            throw new AccommodationPriceMustBePositiveException();
        if (checkOut.DayNumber - checkIn.DayNumber < 1)
            throw new InvalidStayDurationException(checkIn, checkOut);
        if (string.IsNullOrWhiteSpace(primaryGuestName))
            throw new ArgumentException("Primary guest name is required.", nameof(primaryGuestName));

        var checkInUtc = checkIn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (checkInUtc <= clock.UtcNow.UtcDateTime)
            throw new AccommodationCheckInAlreadyPassedException(checkIn, clock.UtcNow);

        var now = clock.UtcNow;

        var booking = new AccommodationBooking
        {
            Id = Guid.NewGuid(),
            AccommodationBookingId = AccommodationBookingId.Generate(DateOnly.FromDateTime(now.UtcDateTime)),
            BookingId = bookingId,
            Property = property,
            PropertyExternalReference = propertyExternalReference,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            TotalPrice = price,
            PrimaryGuestName = primaryGuestName,
            Status = AccommodationBookingStatus.Requested,
            AmendmentVersion = 0,
            Version = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        booking.AddDomainEvent(new AccommodationBookingCreated(
            Guid.NewGuid(), correlationId, booking.Id.ToString(), nameof(AccommodationBooking),
            booking.AccommodationBookingId, bookingId, property, checkIn, checkOut, price, now));

        return booking;
    }

    public void Confirm(CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.Requested)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.Confirmed);
        if (_rooms.Count == 0)
            throw new AccommodationRequiresRoomException(AccommodationBookingId);
        if (_rooms.Sum(r => r.OccupantCount) == 0)
            throw new AccommodationRequiresOccupantException(AccommodationBookingId);

        var now = clock.UtcNow;
        Status = AccommodationBookingStatus.Confirmed;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationBookingConfirmed(
            Guid.NewGuid(), correlationId, Id.ToString(), nameof(AccommodationBooking),
            AccommodationBookingId, BookingId, TotalPrice, now));
    }

    public void Amend(DateOnly newCheckIn, DateOnly newCheckOut, Money newPrice, CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.Confirmed)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.Confirmed);
        if (newCheckOut.DayNumber - newCheckIn.DayNumber < 1)
            throw new InvalidStayDurationException(newCheckIn, newCheckOut);

        var newCheckInUtc = newCheckIn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (newCheckInUtc <= clock.UtcNow.UtcDateTime)
            throw new AccommodationCheckInAlreadyPassedException(newCheckIn, clock.UtcNow);
        if (newPrice.Currency != TotalPrice.Currency)
            throw new AccommodationCurrencyImmutableAfterConfirmationException(AccommodationBookingId, TotalPrice.Currency, newPrice.Currency);

        var previousCheckIn = CheckInDate;
        var previousCheckOut = CheckOutDate;
        var previousPrice = TotalPrice;
        var now = clock.UtcNow;

        CheckInDate = newCheckIn;
        CheckOutDate = newCheckOut;
        TotalPrice = newPrice;
        AmendmentVersion++;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationBookingAmended(
            Guid.NewGuid(), correlationId, Id.ToString(), nameof(AccommodationBooking),
            AccommodationBookingId, AmendmentVersion,
            previousCheckIn, previousCheckOut, newCheckIn, newCheckOut,
            previousPrice, newPrice, now));
    }

    public void Cancel(string reason, TimeSpan cancellationCutoff, CorrelationId correlationId, IClock clock)
    {
        if (Status == AccommodationBookingStatus.Cancelled)
            return;
        if (Status is AccommodationBookingStatus.CheckedIn or AccommodationBookingStatus.CheckedOut or AccommodationBookingStatus.NoShow)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.Cancelled);

        if (Status == AccommodationBookingStatus.Confirmed)
        {
            var checkInUtc = new DateTimeOffset(CheckInDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            if (checkInUtc - clock.UtcNow <= cancellationCutoff)
                throw new AccommodationCancellationWindowExpiredException(AccommodationBookingId, checkInUtc, clock.UtcNow);
        }

        var now = clock.UtcNow;
        Status = AccommodationBookingStatus.Cancelled;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationBookingCancelled(
            Guid.NewGuid(), correlationId, Id.ToString(), nameof(AccommodationBooking),
            AccommodationBookingId, reason, now));
    }

    public void CheckIn(CorrelationId correlationId, IClock clock)
    {
        if (Status != AccommodationBookingStatus.Confirmed)
            throw new InvalidAccommodationStateTransitionException(AccommodationBookingId, Status, AccommodationBookingStatus.CheckedIn);

        var now = clock.UtcNow;
        var checkInDateUtc = new DateTimeOffset(CheckInDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        if (now < checkInDateUtc)
            throw new AccommodationCheckInTooEarlyException(AccommodationBookingId, checkInDateUtc, now);

        Status = AccommodationBookingStatus.CheckedIn;
        UpdatedAt = now;
        Version++;

        AddDomainEvent(new AccommodationGuestCheckedIn(
            Guid.NewGuid(), correlationId, Id.ToString(), nameof(AccommodationBooking), AccommodationBookingId, now));
    }

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

    public void LinkToPilgrimage(string pilgrimageId, IClock clock)
    {
        LinkedPilgrimageId = pilgrimageId;
        UpdatedAt = clock.UtcNow;
    }

    public void AddRoom(Room room)
    {
        if (_rooms.Any(r => r.Id == room.Id)) return;
        if (_rooms.Any(r => r.ProviderRoomReference == room.ProviderRoomReference))
            throw new DuplicateRoomException(room.ProviderRoomReference);
        _rooms.Add(room);
    }

    public void AddAncillaryService(AncillaryService service)
    {
        if (_ancillaryServices.Any(s => s.Id == service.Id)) return;
        _ancillaryServices.Add(service);
    }
}