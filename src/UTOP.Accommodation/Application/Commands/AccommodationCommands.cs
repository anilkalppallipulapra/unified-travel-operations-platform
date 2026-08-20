using UTOP.Accommodation.Domain.Entities;
using UTOP.Accommodation.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Accommodation.Application.Commands;

public sealed record OccupantDetail(
    string Name,
    OccupantType Type,
    int? Age);

public sealed record RoomSelection(
    RoomType Type,
    Money RatePerNight,
    string ProviderRoomReference,
    IReadOnlyList<OccupantDetail> Occupants);

public sealed record CreateAccommodationBookingCommand(
    string BookingId,
    Location Property,
    string PropertyExternalReference,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    Money Price,
    string PrimaryGuestName,
    IReadOnlyList<RoomSelection> Rooms,
    CorrelationId CorrelationId);

public sealed record ConfirmAccommodationBookingCommand(
    AccommodationBookingId AccommodationBookingId,
    CorrelationId CorrelationId);

public sealed record AmendAccommodationBookingCommand(
    AccommodationBookingId AccommodationBookingId,
    DateOnly NewCheckInDate,
    DateOnly NewCheckOutDate,
    Money NewPrice,
    CorrelationId CorrelationId);

public sealed record CancelAccommodationBookingCommand(
    AccommodationBookingId AccommodationBookingId,
    string Reason,
    CorrelationId CorrelationId);

public sealed record CheckInCommand(
    AccommodationBookingId AccommodationBookingId,
    CorrelationId CorrelationId);

public sealed record CheckOutCommand(
    AccommodationBookingId AccommodationBookingId,
    CorrelationId CorrelationId);

public sealed record RecordNoShowCommand(
    AccommodationBookingId AccommodationBookingId,
    CorrelationId CorrelationId);