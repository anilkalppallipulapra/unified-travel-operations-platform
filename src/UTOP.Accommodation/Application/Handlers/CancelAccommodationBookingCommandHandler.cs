using UTOP.Accommodation.Application.Commands;
using UTOP.Accommodation.Domain.Exceptions;
using UTOP.Accommodation.Domain.Repositories;
using UTOP.Shared.Time;

namespace UTOP.Accommodation.Application.Handlers;

public sealed class CancelAccommodationBookingCommandHandler
{
    private readonly IAccommodationBookingRepository _repository;
    private readonly IClock _clock;
    private readonly TimeSpan _cancellationCutoff;

    // AC-TINV-002 — cutoff value is UTOP-LLD-ACM-01, an open item with no business-decided
    // value yet. Injected here so it's externalized via config (appsettings.json), not
    // hardcoded, per BOOKING-001 Decision 6 precedent. Update appsettings once a value lands.
    public CancelAccommodationBookingCommandHandler(
        IAccommodationBookingRepository repository,
        IClock clock,
        TimeSpan cancellationCutoff)
    {
        _repository = repository;
        _clock = clock;
        _cancellationCutoff = cancellationCutoff;
    }

    public async Task HandleAsync(CancelAccommodationBookingCommand cmd, CancellationToken ct = default)
    {
        var booking = await _repository.GetByIdAsync(cmd.AccommodationBookingId, ct)
            ?? throw new AccommodationBookingNotFoundException(cmd.AccommodationBookingId);

        booking.Cancel(cmd.Reason, _cancellationCutoff, cmd.CorrelationId, _clock);

        await _repository.SaveAsync(booking, ct);
    }
}