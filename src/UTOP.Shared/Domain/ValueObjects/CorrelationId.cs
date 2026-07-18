namespace UTOP.Shared.Domain.ValueObjects;

public readonly record struct CorrelationId(Guid Value)
{
    public static CorrelationId New() => new(Guid.NewGuid());
    public static CorrelationId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
