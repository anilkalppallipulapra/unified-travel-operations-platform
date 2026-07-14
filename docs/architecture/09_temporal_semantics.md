# UTOP Temporal Semantics
**Document ID**: UTOP-ARCH-009  
**Version**: 1.3.0  
**Status**: Locked  
**Branch**: feature/phase3-stabilization  
**Depends on**: UTOP-ARCH-003 (Bounded Context Map), UTOP-ARCH-006 (Shared Kernel), UTOP-ARCH-008 (Context Ownership Matrix)

**Revision history**:
- 1.0.0 — Initial draft
- 1.1.0 — Post-review revision: Business Calendar ownership made explicit; DateOnly UTC exemption clarified; ReplayClock moved to Infrastructure; System.Text.Json wording corrected; tolerances reclassified as configurable defaults; LocalizedTime wrapper introduced; deferred topics catalogued.
- 1.2.0 — Post-review revision: LocalizedTime.UtcSource usage constraint added; Business Calendar rationale sentence added; ReplayClock exclusion from Shared Kernel restated as DDD justification; scheduler config clarified to affect newly scheduled tasks only.
- 1.3.0 — Final editorial pass: SystemClock comment corrected (thread-safe, not atomic); DateOnly description corrected (same calendar date, not same instant); IBusinessCalendar injection wording corrected (composition root, not infrastructure boundary); LocalizedTime type-system enforcement tracked as UTOP-LLD-LOCALTIME-01. **Document locked.**

---

## 1. Purpose

This document defines the canonical temporal model for the Unified Travel Operations Platform. It governs how time values are stored, transmitted, displayed, and reasoned about across all bounded contexts. Violations of these rules are architectural defects, not implementation preferences.

---

## 2. UTC Storage Rules

### 2.1 The Prime Directive

> **All temporal values persisted to any storage medium — relational database, document store, message queue, cache, audit log, or file — MUST be stored as UTC.**

