using UTOP.Booking.Application.Commands;
using UTOP.Booking.Application.Ports;
using UTOP.Booking.Domain.Entities;
using UTOP.Booking.Domain.Repositories;
using UTOP.Shared.Time;
using BookingAggregate = UTOP.Booking.Domain.Aggregates.Booking;

namespace UTOP.Booking.Application.Handlers;

public sealed class CreateBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IGroupExistenceValidator _groupValidator;
    private readonly IClock _clock;

    public CreateBookingCommandHandler(
        IBookingRepository repository,
        IGroupExistenceValidator groupValidator,
        IClock clock)
    {
        _repository = repository;
        _groupValidator = groupValidator;
        _clock = clock;
    }

    public async Task<Domain.ValueObjects.BookingId> HandleAsync(CreateBookingCommand cmd, CancellationToken ct = default)
    {
        // Idempotency: return existing if same key already exists (ARCH-005 §1.4)
        var existing = await _repository.FindByIdempotencyKeyAsync(
            cmd.OperatorId, cmd.Mode, cmd.Route, cmd.DepartureUtc, ct);
        if (existing is not null)
            return existing.BookingId;

        // Validate group exists before association (BK-CINV-003)
        if (cmd.Category == Domain.ValueObjects.TravelCategory.Group && cmd.GroupId is not null)
            await _groupValidator.ValidateGroupExistsAsync(cmd.GroupId, ct);

        var itinerary = Itinerary.Create(
            cmd.DepartureUtc,
            cmd.ArrivalUtc,
            cmd.DeparturePoint,
            cmd.DepartureCity,
            cmd.DepartureCountry,
            cmd.ArrivalPoint,
            cmd.ArrivalCity,
            cmd.ArrivalCountry,
            cmd.CarrierReference,
            cmd.ServiceClass);

        var booking = BookingAggregate.Create(
            cmd.Mode, cmd.Route, cmd.Passengers,
            cmd.Category, cmd.OperatorId, cmd.Price,
            itinerary, cmd.CorrelationId, _clock);

        if (cmd.GroupId is not null)
            booking.AssociateGroup(cmd.GroupId);

        if (cmd.PilgrimageId is not null)
            booking.AssociatePilgrimage(cmd.PilgrimageId);

        await _repository.SaveAsync(booking, ct);
        return booking.BookingId;
    }
}
