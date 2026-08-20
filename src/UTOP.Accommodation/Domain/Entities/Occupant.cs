namespace UTOP.Accommodation.Domain.Entities;
using UTOP.Shared;

public sealed class Occupant : Entity
{
    public string Name { get; private set; } = null!;
    public OccupantType Type { get; private set; }
    public int? Age { get; private set; }

    private Occupant() { }

    public static Occupant Create(string name, OccupantType type, int? age = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Occupant name is required.", nameof(name));
        if (age is < 0)
            throw new ArgumentException("Age cannot be negative.", nameof(age));

        return new Occupant { Id = Guid.NewGuid(), Name = name, Type = type, Age = age };
    }
}