**Exception — calendar-only values**: `DateOnly` (C#) and `DATE` (SQL) types carry no time component and are therefore timezone-neutral by definition. They are exempt from UTC normalisation. They represent the same calendar date across all timezones (e.g., a booking date of 2025-06-01 names the same calendar day everywhere, irrespective of the observer's local time). Do not store timezone metadata alongside `DateOnly` values.

No other exception exists for this rule. Local time, offset-qualified time, or ambiguous timestamps are never stored. They are derived at presentation time.

### 2.2 Type Mapping by Storage Layer

| Layer | Mandated Type | Notes |
|---|---|---|
| PostgreSQL | `TIMESTAMPTZ` | Physically stores UTC + offset; always write with UTC, always read as UTC. `TIMESTAMP WITHOUT TIME ZONE` is forbidden. |
| PostgreSQL date-only | `DATE` | Calendar date with no time component; timezone-neutral by definition; see §2.1 exception |
| Redis | ISO 8601 string `YYYY-MM-DDTHH:mm:ssZ` | Use `Z` suffix explicitly; never omit |
| RabbitMQ message headers | ISO 8601 string with `Z` | Required on every envelope |
| ELK log events | ISO 8601 string with `Z` | Structured field `@timestamp`; millisecond precision minimum |
| In-memory (C# domain) | `DateTimeOffset` (UTC) | **Never** use `DateTime`; use `DateTimeOffset.UtcNow` exclusively (via `IClock`) |
| JSON serialisation | ISO 8601 string with `Z` | Configure global serialisation to emit UTC ISO 8601 values. The specific serialiser API (System.Text.Json or Newtonsoft.Json) is an LLD decision; the output contract is not. |
| gRPC / Protobuf | `google.protobuf.Timestamp` | Represents seconds + nanos from Unix epoch; inherently UTC |

### 2.3 DateTime vs DateTimeOffset

`System.DateTime` is **banned** in domain and application layer code. It carries ambiguity about timezone that has caused production bugs in every large .NET system that tolerated it. The only permitted exception is interop with legacy libraries that require `DateTime`; in that case, convert at the boundary and document the conversion point.

```csharp
// FORBIDDEN
DateTime now = DateTime.Now;          // local time, no offset
DateTime utc = DateTime.UtcNow;       // UTC but loses offset metadata

// REQUIRED
DateTimeOffset now = IClock.UtcNow;   // UTC with explicit +00:00 — always via IClock
```

### 2.4 Boundary Conversion Points

Incoming time values from external systems (third-party APIs, airline GDS feeds, rail APIs, hotel systems) MUST be converted to UTC at the infrastructure adapter layer before they enter the domain. No external time format propagates past the adapter boundary.

```
[External API response] → [Infrastructure Adapter] → converts to UTC DateTimeOffset
                                                     → enters Domain with clean UTC value
```

---

## 3. IClock Interface and Implementations

### 3.1 Rationale

Hardcoding `DateTimeOffset.UtcNow` in domain or application logic creates a seam that cannot be tested and cannot be replayed. `IClock` abstracts the source of current time, enabling two implementations in Shared Kernel (operational and test) and one infrastructure implementation (replay — see §3.5).

### 3.2 Interface Definition

```csharp
namespace UTOP.SharedKernel.Time;

/// <summary>
/// Abstraction over the current point in time.
/// All domain and application code that requires "now" MUST depend on IClock.
/// Direct calls to DateTimeOffset.UtcNow, DateTime.UtcNow, or DateTime.Now
/// are forbidden outside of IClock implementations.
/// </summary>
public interface IClock
{
    /// <summary>Returns the current instant as a UTC DateTimeOffset.</summary>
    DateTimeOffset UtcNow { get; }
}
```

### 3.3 Operational Implementation (Shared Kernel)

```csharp
namespace UTOP.SharedKernel.Time;

/// <summary>
/// Production clock. Delegates to the system wall clock.
/// Registered as a singleton in the DI container.
/// Thread-safe: DateTimeOffset.UtcNow returns an immutable value.
/// </summary>
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    private SystemClock() { }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

DI registration:
```csharp
services.AddSingleton<IClock>(SystemClock.Instance);
```

### 3.4 Test Implementation (Shared Kernel)

```csharp
namespace UTOP.SharedKernel.Time;

/// <summary>
/// Deterministic clock for unit and integration tests.
/// Starts at a fixed point; can be advanced explicitly by tests.
/// Never use SystemClock in tests — time-dependent tests are not repeatable.
/// Thread-safety: NOT thread-safe. Use one FakeClock per test scope.
///   Do not share a FakeClock instance across parallel test threads.
/// </summary>
public sealed class FakeClock : IClock
{
    private DateTimeOffset _current;

    public FakeClock(DateTimeOffset startTime)
    {
        _current = startTime;
    }

    /// <summary>Convenience constructor — defaults to 2024-01-01T00:00:00Z.</summary>
    public FakeClock() : this(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)) { }

    public DateTimeOffset UtcNow => _current;

    public void AdvanceBy(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        _current += duration;
    }

    public void SetTo(DateTimeOffset instant)
    {
        _current = instant;
    }
}
```

Usage in tests:
```csharp
var clock = new FakeClock(new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero));
var service = new BookingService(clock, ...);

service.CreateBooking(request);
clock.AdvanceBy(TimeSpan.FromHours(2));
service.CheckExpiry(); // deterministic — no wall-clock dependency
```

### 3.5 Replay Implementation (Infrastructure — NOT Shared Kernel)

`ReplayClock` is an event-sourcing infrastructure primitive. It is not part of the ubiquitous language and therefore is excluded from the Shared Kernel. It lives in `UTOP.Infrastructure.EventReplay`, not in Shared Kernel. The Shared Kernel contains only concepts that are genuinely ubiquitous across bounded contexts — replay mechanics are not; they belong to one infrastructure pipeline.

```csharp
namespace UTOP.Infrastructure.EventReplay;

/// <summary>
/// Replay clock for event-sourcing and audit trail re-execution.
/// Advances in lock-step with event consumption, in persisted event-stream order.
/// Identical timestamps do not imply reordering — stream position is authoritative.
/// See UTOP-ARCH-007 (Distributed Consistency) for per-aggregate ordering rules.
///
/// Thread-safety: Single-threaded replay only. Do not share across threads.
/// </summary>
public sealed class ReplayClock : IClock
{
    private readonly Queue<DateTimeOffset> _sequence;
    private DateTimeOffset _current;

