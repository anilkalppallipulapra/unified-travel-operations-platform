using UTOP.Booking.Domain.Exceptions;
using UTOP.Booking.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Booking.Application.Commands;

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
