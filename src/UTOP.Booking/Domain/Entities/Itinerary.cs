using UTOP.Booking.Domain.Exceptions;
using UTOP.Shared;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Booking.Domain.Entities;

/// <summary>
/// Travel schedule for one booking leg.
/// Always replaced atomically on amendment — never mutated in place (BK-INV-014).
/// DepartureUtc and ArrivalUtc are UTC DateTimeOffset per ARCH-009 §2.
/// </summary>
public sealed class Itinerary : Entity
{
    public DateTimeOffset DepartureUtc { get; private set; }
    public DateTimeOffset ArrivalUtc { get; private set; }
    public Location DeparturePoint { get; private set; } = null!;   // carries airport/stop Code
    public string DepartureCity { get; private set; } = null!;       // city name — separate from Location.Code
    public string DepartureCountry { get; private set; } = null!;    // ISO 3166-1 alpha-2
    public Location ArrivalPoint { get; private set; } = null!;      // carries airport/stop Code
    public string ArrivalCity { get; private set; } = null!;
    public string ArrivalCountry { get; private set; } = null!;
    public string? CarrierReference { get; private set; }
    public string? ServiceClass { get; private set; }

    private Itinerary() { }

    public static Itinerary Create(
        DateTimeOffset departureUtc,
        DateTimeOffset arrivalUtc,
        Location departurePoint,
        string departureCity,
        string departureCountry,
        Location arrivalPoint,
        string arrivalCity,
        string arrivalCountry,
        string? carrierReference = null,
        string? serviceClass = null)
    {
        if (arrivalUtc <= departureUtc)
            throw new InvalidItineraryScheduleException(departureUtc, arrivalUtc);
        if (string.IsNullOrWhiteSpace(departureCity)) throw new ArgumentException("Departure city required.");
        if (string.IsNullOrWhiteSpace(departureCountry)) throw new ArgumentException("Departure country required.");
        if (string.IsNullOrWhiteSpace(arrivalCity)) throw new ArgumentException("Arrival city required.");
        if (string.IsNullOrWhiteSpace(arrivalCountry)) throw new ArgumentException("Arrival country required.");

        return new Itinerary
        {
            Id = Guid.NewGuid(),
            DepartureUtc = departureUtc,
            ArrivalUtc = arrivalUtc,
            DeparturePoint = departurePoint,
            DepartureCity = departureCity,
            DepartureCountry = departureCountry,
            ArrivalPoint = arrivalPoint,
            ArrivalCity = arrivalCity,
            ArrivalCountry = arrivalCountry,
            CarrierReference = carrierReference,
            ServiceClass = serviceClass
        };
    }

    public TimeSpan Duration => ArrivalUtc - DepartureUtc;
}
