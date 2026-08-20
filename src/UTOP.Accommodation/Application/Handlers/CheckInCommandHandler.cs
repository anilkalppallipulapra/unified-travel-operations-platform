using UTOP.Accommodation.Application.Commands;
using UTOP.Accommodation.Domain.Exceptions;
using UTOP.Accommodation.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Accommodation.Application.Handlers;

public sealed class CheckInCommandHandler
{
    private readonly IAccommodationBookingRepository _repository;
    private readonly IClock _clock;

    public CheckInCommandHandler(
        IAccommodationBookingRepository repository,
        IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(CheckInCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.AccommodationBookingId, ct)
            ?? throw new AccommodationBookingNotFoundException(cmd.AccommodationBookingId);

        booking.CheckIn(cmd.CorrelationId, _clock);

        await _repository.SaveAsync(booking, ct);
    }
}