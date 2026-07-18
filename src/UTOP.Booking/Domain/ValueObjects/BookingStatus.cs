namespace UTOP.Booking.Domain.ValueObjects;

// Note: ARCH-003 uses Draft as initial state. ARCH-005 (state machine) is authoritative.
// PendingValidation is the correct initial state — Draft is pre-command, not a persisted status.
public enum BookingStatus
{
    PendingValidation,  // Created; awaiting availability confirmation
    Confirmed,          // Availability confirmed; resource allocation pending
    Allocated,          // Resource assigned by ResourceAllocation context
    InTransit,          // Journey started
    Completed,          // Journey complete — terminal
    Cancelled,          // Cancelled — leads to Refunded
    Refunded,           // Refund processed — terminal
    Escalated           // Availability failed; awaiting manager decision
}
