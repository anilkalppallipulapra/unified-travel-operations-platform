using UTOP.Accommodation.Application.Commands;
using UTOP.Accommodation.Domain.Exceptions;
using UTOP.Accommodation.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Accommodation.Application.Handlers;

public sealed class CheckOutCommandHandler
{
    private readonly IAccommodationBookingRepository _repository;
    private readonly IClock _clock;

    public CheckOutCommandHandler(
        IAccommodationBookingRepository repository,
        IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(CheckOutCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.AccommodationBookingId, ct)
            ?? throw new AccommodationBookingNotFoundException(cmd.AccommodationBookingId);

        booking.CheckOut(cmd.CorrelationId, _clock);

        await _repository.SaveAsync(booking, ct);
    }
}