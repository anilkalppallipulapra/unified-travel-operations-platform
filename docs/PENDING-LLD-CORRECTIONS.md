# Pending Documentation Corrections — from Implementation

Discovered during `UTOP.Booking` implementation (feature/implementation), against the baselined
LLD/ARCH docs on feature/lld. All six rows below actioned this session.

| # | Change | Document | Status |
|---|---|---|---|
| 1 | `Infrastructure/Messaging/` added to Full DDD context template | `11_solution_structure.md` §2.1 | ✅ Done |
| 2 | `ItineraryConfiguration` DeparturePoint/ArrivalPoint fixed (Code-only via HasConversion, Location has no parameterless ctor) | `lld_booking.md` §9.3 | ✅ Done |
| 3 | `Route` (JourneyRoute) column + JSONB mapping added | `lld_booking.md` §9.2, §9.3 | ✅ Done |
| 4 | `Passengers` (PassengerCount) columns + ComplexProperty mapping added | `lld_booking.md` §9.2, §9.3 | ✅ Done |
| 5 | All six indexes + unique constraint added to EF config | `lld_booking.md` §9.3 | ✅ Done — `ix_outbox_unpublished` intentionally not added; no `OutboxEventConfiguration` exists in this document, that index belongs to the deferred outbox-processor LLD (`UTOP-LLD-BK-04`) |
| 6 | `HasMaxLength()` on Mode/Status/Category/Currency/PassengerType; `PassengerList` given `IsRequired()` + `OnDelete(Cascade)` | `lld_booking.md` §9.3 | ✅ Done |

**All six rows actioned. Per this file's own instruction, delete it from `docs/` once the two corrected documents below are committed to `feature/lld`.**
