using UTOP.Accommodation.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;
using UTOP.Shared.Time;

namespace UTOP.Accommodation.Application.Interfaces;

/// <summary>
/// Checks room availability and rate with the external property/PMS system for a given
/// property, stay window, and room type.
/// Initial implementation: StubAccommodationProvider — always returns available.
/// Production: connects to a channel manager or property management system API.
/// Never crosses schema boundary directly — port is the boundary.
/// </summary>
public interface IAccommodationProvider
{
    Task<bool> CheckAvailabilityAsync(
        Location property,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken ct = default);
}

/// <summary>
/// Returns proximity/distance data between a property and a sacred site, used during
/// pilgrimage-linked accommodation validation (ACM-02, deferred to Pilgrimage LLD).
/// Initial implementation: StubSacredSiteProximityProvider — returns a fixed distance.
/// Production: geospatial lookup against a sacred-sites dataset.
/// </summary>
public interface ISacredSiteProximityProvider
{
    Task<double> GetDistanceInMetersAsync(
        GeoCoordinate propertyLocation,
        string sacredSiteId,
        CancellationToken ct = default);
}