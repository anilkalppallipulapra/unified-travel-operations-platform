using UTOP.Booking.Domain.Exceptions;
using UTOP.Booking.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Booking.Application.Commands;

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
