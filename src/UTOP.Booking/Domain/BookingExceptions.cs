using UTOP.Booking.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Booking.Domain.Exceptions;

public sealed class BookingNotFoundException(BookingId bookingId)
    : Exception($"Booking '{bookingId}' was not found.")
{
    public BookingId BookingId { get; } = bookingId;
}

public sealed class BookingPriceMustBePositiveException()
    : Exception("Booking total price must be greater than zero. (BK-INV-010)");

public sealed class BookingRouteOriginEqualsDestinationException(string locationCode)
    : Exception($"Route origin and destination must differ; both were '{locationCode}'. (BK-INV-011)")
{
    public string LocationCode { get; } = locationCode;
}

public sealed class BookingOperatorIdRequiredException()
    : Exception("OperatorId must not be null or empty. (BK-INV-012)");

public sealed class BookingDepartureAlreadyPassedException(DateTimeOffset departureUtc, DateTimeOffset nowUtc)
    : Exception($"Departure '{departureUtc:O}' has already passed relative to '{nowUtc:O}'. (BK-TINV-001/BK-TINV-004)")
{
    public DateTimeOffset DepartureUtc { get; } = departureUtc;
    public DateTimeOffset NowUtc { get; } = nowUtc;
}

public sealed class InvalidBookingStateTransitionException(
    BookingId bookingId, BookingStatus fromStatus, BookingStatus toStatus)
    : Exception($"Booking '{bookingId}' cannot transition from '{fromStatus}' to '{toStatus}'.")
{
    public BookingId BookingId { get; } = bookingId;
    public BookingStatus FromStatus { get; } = fromStatus;
    public BookingStatus ToStatus { get; } = toStatus;
}

public sealed class BookingAlreadyCompletedException(BookingId bookingId)
    : Exception($"Booking '{bookingId}' is Completed and cannot be mutated. (BK-INV-007)")
{
    public BookingId BookingId { get; } = bookingId;
}

public sealed class BookingRequiresAdultPassengerException(BookingId bookingId)
    : Exception($"Booking '{bookingId}' must have at least one adult passenger to confirm. (BK-INV-004)")
{
    public BookingId BookingId { get; } = bookingId;
}

public sealed class PassengerCountMismatchException(BookingId bookingId, int expected, int actual)
    : Exception($"Booking '{bookingId}' expected {expected} passenger(s) but manifest has {actual}. (BK-INV-005)")
{
    public BookingId BookingId { get; } = bookingId;
    public int Expected { get; } = expected;
    public int Actual { get; } = actual;
}

public sealed class PilgrimageBookingRequiresPilgrimageAssociationException(BookingId bookingId)
    : Exception($"Religious-category booking '{bookingId}' requires a PilgrimageId association. (BK-INV-008)")
{
    public BookingId BookingId { get; } = bookingId;
}

public sealed class GroupBookingRequiresGroupAssociationException(BookingId bookingId)
    : Exception($"Group-category booking '{bookingId}' requires a GroupId association. (BK-INV-009)")
{
    public BookingId BookingId { get; } = bookingId;
}

public sealed class CurrencyImmutableAfterConfirmationException(
    BookingId bookingId, Currency currentCurrency, Currency attemptedCurrency)
    : Exception($"Booking '{bookingId}' currency '{currentCurrency}' cannot change to '{attemptedCurrency}' after confirmation. (BK-INV-003)")
{
    public BookingId BookingId { get; } = bookingId;
    public Currency CurrentCurrency { get; } = currentCurrency;
    public Currency AttemptedCurrency { get; } = attemptedCurrency;
}

public sealed class BookingAmendmentWindowExpiredException(
    BookingId bookingId, DateTimeOffset departureUtc, DateTimeOffset nowUtc)
    : Exception($"Booking '{bookingId}' cannot be amended within 2 hours of departure ('{departureUtc:O}', now '{nowUtc:O}'). (BK-TINV-002)")
{
    public BookingId BookingId { get; } = bookingId;
    public DateTimeOffset DepartureUtc { get; } = departureUtc;
    public DateTimeOffset NowUtc { get; } = nowUtc;
}

public sealed class InvalidItineraryScheduleException(DateTimeOffset departureUtc, DateTimeOffset arrivalUtc)
    : Exception($"Arrival '{arrivalUtc:O}' must be after departure '{departureUtc:O}'.")
{
    public DateTimeOffset DepartureUtc { get; } = departureUtc;
    public DateTimeOffset ArrivalUtc { get; } = arrivalUtc;
}