    public ReplayClock(IEnumerable<DateTimeOffset> eventTimestamps)
    {
        ArgumentNullException.ThrowIfNull(eventTimestamps);
        _sequence = new Queue<DateTimeOffset>(eventTimestamps);
        _current = _sequence.TryDequeue(out var first)
            ? first
            : DateTimeOffset.MinValue;
    }

    public DateTimeOffset UtcNow => _current;

    /// <summary>
    /// Advances the clock to the next recorded timestamp.
    /// Call once per event consumed during replay.
    /// If the sequence is exhausted, the clock holds at the last known event time.
    /// </summary>
    public void Advance()
    {
        if (_sequence.TryDequeue(out var next))
            _current = next;
    }

    public bool IsExhausted => _sequence.Count == 0;
}
```

---

## 4. Timezone Ownership — Localization Context

### 4.1 Single Authority Rule

**The Localization bounded context is the sole owner of timezone presentation logic.** No other context performs timezone conversion for display. All other contexts store and operate in UTC exclusively and expose UTC values in their APIs.

### 4.2 What Localization Context Owns

- IANA timezone database integration (via any library capable of IANA-to-OS mapping; LLD specifies the package)
- User timezone preference resolution: explicit user setting → inferred from itinerary origin → server default UTC
- Conversion of UTC values to local display values
- Locale-aware formatting (date/time format strings per culture)
- Timezone display name localisation
- All construction of `LocalizedTime` values (see §4.4)

### 4.3 What Other Contexts Must NOT Do

```csharp
// FORBIDDEN in any context other than Localization
DateTimeOffset local = utcValue.ToLocalTime();
DateTimeOffset converted = TimeZoneInfo.ConvertTime(utcValue, someZone);
string formatted = utcValue.ToString("HH:mm", cultureInfo);
TimeZoneInfo.Local // FORBIDDEN — system local timezone is not a valid platform concept
```

### 4.4 Localization API Contract

The `ToLocalTime` method previously returned `DateTimeOffset`. That leaks responsibility: a `DateTimeOffset` can be re-formatted by any caller without going through Localization. Instead, the API returns a `LocalizedTime` value — an opaque display record that carries the formatted string and metadata but does not expose a raw `DateTimeOffset` for downstream manipulation.

```csharp
namespace UTOP.SharedKernel.Time;

/// <summary>
/// An opaque display-ready time value produced by the Localization context.
/// Other contexts MUST NOT reconstruct this; they receive it from ITimeZonePresenter.
///
/// UtcSource is included solely for traceability, correlation, and audit.
/// Consumers MUST NOT use UtcSource for presentation, timezone conversion,
/// or any calculation that bypasses the Localization context.
/// The presence of UtcSource does not grant permission to re-enter the
/// timezone conversion path independently.
///
/// LLD note: Type-system enforcement of these constraints (private members,
/// internal constructor, or interface-only exposure) is an LLD decision.
/// Architecture governs intent; implementation governs enforcement.
/// Tracked: UTOP-LLD-LOCALTIME-01.
/// </summary>
public sealed record LocalizedTime(
    DateTimeOffset UtcSource,          // FOR AUDIT/TRACEABILITY ONLY — see above
    string IanaTimeZoneId,             // e.g. "Asia/Riyadh"
    string DisplayValue,               // e.g. "14:30" or "14:30 AST" — formatted for the user
    string DisplayDate                 // e.g. "01 Jun 2025" — formatted for the user
);
```

```csharp
namespace UTOP.Localization.Application;

public interface ITimeZonePresenter
{
    /// <summary>
    /// Convert a UTC instant to a display-ready LocalizedTime for a given user.
    /// Timezone is resolved from the user's stored preferences.
    /// </summary>
    LocalizedTime FormatForUser(DateTimeOffset utcInstant, UserId userId, string formatHint = "default");

