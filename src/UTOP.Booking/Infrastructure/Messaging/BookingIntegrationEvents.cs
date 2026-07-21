namespace UTOP.Booking.Infrastructure.Messaging;

// Routing key: booking.created
public sealed record BookingCreatedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Mode,
    string Category,
    string OriginCity,
    string OriginCountry,
    string? OriginAirportCode,
    string DestinationCity,
    string DestinationCountry,
    string? DestinationAirportCode,
    decimal TotalAmount,
    string Currency,
    int Adults,
    int Children,
    int Infants,
    string OperatorId,
    string? GroupId,
    string? PilgrimageId,
    DateTimeOffset DepartureUtc,
    DateTimeOffset OccurredAt);

// Routing key: booking.confirmed
public sealed record BookingConfirmedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Category,
    decimal TotalAmount,
    string Currency,
    int Adults,
    int Children,
    int Infants,
    DateTimeOffset DepartureUtc,
    DateTimeOffset OccurredAt);

// Routing key: booking.amended
public sealed record BookingAmendedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    int AmendmentVersion,
    DateTimeOffset NewDepartureUtc,
    DateTimeOffset NewArrivalUtc,
    decimal NewTotalAmount,
    string Currency,
    DateTimeOffset OccurredAt);

// Routing key: booking.cancelled
public sealed record BookingCancelledIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Reason,
    DateTimeOffset CancelledAt,
    DateTimeOffset OccurredAt);

// Routing key: booking.escalated
public sealed record BookingEscalatedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    string Reason,
    DateTimeOffset OccurredAt);

// Routing key: booking.completed
public sealed record BookingCompletedIntegrationEvent(
    Guid EventId,
    Guid CorrelationId,
    string BookingId,
    DateTimeOffset OccurredAt);
