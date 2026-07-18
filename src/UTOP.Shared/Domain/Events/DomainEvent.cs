using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Shared.Domain.Events;

// CORRECTION: OccurredAt changed from DateTime to DateTimeOffset (ARCH-009)
// CORRECTION: CorrelationId changed from string to CorrelationId struct (ARCH-010)
public abstract record DomainEvent(
    Guid EventId,
    CorrelationId CorrelationId,
    string AggregateId,
    string AggregateType,
    DateTimeOffset OccurredAt);