    /// <summary>
    /// Convert a UTC instant to a LocalizedTime for a given IANA timezone.
    /// Use only when user context is unavailable (e.g., public schedule display, shared itinerary links).
    /// </summary>
    LocalizedTime ForLocalTime(DateTimeOffset utcInstant, string ianaTimeZoneId);
}
```

---

## 5. Business-Day Semantics

### 5.1 Definition

A *business day* in UTOP is context-specific. The definition varies by country, sector, and route.

### 5.2 Business Calendar Ownership

**The Business Calendar is owned by the Localization bounded context as a supporting sub-domain.** It belongs to Localization because it resolves jurisdictional calendar semantics — what counts as a working day in a given country and sector — rather than operational business rules specific to booking, payment, or pilgrimage. It is not a separate bounded context, and it is not owned by Booking, Payment, or Pilgrimage. Those contexts depend on the `IBusinessCalendar` abstraction via constructor injection at the composition root. The infrastructure implementation of `IBusinessCalendar` resides in Localization's infrastructure layer. No other context provides its own implementation. Ownership fragmentation at this boundary is explicitly prohibited.

### 5.3 IBusinessCalendar Interface

```csharp
namespace UTOP.SharedKernel.Time;

public interface IBusinessCalendar
{
    /// <summary>
    /// Returns true if the given calendar date is a business day
    /// within the specified calendar context.
    /// </summary>
    bool IsBusinessDay(DateOnly date, CalendarContext context);

    /// <summary>
    /// Returns the next business day at or after the given date.
    /// </summary>
    DateOnly NextBusinessDay(DateOnly date, CalendarContext context);

    /// <summary>
    /// Returns the number of business days between two dates,
    /// inclusive of start, exclusive of end.
    /// </summary>
    int BusinessDaysBetween(DateOnly from, DateOnly to, CalendarContext context);
}

public sealed record CalendarContext(
    string CountryCode,       // ISO 3166-1 alpha-2
    string? RegionCode,       // ISO 3166-2 subdivision (optional)
    string? SectorCode        // "pilgrimage" | "aviation" | "rail" (optional)
);
```

### 5.4 Pilgrimage-Specific Rules

The Hajj and Umrah corridors operate on the Islamic (Hijri) calendar for sanctioned travel windows:

- Ihram boundary dates are Hijri dates translated to Gregorian for system storage.
- The Miqat window is stored as a `DateOnly` pair (Gregorian UTC calendar date), not as timestamps.
- Business day rules for Saudi Arabia (`SA`) apply inside the Miqat window; home-country rules apply outside.
- Implementation uses `System.Globalization.UmAlQuraCalendar` for Saudi civil date conversion and `HijriCalendar` for liturgical purposes. LLD specifies which methods and correction factors apply.

### 5.5 Settlement Deadlines

Financial settlement deadlines (refunds, fare adjustments, penalty charges) use business days under the payment processor's jurisdiction, not the traveller's jurisdiction. The `CalendarContext` for settlement operations MUST use the payment context's `CountryCode`.

---

## 6. Scheduler Skew Handling

### 6.1 Problem Statement

UTOP uses scheduled tasks (fare expiry checks, seat-hold releases, notification triggers, prayer time pre-computation). In a containerised, horizontally-scaled deployment, scheduler processes are not perfectly synchronised with wall-clock time. Skew — the gap between the scheduled fire time and actual execution time — is inevitable and must be handled explicitly.

### 6.2 Tolerance Thresholds

The values below are **configurable operational defaults**. They MUST NOT be hardcoded in source. They are externalised to application configuration (environment variable or configuration service) so that each deployment environment can tune them without a code change.

| Task Category | Default Maximum Tolerated Skew | Default Action if Exceeded |
|---|---|---|
| Seat-hold release | 30 seconds | Execute immediately; log skew; do not skip |
| Fare expiry notification | 2 minutes | Execute immediately; log skew |
| Prayer time reminder | 5 minutes | Execute immediately; log skew; suppress if prayer time already passed |
| Daily schedule pre-computation | 10 minutes | Execute immediately; log skew |
| Audit log compaction | 1 hour | Execute; log skew; no functional impact |

Configuration key pattern: `Scheduler:SkewTolerances:{TaskCategory}` (seconds).

Configuration changes to skew tolerances affect **newly scheduled tasks only**. Already-scheduled tasks that are in-flight or queued at the time of a configuration change continue to execute under the tolerance value that was active when they were scheduled. Retroactive re-evaluation of in-flight tasks is not performed unless explicitly documented as part of a deployment procedure.

### 6.3 Skew Detection

```csharp
public sealed class ScheduledTaskContext
{
    public DateTimeOffset ScheduledAt { get; init; }   // When it was supposed to fire
    public DateTimeOffset ExecutedAt { get; init; }    // IClock.UtcNow at actual execution
    
