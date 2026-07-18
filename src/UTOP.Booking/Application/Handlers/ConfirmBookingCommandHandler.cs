using UTOP.Booking.Application.Ports;
using UTOP.Booking.Domain.Entities;
using UTOP.Booking.Domain.Exceptions;
using UTOP.Booking.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Booking.Application.Commands;

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
