namespace UTOP.Accommodation.Domain.Repositories;
using UTOP.Accommodation.Domain.Aggregates;
using UTOP.Accommodation.Domain.ValueObjects;

public interface IAccommodationBookingRepository
{
    Task<AccommodationBooking?> GetByIdAsync(AccommodationBookingId id, CancellationToken ct = default);
    Task SaveAsync(AccommodationBooking booking, CancellationToken ct = default);
}