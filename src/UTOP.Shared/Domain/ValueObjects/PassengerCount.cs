using System;

namespace UTOP.Shared.Domain.ValueObjects;

/// <summary>
/// Structural validity only — the lap-infant rule derives from IATA/aviation regulation,
/// not UTOP business rules. Does not apply age bands, nationality, or visa requirements
/// (ARCH-010 §5.5 constraints — those are Pricing/Compliance concerns).
/// Constructor is private — Create() is the only construction path, so the
/// adult-minimum and lap-infant invariants cannot be bypassed by calling `new PassengerCount(...)` directly.
/// </summary>
public readonly record struct PassengerCount
{
    public int Adults { get; }
    public int Children { get; }
    public int Infants { get; }

    private PassengerCount(int adults, int children, int infants)
    {
        Adults = adults;
        Children = children;
        Infants = infants;
    }

    public static PassengerCount Create(int adults, int children, int infants)
    {
        if (adults < 1)
            throw new ArgumentOutOfRangeException(nameof(adults), "At least one adult required.");
        if (children < 0)
            throw new ArgumentOutOfRangeException(nameof(children));
        if (infants < 0)
            throw new ArgumentOutOfRangeException(nameof(infants));
        if (infants > adults)
            throw new ArgumentException("Infants may not exceed adults (lap infant rule).");

        return new PassengerCount(adults, children, infants);
    }

    public int Total => Adults + Children + Infants;
}
