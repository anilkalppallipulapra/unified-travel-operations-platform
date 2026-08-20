using UTOP.Accommodation.Domain.Repositories;
using UTOP.Accommodation.Domain.ValueObjects;
using UTOP.Shared.Time;
using UTOP.Accommodation.Application.Queries;   // for IAccommodationBookingReadRepository
using UTOP.Accommodation.Infrastructure.Messaging;  // for BookingCancelledIntegrationEvent

namespace UTOP.Accommodation.Application.Handlers;

/// <summary>
/// Releases the accommodation hold when the associated travel booking is cancelled.
/// No-op if no matching AccommodationBooking is found (mirrors Booking's
/// ResourceAllocatedEventHandler tolerance pattern).
/// </summary>
public sealed class BookingCancelledEventHandler
{
    private readonly IAccommodationBookingRepository _repository;
    private readonly IAccommodationBookingReadRepository _readRepository;
    private readonly IClock _clock;

    public BookingCancelledEventHandler(
        IAccommodationBookingRepository repository,
        IAccommodationBookingReadRepository readRepository,
        IClock clock)
    {
        _repository = repository;
        _readRepository = readRepository;
        _clock = clock;
    }

    public async Task HandleAsync(BookingCancelledIntegrationEvent evt, CancellationToken ct = default)
    {
        var bookings = await _readRepository.GetByBookingIdAsync(evt.BookingId, ct);

        foreach (var booking in bookings.Where(b =>
            b.Status is AccommodationBookingStatus.Requested or AccommodationBookingStatus.Confirmed))
        {
            booking.Cancel("Associated booking was cancelled.", TimeSpan.Zero, evt.CorrelationId, _clock);
            await _repository.SaveAsync(booking, ct);
        }
    }
}