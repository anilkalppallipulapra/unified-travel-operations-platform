# Pending Documentation Corrections — from Implementation

Discovered during `UTOP.Booking` implementation (feature/implementation), against the baselined
LLD/ARCH docs on feature/lld. Delete this file once all rows are actioned and committed.

| # | Change | Document | Status |
|---|---|---|---|
| 1 | `IBookingReadRepository` relocated Domain → Application | `lld_booking.md` | ✅ Done — v1.4.0 |
| 2 | `RemovePassenger()` added to `Booking` aggregate | `lld_booking.md` | ✅ Done — v1.4.0 |
| 3 | `Completed` guard added to `AddPassenger()`/`RemovePassenger()` | `lld_booking.md` | ✅ Done — v1.4.0 |
| 4 | `Money`/`PassengerCount` constructors made `private`, `Create()` factory only | `10-shared-kernel-governance.md` (ARCH-010) | ✅ Done — v1.0.1 |
| 5 | Namespace `UTOP.SharedKernel` → `UTOP.Shared` (global) | `lld_booking.md`, `10-shared-kernel-governance.md` | ✅ Done — both files |
| 6 | `GeoCoordinate`, `DailyPrayerSchedule`, `PrayerWindow`, `Prayer` moved to `Time/` folder | `11_solution_structure.md` | ✅ Done |

**All six rows actioned. Per this file's own instruction, delete it from `docs/design` once the four corrected documents below are committed to `feature/lld`.**

## Also worth deciding while in the docs thread
- ARCH-009 §3/§8 confirmed already correct — no doc change needed, noted so it isn't re-litigated.

## Deferred code cleanup (end-of-project, code only, no doc impact)
- Split `BookingPorts.cs`, `BookingEvents.cs`, `BookingExceptions.cs` into one-type-per-file. Explicitly deferred by Anil.

---
*Once #6 is closed, delete this file and confirm both corrected documents are committed to `feature/lld`.*
