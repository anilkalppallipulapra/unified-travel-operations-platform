namespace UTOP.Booking.Domain.ValueObjects;

/// <summary>
/// Human-readable booking identifier.
/// Format: {MODE_PREFIX}-{YYYYMMDD}-{4-char hex suffix}
/// Examples: FLT-20250601-A3F9, BUS-20250601-C7D2
/// Immutable after creation (BK-INV-001).
/// IMPORTANT: Generate() accepts DateOnly from IClock.UtcNow — never calls DateTime.UtcNow internally.
/// </summary>
public sealed record BookingId(string Value)
{
    public static BookingId Generate(TravelMode mode, DateOnly date)
    {
        var prefix = mode switch
        {
            TravelMode.Flight => "FLT",
            TravelMode.Bus    => "BUS",
            TravelMode.Train  => "TRN",
            TravelMode.Ferry  => "FRY",
            TravelMode.Coach  => "CCH",
            _                 => "BKG"
        };
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return new BookingId($"{prefix}-{date:yyyyMMdd}-{suffix}");
    }

    public override string ToString() => Value;
}