    public TimeSpan Skew => ExecutedAt - ScheduledAt;
    public bool IsSkewed(TimeSpan tolerance) => Skew > tolerance;
}
```

### 6.4 Skip-on-Stale Rule

Prayer time reminders are the only task category with a **skip-on-stale** rule: if the scheduled prayer time has passed by more than the configured skew tolerance at the moment of execution, the reminder is suppressed and logged as `SKIPPED_STALE`. Sending a reminder for a prayer that has already started is a higher-severity UX defect than a missed notification.

### 6.5 Distributed Lock

Horizontally-scaled schedulers MUST acquire a distributed lock before executing any scheduled task to prevent duplicate execution when multiple instances race on the same task.

Lock key pattern: `scheduler:task:{taskType}:{taskId}`  
Lock TTL: `scheduledAt + maxSkewTolerance + 60s`

**Implementation requirement**: The lock MUST use an ownership token (a unique value set on acquisition and verified on release). A plain `DEL` on release is forbidden — it can delete a lock acquired by a different instance if the original holder's TTL expired. Use compare-and-delete via a Lua script, or use a library that implements this correctly (e.g., RedLock.net or equivalent). LLD specifies the chosen implementation.

```lua
-- Safe unlock: only delete if the lock value matches our ownership token
if redis.call("get", KEYS[1]) == ARGV[1] then
    return redis.call("del", KEYS[1])
else
    return 0
end
```

---

## 7. DST Handling Rules

### 7.1 UTC Immunity

Because all storage is UTC, DST transitions are invisible to the persistence and domain layers. DST is exclusively a presentation-layer concern, owned by the Localization context.

### 7.2 The Ambiguous Hour Problem

When a DST clock falls back, a wall-clock hour occurs twice. Any time value produced by a user input during that hour is ambiguous. Resolution rule:

> **When a user-supplied local time falls in a DST ambiguous window, always resolve to the later UTC equivalent (post-transition).**

This is the conservative choice: it avoids the system treating a post-transition action as having occurred pre-transition, which could trigger premature expirations.

```csharp
public static DateTimeOffset ResolveAmbiguous(
    DateTime localTime,
    string ianaTimeZoneId)
{
    var tz = /* resolve IANA timezone via platform-appropriate library */;
    if (tz.IsAmbiguousTime(localTime))
    {
        var offsets = tz.GetAmbiguousTimeOffsets(localTime);
        var laterOffset = offsets.Min(); // Min = more negative = later in UTC
        return new DateTimeOffset(localTime, laterOffset);
    }
    return new DateTimeOffset(localTime, tz.GetUtcOffset(localTime));
}
```

### 7.3 DST Gap Handling (Spring Forward)

When a DST clock springs forward, some wall-clock times do not exist. If a scheduled task or user-input time falls in a gap:

> **Map the non-existent time forward to the first valid time after the gap.**

```csharp
if (tz.IsInvalidTime(localTime))
{
    var delta = tz.GetAdjustmentRules()
        .FirstOrDefault(r => r.DateStart <= localTime.Date && r.DateEnd >= localTime.Date)
        ?.DaylightDelta ?? TimeSpan.FromHours(1);
    localTime = localTime.Add(delta);
}
```

### 7.4 Booking Straddling a DST Transition

Itineraries that span a DST boundary store all timestamps in UTC and display each leg in the local time applicable at that leg's location and instant. The display layer makes the transition visible to the user: *"Clocks advance 1 hour on [date] in [region]."*

---

## 8. Prayer Schedule Time Precision Requirements

### 8.1 Context

Prayer time scheduling is a first-class travel coordination feature for Muslim travellers. It affects itinerary sequencing, layover validation, and group synchronisation for pilgrimage journeys.

### 8.2 Precision Standard

Prayer times are calculated to **second-level precision** in UTC and stored as `DateTimeOffset`. They are displayed to **minute-level precision** in local time.

Rationale: Calculation methods produce second-level results. Storing to the second preserves accuracy for itinerary constraint evaluation. Displaying to the minute avoids false precision for users.

### 8.3 Calculation Authority

Prayer times are not calculated in-application from solar algorithms. They are sourced from an external calculation service or a pre-computed offline dataset for known routes. The infrastructure adapter normalises results to `DateTimeOffset` UTC.

Cache key: `(Date, Latitude, Longitude, CalculationMethod)`  
Cache TTL: 30 days.

Rationale for TTL: Cached entries are deterministic for a fixed calculation method and dataset version. A 30-day TTL guards against serving stale results if the calculation method or dataset is updated (a version bump triggers cache invalidation). Astronomical values themselves are stable indefinitely, but the authoritative source definition is not.

### 8.4 Prayer Time Value Object

```csharp
namespace UTOP.SharedKernel.Time;

