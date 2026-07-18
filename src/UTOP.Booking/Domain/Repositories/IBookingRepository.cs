using System;
using System.Threading;
using System.Threading.Tasks;
using UTOP.Booking.Domain.ValueObjects;

// Alias mapping resolves the namespace naming collision without changing baseline folders
using BookingAggregate = UTOP.Booking.Domain.Aggregates.Booking;

namespace UTOP.Booking.Domain.Repositories;

public interface IBookingRepository
{
    Task<BookingAggregate?> GetByIdAsync(BookingId id, CancellationToken ct = default);
    Task SaveAsync(BookingAggregate booking, CancellationToken ct = default);

    /// <summary>
    /// Idempotency check for CreateBooking (ARCH-005 §1.4).
    /// Key: SHA-256 of (operatorId + mode + route.Origin.Code + route.Destination.Code + departureUtc ISO8601).
    /// </summary>
    Task<BookingAggregate?> FindByIdempotencyKeyAsync(
        string operatorId,
        TravelMode mode,
        JourneyRoute route,
        DateTimeOffset departureUtc,
        CancellationToken ct = default);
}