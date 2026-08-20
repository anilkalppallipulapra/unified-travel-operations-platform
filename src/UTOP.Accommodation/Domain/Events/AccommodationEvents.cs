using UTOP.Accommodation.Domain.ValueObjects;
using UTOP.Shared.Domain.Events;
using UTOP.Shared.Domain.ValueObjects;
using UTOP.Accommodation.Domain.Aggregates;

namespace UTOP.Accommodation.Domain.Events;

public sealed record AccommodationBookingCreated(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    AccommodationBookingId AccommodationBookingId,
    string BookingId,
    Location Property,
    DateOnly CheckIn,
    DateOnly CheckOut,
    Money TotalPrice,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationBookingConfirmed(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    AccommodationBookingId AccommodationBookingId,
    string BookingId,
    Money TotalPrice,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationBookingAmended(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    AccommodationBookingId AccommodationBookingId,
    int AmendmentVersion,
    DateOnly PreviousCheckIn,
    DateOnly PreviousCheckOut,
    DateOnly NewCheckIn,
    DateOnly NewCheckOut,
    Money PreviousPrice,
    Money NewPrice,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationBookingCancelled(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    AccommodationBookingId AccommodationBookingId,
    string Reason,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationGuestCheckedIn(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    AccommodationBookingId AccommodationBookingId,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationGuestCheckedOut(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    AccommodationBookingId AccommodationBookingId,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);

public sealed record AccommodationNoShowRecorded(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    AccommodationBookingId AccommodationBookingId,
    DateTimeOffset OccurredAt)
    : DomainEvent(EventId, CorrelationId, AggregateId, AggregateType, OccurredAt);