using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Booking.Domain.ValueObjects;

/// <summary>
/// Immutable route definition.
/// Origin and Destination are Location records from Shared Kernel.
/// BK-INV-011 (Origin.Code != Destination.Code) enforced at Booking.Create().
/// </summary>
public sealed record JourneyRoute(
    Location Origin,
    Location Destination,
    RouteType Type);
