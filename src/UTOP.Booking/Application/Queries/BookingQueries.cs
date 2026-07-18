using UTOP.Booking.Domain.ValueObjects;

namespace UTOP.Booking.Application.Queries;

public sealed record GetBookingByIdQuery(BookingId BookingId);

public sealed record GetBookingsByOperatorQuery(
    string OperatorId,
    int Page = 1,
    int PageSize = 20);

public sealed record BookingReadModel(
    string BookingId,
    string Status,
    string Mode,
    string Category,
    string OriginCity,
    string OriginCountry,
    string? OriginAirportCode,
    string DestinationCity,
    string DestinationCountry,
    string? DestinationAirportCode,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    decimal TotalAmount,
    string Currency,
    int Adults,
    int Children,
    int Infants,
    string OperatorId,
    string? GroupId,
    string? PilgrimageId,
    int AmendmentVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Relocated here from the LLD's literal Domain/Repositories placement (§9.1) —
/// its return type BookingReadModel lives in this Application/Queries namespace,
/// so keeping the interface in Domain would make Domain depend on Application,
/// backwards for Clean Architecture. Co-located here instead.
/// </summary>
public interface IBookingReadRepository
{
    Task<BookingReadModel?> GetByIdAsync(BookingId id, CancellationToken ct = default);
    Task<IReadOnlyList<BookingReadModel>> GetByOperatorAsync(
        string operatorId, int page, int pageSize, CancellationToken ct = default);
}

public sealed class GetBookingByIdQueryHandler
{
    private readonly IBookingReadRepository _read;

    public GetBookingByIdQueryHandler(IBookingReadRepository read) => _read = read;

    public async Task<BookingReadModel?> HandleAsync(
        GetBookingByIdQuery query,
        CancellationToken ct = default)
        => await _read.GetByIdAsync(query.BookingId, ct);
}

public sealed class GetBookingsByOperatorQueryHandler
{
    private readonly IBookingReadRepository _read;

    public GetBookingsByOperatorQueryHandler(IBookingReadRepository read) => _read = read;

    public async Task<IReadOnlyList<BookingReadModel>> HandleAsync(
        GetBookingsByOperatorQuery query,
        CancellationToken ct = default)
        => await _read.GetByOperatorAsync(query.OperatorId, query.Page, query.PageSize, ct);
}
