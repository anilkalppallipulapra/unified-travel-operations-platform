using UTOP.Shared.Domain.Events;

namespace UTOP.Shared;

// CORRECTION: CreatedAt and UpdatedAt changed from DateTime to DateTimeOffset (ARCH-009)
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }   // UTC DateTimeOffset
    public DateTimeOffset UpdatedAt { get; protected set; }   // UTC DateTimeOffset

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