/// <summary>
/// Immutable record of the five daily prayer windows for a given day and location.
/// All times stored in UTC. Calculation method preserved for auditability.
/// </summary>
public sealed record DailyPrayerSchedule
{
    public DateOnly Date { get; init; }                   // Calendar date (timezone-neutral)
    public GeoCoordinate Location { get; init; }          // Lat/Lon of calculation point
    public string CalculationMethod { get; init; }        // "UmmAlQura" | "ISNA" | "MWL" | "Karachi"
    
    public DateTimeOffset Fajr { get; init; }
    public DateTimeOffset Sunrise { get; init; }           // Marks end of Fajr window
    public DateTimeOffset Dhuhr { get; init; }
    public DateTimeOffset Asr { get; init; }
    public DateTimeOffset Maghrib { get; init; }
    public DateTimeOffset Isha { get; init; }

    public DateTimeOffset? Jumuah { get; init; }           // Friday prayer; replaces Dhuhr on Fridays

    public IReadOnlyList<PrayerWindow> AsPrayerWindows()
    {
        var windows = new List<PrayerWindow>
        {
            new(Prayer.Fajr, Fajr, Sunrise),
            new(Prayer.Dhuhr, Dhuhr, Asr),
            new(Prayer.Asr, Asr, Maghrib),
            new(Prayer.Maghrib, Maghrib, Isha),
            // Isha window intentionally spans midnight to the next day's Fajr
            new(Prayer.Isha, Isha, Fajr.AddDays(1))
        };
        if (Jumuah.HasValue)
            windows.Add(new(Prayer.Jumuah, Jumuah.Value, Asr));
        return windows.AsReadOnly();
    }
}

public sealed record PrayerWindow(
    Prayer Prayer,
    DateTimeOffset Start,
    DateTimeOffset End)
{
    public bool IsActive(DateTimeOffset utcNow) =>
        utcNow >= Start && utcNow < End;

    public TimeSpan TimeUntil(DateTimeOffset utcNow) =>
        utcNow < Start ? Start - utcNow : TimeSpan.Zero;
}

public enum Prayer { Fajr, Dhuhr, Asr, Maghrib, Isha, Jumuah }
```

### 8.5 Itinerary Constraint Integration

When validating a pilgrimage itinerary segment, the scheduler checks whether the travel window overlaps with any prayer window for the departure location. The minimum layover margin before the next prayer end time is a **configurable operational default** (initial value: 20 minutes). Configuration key: `Pilgrimage:PrayerMarginMinutes`.

Violations are surfaced as `ItineraryWarning` — not hard errors. The traveller may consciously choose to travel during a prayer window.

```csharp
public sealed record ItineraryWarning(
    WarningCode Code,
    string Description,
    DateTimeOffset ReferencedAt,
    Prayer? AffectedPrayer);
