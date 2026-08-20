using UTOP.Accommodation.Application.Queries;
using UTOP.Accommodation.Domain.Repositories;
using UTOP.Accommodation.Infrastructure.Messaging;
using UTOP.Shared.Time;

namespace UTOP.Accommodation.Application.Handlers;

/// <summary>
/// Sets the passive pilgrimage correlation reference. No business logic — this context
/// never owns pilgrimage compliance decisions (ARCH-008 §2).
/// </summary>
public sealed class PilgrimageConfirmedEventHandler
{
    private readonly IAccommodationBookingRepository _repository;
    private readonly IAccommodationBookingReadRepository _readRepository;
    private readonly IClock _clock;

    public PilgrimageConfirmedEventHandler(
        IAccommodationBookingRepository repository,
        IAccommodationBookingReadRepository readRepository,
        IClock clock)
    {
        _repository = repository;
        _readRepository = readRepository;
        _clock = clock;
    }

    public async Task HandleAsync(PilgrimageConfirmedIntegrationEvent evt, CancellationToken ct = default)
    {
        var bookings = await _readRepository.GetByBookingIdsAsync(evt.PilgrimBookingIds, ct);

        foreach (var booking in bookings)
        {
            booking.LinkToPilgrimage(evt.PilgrimageId, _clock);
            await _repository.SaveAsync(booking, ct);
        }
    }
}