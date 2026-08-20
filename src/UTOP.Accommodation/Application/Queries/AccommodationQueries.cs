namespace UTOP.Accommodation.Application.Queries;
using UTOP.Accommodation.Domain.Aggregates;
using UTOP.Accommodation.Domain.ValueObjects;

public interface IAccommodationBookingReadRepository
{
    Task<AccommodationBooking?> GetByAccommodationBookingIdAsync(AccommodationBookingId id, CancellationToken ct = default);
    Task<IReadOnlyList<AccommodationBooking>> GetByBookingIdAsync(string bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<AccommodationBooking>> GetByBookingIdsAsync(IEnumerable<string> bookingIds, CancellationToken ct = default);
}