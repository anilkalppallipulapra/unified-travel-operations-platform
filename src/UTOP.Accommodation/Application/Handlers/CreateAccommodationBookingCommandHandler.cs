using UTOP.Accommodation.Application.Commands;
using UTOP.Accommodation.Domain.Entities;
using UTOP.Accommodation.Domain.Repositories;
using UTOP.Shared.Time;
using AccommodationAggregate = UTOP.Accommodation.Domain.Aggregates.AccommodationBooking;

namespace UTOP.Accommodation.Application.Handlers;

public sealed class CreateAccommodationBookingCommandHandler
{
    private readonly IAccommodationBookingRepository _repository;
    private readonly IClock _clock;

    public CreateAccommodationBookingCommandHandler(
        IAccommodationBookingRepository repository,
        IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Domain.ValueObjects.AccommodationBookingId> HandleAsync(
        CreateAccommodationBookingCommand cmd, CancellationToken ct = default)
    {
        var booking = AccommodationAggregate.Create(
            cmd.BookingId, cmd.Property, cmd.PropertyExternalReference,
            cmd.CheckInDate, cmd.CheckOutDate, cmd.Price, cmd.PrimaryGuestName,
            cmd.CorrelationId, _clock);

        foreach (var roomSelection in cmd.Rooms)
        {
            var room = Room.Create(roomSelection.Type, roomSelection.RatePerNight, roomSelection.ProviderRoomReference);
            foreach (var occupantDetail in roomSelection.Occupants)
                room.AddOccupant(Occupant.Create(occupantDetail.Name, occupantDetail.Type, occupantDetail.Age));

            booking.AddRoom(room);
        }

        await _repository.SaveAsync(booking, ct);
        return booking.AccommodationBookingId;
    }
}