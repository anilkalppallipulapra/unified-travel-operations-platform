using UTOP.Booking.Domain.Exceptions;
using UTOP.Booking.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Booking.Application.Commands;

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
