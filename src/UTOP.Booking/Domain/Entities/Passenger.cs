using UTOP.Shared;

namespace UTOP.Booking.Domain.Entities;

/// <summary>
/// Individual traveller on a booking.
/// PII fields (FirstName, LastName, DocumentNumber) are encrypted at rest.
/// Encryption is handled at the infrastructure layer via EF Core value converter.
/// See open item UTOP-LLD-BK-01.
/// </summary>
public sealed class Passenger : Entity
{
    public string FirstName { get; private set; } = null!;        // PII — encrypted at rest
    public string LastName { get; private set; } = null!;         // PII — encrypted at rest
    public PassengerType Type { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string? DocumentNumber { get; private set; }           // PII — encrypted at rest
    public string? Nationality { get; private set; }              // ISO 3166-1 alpha-2

    private Passenger() { }

    public static Passenger Create(
        string firstName,
        string lastName,
        PassengerType type,
        DateOnly dateOfBirth,
        string? documentNumber = null,
        string? nationality = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        return new Passenger
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Type = type,
            DateOfBirth = dateOfBirth,
            DocumentNumber = documentNumber,
            Nationality = nationality
        };
    }

    public string FullName => $"{FirstName} {LastName}";
}
