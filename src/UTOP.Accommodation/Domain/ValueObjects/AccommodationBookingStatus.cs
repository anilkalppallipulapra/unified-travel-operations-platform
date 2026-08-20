namespace UTOP.Accommodation.Domain.ValueObjects;

/// <summary>
/// Requested → Confirmed → CheckedIn → CheckedOut (terminal)
///                 │             │
///                 │             └─ RecordNoShow() → NoShow (terminal)
///                 │
///          Cancel() from either Requested or Confirmed → Cancelled (terminal)
///
/// Forbidden (throw InvalidAccommodationStateTransitionException):
/// - Any mutation from CheckedOut, Cancelled, or NoShow
/// - CheckIn() from Requested (must be Confirmed first)
/// - CheckOut() from anything other than CheckedIn
/// - RecordNoShow() from anything other than Confirmed
/// </summary>
public enum AccommodationBookingStatus
{
    Requested,
    Confirmed,
    CheckedIn,
    CheckedOut,
    Cancelled,
    NoShow
}