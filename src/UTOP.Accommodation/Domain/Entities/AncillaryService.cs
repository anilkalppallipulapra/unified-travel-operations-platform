using UTOP.Accommodation.Domain.Exceptions;
using UTOP.Shared;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Accommodation.Domain.Entities;

public sealed class AncillaryService : Entity
{
    public AncillaryServiceType Type { get; private set; }
    public string Description { get; private set; } = null!;
    public Money Price { get; private set; } = null!;

    private AncillaryService() { }

    public static AncillaryService Create(AncillaryServiceType type, string description, Money price)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidAncillaryServiceException("Ancillary service description is required.");
        if (price.Amount < 0)
            throw new InvalidAncillaryServiceException("Ancillary service price cannot be negative.");

        return new AncillaryService { Id = Guid.NewGuid(), Type = type, Description = description, Price = price };
    }
}