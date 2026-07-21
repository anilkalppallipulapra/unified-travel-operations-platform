using UTOP.Booking.Application.Ports;
using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Booking.Infrastructure.ExternalServices.Stubs;

/// <summary>
/// Always returns true (LLD §12.3). Replace with a real Inventory context or
/// external GDS adapter under Infrastructure/ExternalServices/Adapters/ when built.
/// </summary>
public sealed class StubAvailabilityProvider : IAvailabilityProvider
{
    public Task<bool> CheckAvailabilityAsync(
        JourneyRoute route,
        DateTimeOffset departureUtc,
        PassengerCount passengers,
        CancellationToken ct = default)
        => Task.FromResult(true);
}
