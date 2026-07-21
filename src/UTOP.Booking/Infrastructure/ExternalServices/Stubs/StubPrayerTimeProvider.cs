using UTOP.Booking.Application.Ports;
using UTOP.Shared.Time;

namespace UTOP.Booking.Infrastructure.ExternalServices.Stubs;

/// <summary>
/// Returns a fixed, illustrative Mecca schedule for any input (LLD §12.3).
/// Times below are NOT astronomically calculated — plausible placeholders only,
/// clearly not authoritative. Replace with a real Aladhan API adapter or
/// pre-computed offline dataset (per IPrayerTimeProvider's doc comment) when built.
/// </summary>
public sealed class StubPrayerTimeProvider : IPrayerTimeProvider
{
    private static readonly GeoCoordinate MeccaCoordinate = new(21.4225, 39.8262);
    private static readonly TimeSpan MeccaUtcOffset = TimeSpan.FromHours(3); // Saudi Arabia, no DST

    public Task<DailyPrayerSchedule> GetScheduleAsync(
        GeoCoordinate location,
        DateOnly date,
        string calculationMethod = "UmmAlQura",
        CancellationToken ct = default)
    {
        DateTimeOffset At(int hour, int minute) =>
            new(date.Year, date.Month, date.Day, hour, minute, 0, MeccaUtcOffset);

        var schedule = new DailyPrayerSchedule
        {
            Date = date,
            Location = MeccaCoordinate,
            CalculationMethod = calculationMethod,
            Fajr = At(4, 30),
            Sunrise = At(5, 50),
            Dhuhr = At(12, 15),
            Asr = At(15, 30),
            Maghrib = At(18, 40),
            Isha = At(20, 0),
            Jumuah = date.DayOfWeek == DayOfWeek.Friday ? At(12, 30) : null
        };

        return Task.FromResult(schedule);
    }
}
