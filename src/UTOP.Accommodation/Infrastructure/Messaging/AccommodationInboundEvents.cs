using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Accommodation.Infrastructure.Messaging;

public sealed record BookingCancelledIntegrationEvent(
    string BookingId,
    CorrelationId CorrelationId);

public sealed record PilgrimageConfirmedIntegrationEvent(
    string PilgrimageId,
    IReadOnlyList<string> PilgrimBookingIds,
    CorrelationId CorrelationId);