namespace UTOP.Shared.Domain.ValueObjects;

/// <summary>
/// Identity and classification primitive only (ARCH-010 §5.3 constraints).
/// Does not carry geographic coordinates, operating hours, or capacity —
/// those are owned by whichever context needs them and looked up, not stored here.
/// </summary>
public sealed record Location(
    string Code,           // IATA airport code, GTFS stop ID, or platform-defined location code
    LocationType Type,
    string? DisplayName    // Optional; presentation only
);
