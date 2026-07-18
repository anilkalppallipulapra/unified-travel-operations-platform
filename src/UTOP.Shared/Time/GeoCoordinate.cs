namespace UTOP.Shared.Time;

/// <summary>
/// Lat/lon pair. No logic (ARCH-010 §5.7 supporting types).
/// Used by DailyPrayerSchedule as the calculation point.
/// </summary>
public sealed record GeoCoordinate(double Latitude, double Longitude);
