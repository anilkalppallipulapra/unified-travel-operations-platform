using UTOP.Booking.Application.Ports;

namespace UTOP.Booking.Infrastructure.ExternalServices.Stubs;

/// <summary>
/// Always passes, no exception (LLD §12.3). Replace with a real adapter that
/// checks GroupManagement's utop_group schema via integration event / API call
/// (never a direct cross-schema query — ARCH-008 FORBIDDEN) when built.
/// </summary>
public sealed class StubGroupExistenceValidator : IGroupExistenceValidator
{
    public Task ValidateGroupExistsAsync(string groupId, CancellationToken ct = default)
        => Task.CompletedTask;
}
