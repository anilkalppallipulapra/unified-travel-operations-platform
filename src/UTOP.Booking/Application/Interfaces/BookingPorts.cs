using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;
using UTOP.Shared.Time;

namespace UTOP.Booking.Application.Ports;

/// <summary>
/// Checks seat or service availability for a given route and departure.
/// Initial implementation: StubAvailabilityProvider — always returns true.
/// Production: connects to internal Inventory context or external GDS.
/// Never crosses schema boundary directly — port is the boundary.
/// </summary>
public interface IAvailabilityProvider
{
    Task<bool> CheckAvailabilityAsync(
        JourneyRoute route,
        DateTimeOffset departureUtc,
        PassengerCount passengers,
        CancellationToken ct = default);
}

/// <summary>
/// Validates that a group with the given Id exists and is active.
/// Prevents Booking from querying utop_group schema (ARCH-008 FORBIDDEN).
/// Initial implementation: StubGroupExistenceValidator — always passes.
/// </summary>
public interface IGroupExistenceValidator
{
    Task ValidateGroupExistsAsync(string groupId, CancellationToken ct = default);
}

/// <summary>
/// Fetches prayer schedule for a given location and date.
/// Used during pilgrimage booking leg validation (BK-TINV-005).
/// Returns DailyPrayerSchedule from Shared Kernel (ARCH-009 §8.4).
/// Initial implementation: StubPrayerTimeProvider — returns pre-defined Mecca schedule.
/// Production: Aladhan API or pre-computed offline dataset.
/// </summary>
public interface IPrayerTimeProvider
{
    Task<DailyPrayerSchedule> GetScheduleAsync(
        GeoCoordinate location,
        DateOnly date,
        string calculationMethod = "UmmAlQura",
        CancellationToken ct = default);
}
