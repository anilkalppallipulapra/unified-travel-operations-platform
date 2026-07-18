using UTOP.Booking.Domain.Entities;
using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;
using UTOP.Shared.Domain.Events;
namespace UTOP.Booking.Domain.Events;

// All events inherit from DomainEvent. OccurredAt is DateTimeOffset UTC.
// CorrelationId is the Shared Kernel struct.

public sealed record BookingCreated(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    TravelMode Mode,
    JourneyRoute Route,
    Money TotalPrice,
    TravelCategory Category,
    string OperatorId,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingConfirmed(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    TravelCategory Category,
    Money TotalPrice,
    PassengerCount Passengers,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingAmended(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    int AmendmentVersion,
    Itinerary PreviousItinerary,
    Itinerary NewItinerary,
    Money PreviousPrice,
    Money NewPrice,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingCancelled(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    string Reason,
    DateTimeOffset CancelledAt,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingEscalated(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record BookingCompleted(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    BookingId BookingId,
    DateTimeOffset OccurredAt) : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);