```

---

## 9. Deferred Topics

The following temporal concerns are acknowledged but explicitly deferred. They are not omissions — they are out of scope for this stabilization artifact and will be addressed in LLD or a future architecture revision.

| Topic | Disposition |
|---|---|
| Monotonic time / elapsed time for timeout calculations | LLD — specific timeout implementations will specify |
| Leap seconds | Delegated to OS and .NET runtime. UTOP does not handle leap seconds explicitly. |
| NTP synchronisation requirements / server clock drift monitoring | Operations runbook (not architecture) |
| Event ordering when two events share identical timestamps | Cross-reference UTOP-ARCH-007 §[aggregate ordering] — stream position is authoritative when timestamps collide |
| Temporal versioning (Effective From / Effective To periods) | Not required in Phase 3 scope. If introduced, Booking or Inventory LLD will specify. |

---

## 10. Prohibited Patterns (Enforcement Checklist)

The following patterns constitute temporal model violations. Code review MUST reject PRs containing any of these:

| Pattern | Status | Correct Alternative |
|---|---|---|
| `DateTime.Now` | **FORBIDDEN** | `IClock.UtcNow` |
| `DateTime.UtcNow` | **FORBIDDEN** | `IClock.UtcNow` |
| `DateTimeOffset.Now` | **FORBIDDEN** | `IClock.UtcNow` |
| `DateTimeOffset.UtcNow` in domain/app layer | **FORBIDDEN** | `IClock.UtcNow` |
| `DateTimeOffset.UtcNow` in `IClock` implementation | **PERMITTED** | — |
| `TimeZoneInfo.Local` | **FORBIDDEN** | Explicit IANA timezone string |
| `.ToLocalTime()` outside Localization context | **FORBIDDEN** | Route to `ITimeZonePresenter` |
| Returning raw `DateTimeOffset` from Localization display API | **FORBIDDEN** | Return `LocalizedTime` |
| `TIMESTAMP WITHOUT TIME ZONE` in PostgreSQL | **FORBIDDEN** | Use `TIMESTAMPTZ` |
| Storing timezone as integer offset (e.g., `+5:30`) | **FORBIDDEN** | Store IANA timezone ID string |
| `new ReplayClock(...)` outside `Infrastructure.EventReplay` | **FORBIDDEN** | ReplayClock is infrastructure-only |
| Redis lock release without ownership token verification | **FORBIDDEN** | Compare-and-delete via Lua script |
| Hardcoded skew tolerance values in source | **FORBIDDEN** | Externalise to configuration |
| Hardcoded prayer margin in source | **FORBIDDEN** | Externalise to configuration |
| Comparing `DateOnly` against `DateTimeOffset` directly | **FORBIDDEN** | Convert explicitly |
| Hardcoded prayer times as string constants | **FORBIDDEN** | Use `DailyPrayerSchedule` from service |

---

## 11. Inter-Context Summary

| Bounded Context | Stores | Displays | Converts? | Business Calendar? |
|---|---|---|---|---|
| Booking | UTC `DateTimeOffset` | UTC only in internal ops | No | Consumes via `IBusinessCalendar` |
| Inventory | UTC `DateTimeOffset` | UTC only | No | Consumes via `IBusinessCalendar` |
| Scheduling | UTC `DateTimeOffset` | UTC only | No | Consumes via `IBusinessCalendar` |
| Payment | UTC `DateTimeOffset` | UTC only | No | Consumes (payment jurisdiction) |
| Localization | UTC (input) | `LocalizedTime` (output) | **Yes — sole authority** | **Owns** |
| Notification | UTC `DateTimeOffset` | Delegates to Localization | No | — |
| Pilgrimage | UTC `DateTimeOffset` + `DailyPrayerSchedule` | Delegates to Localization | No | Consumes via `IBusinessCalendar` |

---

## 12. Related Documents

- UTOP-ARCH-003: Bounded Context Map
- UTOP-ARCH-006: Shared Kernel Contents (IClock, FakeClock, IBusinessCalendar, DailyPrayerSchedule are Shared Kernel members; ReplayClock is NOT)
- UTOP-ARCH-007: Distributed Consistency & Concurrency (event ordering, per-aggregate sequence rules)
- UTOP-ARCH-008: Context Ownership Matrix
- UTOP-ARCH-010: Shared Kernel Governance (admission and anti-bloat rules)

---

*Document owner: UTOP Architecture Board*  
*Baselined: 2025-06-30*  
*Next review: Prior to LLD kickoff*
