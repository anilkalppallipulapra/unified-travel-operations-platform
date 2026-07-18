using UTOP.Booking.Domain.Entities;
using UTOP.Booking.Domain.Exceptions;
using UTOP.Booking.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Booking.Application.Commands;

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
