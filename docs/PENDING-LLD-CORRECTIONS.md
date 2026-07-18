# Pending Documentation Corrections — from Implementation

Discovered during `UTOP.Booking` implementation (feature/implementation), against the baselined
LLD/ARCH docs on feature/lld. Action these next time the docs (sdlc-mentor) thread is open.
Delete this file once all rows are actioned and the doc changes are committed to feature/lld.

| # | Change | Document | Section | Action |
|---|---|---|---|---|
| 1 | `IBookingReadRepository` relocated from Domain to Application layer (fixes a Domain→Application dependency inversion in the original LLD — its return type `BookingReadModel` lives in Application.Queries) | `lld_booking.md` | §9.1 / §7.5 | Move interface definition to sit alongside §7.5; update namespace to `UTOP.Booking.Application.Queries` |
| 2 | `RemovePassenger()` added to the `Booking` aggregate — required by BK-INV-005 ("enforced on AddPassenger() and RemovePassenger()") but missing from the original code sample. Does NOT reduce `PassengerCount` — only removes from the manifest; reducing party size is a pricing concern deferred to CostSplitting (see UTOP-LLD-BK-02) | `lld_booking.md` | §4.1 | Add method + rationale to aggregate documentation |
| 3 | `AddPassenger()` and `RemovePassenger()` now guard against `Completed` status — BK-INV-007 says "all mutation methods throw if Completed" but the original code only enforced this on some methods | `lld_booking.md` | §4.1 | Note guard in both method docs |
| 4 | `Money` and `PassengerCount` constructors made `private` — `Create()` static factory is now the only construction path. The original code's public positional-record pattern allowed `new Money(-500, ...)` etc. to bypass validation entirely | **`10-shared-kernel-governance.md` (ARCH-010)** | §5.1, §5.5 | Update canonical code samples — this is the more important fix since ARCH-010 is what future context LLDs will copy from |
| 5 | Shared Kernel namespace renamed `UTOP.SharedKernel` → `UTOP.Shared` globally (kept the already-built code as-is rather than reverting, since it's simpler and matches C# convention of namespace mirroring project name) | `lld_booking.md` §3, **`10-shared-kernel-governance.md`** (all §5 code samples) | — | Global find-replace in namespace headers across both docs |
| 6 | `GeoCoordinate`, `DailyPrayerSchedule`, `PrayerWindow`, `Prayer` belong under `Time/` folder (namespace `UTOP.Shared.Time` per ARCH-009 §8.4), not `ValueObjects/` as originally listed | `11_solution_structure.md` | Shared Kernel tree | Move those 4 lines from the ValueObjects list to the Time list |

## Also worth deciding while in the docs thread
- Confirm ARCH-009 §3 (IClock) and §8 (prayer schedule) content — already retrieved and used to correct `SystemClock`/`FakeClock`/`DailyPrayerSchedule` in this session; no doc change needed there, they were already correct, the earlier code was the gap. Just noting so it's not re-litigated.

## Deferred code cleanup (end-of-project, not a doc correction — code only, no doc impact)
- Split consolidated files into one-type-per-file: `BookingPorts.cs` (→ `IAvailabilityProvider.cs`, `IGroupExistenceValidator.cs`, `IPrayerTimeProvider.cs`), `BookingEvents.cs` (→ one file per event record), `BookingExceptions.cs` (→ one file per exception class). Purely cosmetic — no behavior change, no SOLID implications, just navigability. Explicitly deferred by Anil to end-of-project cleanup, not urgent.
