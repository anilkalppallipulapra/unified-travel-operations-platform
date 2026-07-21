using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using UTOP.Booking.Domain.Repositories;
using UTOP.Booking.Domain.ValueObjects;
using UTOP.Booking.Infrastructure.Messaging;
using UTOP.Shared.Time;
using BookingAggregate = UTOP.Booking.Domain.Aggregates.Booking;

namespace UTOP.Booking.Infrastructure.Persistence;

public sealed class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;
    private readonly IClock _clock;

    public BookingRepository(BookingDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<BookingAggregate?> GetByIdAsync(BookingId id, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.Itinerary)
            .Include(b => b.PassengerList)
            .FirstOrDefaultAsync(b => b.BookingId == id, ct);
    }

    public async Task SaveAsync(BookingAggregate booking, CancellationToken ct = default)
    {
        var alreadyExists = await _context.Bookings.AnyAsync(b => b.Id == booking.Id, ct);

        if (!alreadyExists)
        {
            _context.Bookings.Add(booking);

            var keyHash = ComputeIdempotencyKeyHash(
                booking.OperatorId, booking.Mode, booking.Route, booking.Itinerary.DepartureUtc);

            _context.IdempotencyKeys.Add(new IdempotencyKeyEntity
            {
                KeyHash = keyHash,
                BookingId = booking.BookingId.Value,
                CreatedAt = _clock.UtcNow
            });
        }
        // If it already existed, it was loaded via GetByIdAsync and EF's change
        // tracker already knows about it — no explicit Update() call needed.

        // Outbox (ARCH-006): each domain event raised this operation becomes an
        // outbox row in the same transaction as the aggregate write. Publishing
        // unpublished rows is the deferred outbox processor's job (UTOP-LLD-BK-04).
        foreach (var domainEvent in booking.DomainEvents)
        {
            var outboxRow = BookingEventTranslator.ToOutboxEvent(domainEvent, booking);
            _context.OutboxEvents.Add(outboxRow);
        }
        booking.ClearDomainEvents();

        await _context.SaveChangesAsync(ct);
    }

    public async Task<BookingAggregate?> FindByIdempotencyKeyAsync(
        string operatorId,
        TravelMode mode,
        JourneyRoute route,
        DateTimeOffset departureUtc,
        CancellationToken ct = default)
    {
        var keyHash = ComputeIdempotencyKeyHash(operatorId, mode, route, departureUtc);

        var record = await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);

        if (record is null)
            return null;

        return await GetByIdAsync(new BookingId(record.BookingId), ct);
    }

    /// <summary>
    /// SHA-256 of (operatorId + mode + route.Origin.Code + route.Destination.Code + departureUtc ISO8601),
    /// matching IBookingRepository's documented key derivation exactly (LLD §9.1).
    /// </summary>
    private static string ComputeIdempotencyKeyHash(
        string operatorId, TravelMode mode, JourneyRoute route, DateTimeOffset departureUtc)
    {
        var composite = $"{operatorId}{mode}{route.Origin.Code}{route.Destination.Code}{departureUtc:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(composite));
        return Convert.ToHexStringLower(bytes);
    }
}
