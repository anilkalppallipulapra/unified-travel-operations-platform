using UTOP.Accommodation.Domain.ValueObjects;
using UTOP.Shared.Domain.ValueObjects;

namespace UTOP.Accommodation.Domain.Exceptions;

public sealed class AccommodationBookingNotFoundException(AccommodationBookingId id)
    : Exception($"AccommodationBooking '{id}' was not found.")
{
    public AccommodationBookingId AccommodationBookingId { get; } = id;
}

public sealed class AccommodationPriceMustBePositiveException()
    : Exception("Accommodation total price must be greater than zero. (AC-INV-001)");

public sealed class InvalidStayDurationException(DateOnly start, DateOnly end)
    : Exception($"Stay from '{start}' to '{end}' must be at least one night. (AC-INV-002)")
{
    public DateOnly Start { get; } = start;
    public DateOnly End { get; } = end;
}

public sealed class AccommodationBookingIdRequiredException()
    : Exception("BookingId is required to create an accommodation reservation. (AC-INV-003)");

public sealed class PropertyExternalReferenceRequiredException()
    : Exception("Property external reference is required. (AC-INV-015)");

public sealed class InvalidPropertyIdentityException()
    : Exception("Property must carry a valid identity (Location.Code) before persisting a reservation. (AC-INV-016)");

public sealed class AccommodationCheckInAlreadyPassedException(DateOnly checkIn, DateTimeOffset nowUtc)
    : Exception($"Check-in date '{checkIn}' has already passed relative to '{nowUtc:O}'. (AC-TINV-001/AC-TINV-004)")
{
    public DateOnly CheckIn { get; } = checkIn;
    public DateTimeOffset NowUtc { get; } = nowUtc;
}

public sealed class AccommodationRequiresRoomException(AccommodationBookingId id)
    : Exception($"AccommodationBooking '{id}' requires at least one room before confirmation. (AC-INV-004)")
{
    public AccommodationBookingId AccommodationBookingId { get; } = id;
}

public sealed class AccommodationRequiresOccupantException(AccommodationBookingId id)
    : Exception($"AccommodationBooking '{id}' requires at least one occupant before confirmation. (AC-INV-005)")
{
    public AccommodationBookingId AccommodationBookingId { get; } = id;
}

public sealed class InvalidAccommodationStateTransitionException(
    AccommodationBookingId id, AccommodationBookingStatus fromStatus, AccommodationBookingStatus toStatus)
    : Exception($"AccommodationBooking '{id}' cannot transition from '{fromStatus}' to '{toStatus}'.")
{
    public AccommodationBookingId AccommodationBookingId { get; } = id;
    public AccommodationBookingStatus FromStatus { get; } = fromStatus;
    public AccommodationBookingStatus ToStatus { get; } = toStatus;
}

public sealed class AccommodationCancellationWindowExpiredException(
    AccommodationBookingId id, DateTimeOffset checkInUtc, DateTimeOffset nowUtc)
    : Exception($"AccommodationBooking '{id}' is within the cancellation cutoff (check-in '{checkInUtc:O}', now '{nowUtc:O}'). (AC-TINV-002)")
{
    public AccommodationBookingId AccommodationBookingId { get; } = id;
    public DateTimeOffset CheckInUtc { get; } = checkInUtc;
    public DateTimeOffset NowUtc { get; } = nowUtc;
}

public sealed class AccommodationCheckInTooEarlyException(
    AccommodationBookingId id, DateTimeOffset checkInDateUtc, DateTimeOffset nowUtc)
    : Exception($"AccommodationBooking '{id}' cannot check in before '{checkInDateUtc:O}' (now '{nowUtc:O}'). (AC-TINV-003)")
{
    public AccommodationBookingId AccommodationBookingId { get; } = id;
    public DateTimeOffset CheckInDateUtc { get; } = checkInDateUtc;
    public DateTimeOffset NowUtc { get; } = nowUtc;
}

public sealed class AccommodationCurrencyImmutableAfterConfirmationException(
    AccommodationBookingId id, Currency currentCurrency, Currency attemptedCurrency)
    : Exception($"AccommodationBooking '{id}' currency '{currentCurrency}' cannot change to '{attemptedCurrency}' after confirmation. (AC-INV-006)")
{
    public AccommodationBookingId AccommodationBookingId { get; } = id;
    public Currency CurrentCurrency { get; } = currentCurrency;
    public Currency AttemptedCurrency { get; } = attemptedCurrency;
}

public sealed class InvalidRoomRateException(Money rate)
    : Exception($"Room rate '{rate.Amount} {rate.Currency}' must be positive. (AC-INV-009)")
{
    public Money Rate { get; } = rate;
}

public sealed class InvalidAncillaryServiceException(string message)
    : Exception(message);

public sealed class DuplicateRoomException(string providerRoomReference)
    : Exception($"Room with provider reference '{providerRoomReference}' is already assigned to this reservation. (AC-INV-018)")
{
    public string ProviderRoomReference { get; } = providerRoomReference;
}

public sealed class DuplicateOccupantException(string name)
    : Exception($"Occupant '{name}' is already registered for this room. (AC-INV-017)")
{
    public string Name { get; } = name;
}