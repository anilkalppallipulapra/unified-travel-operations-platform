using UTOP.Accommodation.Domain.Exceptions;
using UTOP.Shared;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Accommodation.Domain.Entities;

/// <summary>
/// A room line-item within the reservation. Rate is per-night; total room cost is
/// derived (RatePerNight × nights), never stored redundantly.
/// AC-INV-009: RatePerNight must be positive.
/// AC-INV-017: rejects a duplicate occupant within the same room (Name + OccupantType).
/// AC-INV-018 (enforced by the aggregate's AddRoom): duplicate ProviderRoomReference rejected.
/// </summary>
public sealed class Room : Entity
{
    public RoomType Type { get; private set; }
    public Money RatePerNight { get; private set; } = null!;
    public string ProviderRoomReference { get; private set; } = null!;

    private readonly List<Occupant> _occupants = new();
    public IReadOnlyList<Occupant> Occupants => _occupants.AsReadOnly();
    public int OccupantCount => _occupants.Count;

    private Room() { }

    public static Room Create(RoomType type, Money ratePerNight, string providerRoomReference)
    {
        if (ratePerNight.Amount <= 0)
            throw new InvalidRoomRateException(ratePerNight);
        if (string.IsNullOrWhiteSpace(providerRoomReference))
            throw new ArgumentException("Provider room reference is required.", nameof(providerRoomReference));

        return new Room { Id = Guid.NewGuid(), Type = type, RatePerNight = ratePerNight, ProviderRoomReference = providerRoomReference };
    }

    public void AddOccupant(Occupant occupant)
    {
        if (_occupants.Any(o => o.Id == occupant.Id))
            return;
        if (_occupants.Any(o => o.Name.Equals(occupant.Name, StringComparison.OrdinalIgnoreCase) && o.Type == occupant.Type))
            throw new DuplicateOccupantException(occupant.Name);

        _occupants.Add(occupant);
    }
}