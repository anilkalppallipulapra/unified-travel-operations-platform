namespace UTOP.Shared.Time;

/// <summary>
/// Immutable record of the five daily prayer windows for a given day and location (ARCH-009 §8.4).
/// All times stored in UTC. Calculation method preserved for auditability.
/// Precision: calculated to second-level in UTC (ARCH-009 §8.2); display at minute-level only.
/// </summary>
public sealed record DailyPrayerSchedule
{
    public DateOnly Date { get; init; }                   // Calendar date (timezone-neutral)
    public GeoCoordinate Location { get; init; } = null!;  // Lat/Lon of calculation point
    public string CalculationMethod { get; init; } = null!; // "UmmAlQura" | "ISNA" | "MWL" | "Karachi"

    public DateTimeOffset Fajr { get; init; }
    public DateTimeOffset Sunrise { get; init; }           // Marks end of Fajr window
    public DateTimeOffset Dhuhr { get; init; }
    public DateTimeOffset Asr { get; init; }
    public DateTimeOffset Maghrib { get; init; }
    public DateTimeOffset Isha { get; init; }

    public DateTimeOffset? Jumuah { get; init; }           // Friday prayer; replaces Dhuhr on Fridays

    public IReadOnlyList<PrayerWindow> AsPrayerWindows()
    {
        var windows = new List<PrayerWindow>
        {
            new(Prayer.Fajr, Fajr, Sunrise),
            new(Prayer.Dhuhr, Dhuhr, Asr),
            new(Prayer.Asr, Asr, Maghrib),
            new(Prayer.Maghrib, Maghrib, Isha),
            // Isha window intentionally spans midnight to the next day's Fajr
            new(Prayer.Isha, Isha, Fajr.AddDays(1))
        };
        if (Jumuah.HasValue)
            windows.Add(new(Prayer.Jumuah, Jumuah.Value, Asr));
        return windows.AsReadOnly();
    }
}

public sealed record PrayerWindow(
    Prayer Prayer,
    DateTimeOffset Start,
    DateTimeOffset End)
{
    public bool IsActive(DateTimeOffset utcNow) =>
        utcNow >= Start && utcNow < End;

    public TimeSpan TimeUntil(DateTimeOffset utcNow) =>
        utcNow < Start ? Start - utcNow : TimeSpan.Zero;
}

public enum Prayer { Fajr, Dhuhr, Asr, Maghrib, Isha, Jumuah }
