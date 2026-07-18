using UTOP.Booking.Domain.Repositories;
using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;
using UTOP.Shared.Time;

namespace UTOP.Booking.Application.EventHandlers;

/// <summary>
/// Handles ResourceAllocatedIntegrationEvent published by ResourceAllocation context.
/// Advances Booking status from Confirmed → Allocated.
/// Booking context does not own this event — it reacts to it.
/// Cross-schema query forbidden (ARCH-008). Only BookingId is used.
/// </summary>
public sealed class ResourceAllocatedEventHandler
{
    private readonly IBookingRepository _repository;
    private readonly IClock _clock;

    public ResourceAllocatedEventHandler(IBookingRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task HandleAsync(
        ResourceAllocatedIntegrationEvent evt,
        CancellationToken ct = default)
    {
        var bookingId = new BookingId(evt.BookingId);
        var booking = await _repository.GetByIdAsync(bookingId, ct);

        if (booking is null)
        {
            // Log warning — event may have arrived before booking persistence in edge case
            return;
        }

        var correlationId = CorrelationId.From(evt.CorrelationId);
        booking.MarkAllocated(correlationId, _clock);
        await _repository.SaveAsync(booking, ct);
    }
}

/// <summary>
/// Integration event shape received from ResourceAllocation context.
/// Defined here as a consumer DTO — not shared from ResourceAllocation.
/// No shared DTOs across contexts (ARCH-008 governance rules).
/// </summary>
public sealed record ResourceAllocatedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string ResourceId,
    DateTimeOffset OccurredAt);
