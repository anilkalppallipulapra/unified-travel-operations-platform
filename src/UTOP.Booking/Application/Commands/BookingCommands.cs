using UTOP.Booking.Domain.Entities;
using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Booking.Application.Commands;

/// <summary>
/// Passenger data carried in ConfirmBookingCommand.
/// This is an application-layer DTO — not a domain entity.
/// It is translated to a Passenger entity inside the command handler.
/// </summary>
public sealed record PassengerDetail(
    string FirstName,
    string LastName,
    PassengerType Type,
    DateOnly DateOfBirth,
    string? DocumentNumber,
    string? Nationality);

public sealed record CreateBookingCommand(
    TravelMode Mode,
    JourneyRoute Route,
    PassengerCount Passengers,
    TravelCategory Category,
    string OperatorId,
    Money Price,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    Location DeparturePoint,
    string DepartureCity,
    string DepartureCountry,
    Location ArrivalPoint,
    string ArrivalCity,
    string ArrivalCountry,
    string? CarrierReference,
    string? ServiceClass,
    string? GroupId,
    string? PilgrimageId,
    CorrelationId CorrelationId);

public sealed record ConfirmBookingCommand(
    BookingId BookingId,
    IReadOnlyList<PassengerDetail> Passengers,
    CorrelationId CorrelationId);

public sealed record AmendBookingCommand(
    BookingId BookingId,
    DateTimeOffset NewDepartureUtc,
    DateTimeOffset NewArrivalUtc,
    Location NewDeparturePoint,
    string NewDepartureCity,
    string NewDepartureCountry,
    Location NewArrivalPoint,
    string NewArrivalCity,
    string NewArrivalCountry,
    Money NewPrice,
    string? NewCarrierReference,
    string? NewServiceClass,
    CorrelationId CorrelationId);

public sealed record CancelBookingCommand(
    BookingId BookingId,
    string Reason,
    CorrelationId CorrelationId);

public sealed record StartJourneyCommand(
    BookingId BookingId,
    CorrelationId CorrelationId);

public sealed record CompleteBookingCommand(
    BookingId BookingId,
    CorrelationId CorrelationId);
