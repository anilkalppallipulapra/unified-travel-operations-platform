namespace UTOP.Accommodation.Domain.ValueObjects;

/// <summary>
/// Format: ACM-{YYYYMMDD}-{4-char hex suffix}. Example: ACM-20260715-F2A9.
/// Generate() takes DateOnly from IClock.UtcNow — never calls DateTime.UtcNow directly (ARCH-009 §3).
/// </summary>
public sealed record AccommodationBookingId(string Value)
{
    public static AccommodationBookingId Generate(DateOnly date)
    {
        var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return new AccommodationBookingId($"ACM-{date:yyyyMMdd}-{suffix}");
    }

    public override string ToString() => Value;
}