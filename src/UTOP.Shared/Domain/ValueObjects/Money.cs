using System;

namespace UTOP.Shared.Domain.ValueObjects;

/// <summary>
/// Arithmetic only — does not convert currencies (ARCH-010 §5.1 constraints).
/// Currency conversion belongs to a dedicated FX service (not yet defined).
/// Constructor is private — Create() is the only construction path, so the
/// non-negativity invariant cannot be bypassed by calling `new Money(...)` directly.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount must not be negative.");

        return new Money(amount, currency);
    }

    public Money Add(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException(
                $"Cannot add Money of differing currencies: {Currency} and {other.Currency}.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException(
                $"Cannot subtract Money of differing currencies: {Currency} and {other.Currency}.");

        var result = Amount - other.Amount;
        if (result < 0)
            throw new InvalidOperationException("Money subtraction must not produce a negative result.");

        return new Money(result, Currency);
    }

    public static Money Zero(Currency currency) => new(0m, currency);
}
