# UTOP Shared Kernel Governance
**Document ID**: UTOP-ARCH-010  
**Version**: 1.0.0  
**Status**: Locked  
**Branch**: feature/phase3-stabilization  
**Depends on**: UTOP-ARCH-003 (Bounded Context Map), UTOP-ARCH-006 (Shared Kernel Contents), UTOP-ARCH-008 (Context Ownership Matrix), UTOP-ARCH-009 (Temporal Semantics)

---

## 1. Purpose

The Shared Kernel is the most dangerous architectural construct in a bounded-context system. Used correctly it eliminates duplication of concepts that are genuinely universal. Used carelessly it becomes a dumping ground that couples every context to every other context and defeats the purpose of the bounded-context model entirely.

This document governs what enters the Shared Kernel, who decides, how it is versioned, and — critically — how items leave it. It is a governance document, not a catalogue. The catalogue is UTOP-ARCH-006.

---

## 2. Ownership Authority

### 2.1 Single Owner

The Shared Kernel has one owner: the **Architecture Board**. For this platform at its current scale, the Architecture Board is the lead architect role. There is no committee vote, no majority rule, no per-team approval. Admission and removal decisions are unilateral and documented.

This is intentional. Shared Kernel governance by committee produces bloat. Every team can argue that their abstraction is universal. The Architecture Board exists to say no.

### 2.2 Responsibilities of the Owner

- Final admission decision on all proposed additions
- Versioning authority — only the owner increments the Shared Kernel version
- Anti-bloat enforcement — periodic review, no exceptions
- Extraction decisions — when a Shared Kernel item graduates to its own context
- Breaking-change protocol initiation — the owner coordinates cross-context migration

### 2.3 Who May Propose

Any bounded context team may propose an addition. Proposals are submitted as an Architecture Decision Record (ADR) against the Shared Kernel. The proposal is not an addition — it is a request. Admission follows the criteria in §3.

---

## 3. Admission Criteria

A concept may enter the Shared Kernel if and only if it satisfies **all five** of the following criteria. Satisfying four of five is not sufficient.

### Criterion 1 — Universal Reference

The concept is referenced by **three or more** bounded contexts, and duplication of the concept across those contexts would produce divergence risk — not merely code duplication.

*Code duplication is tolerable. Semantic divergence is not.*

A `Money` type that means different things in Booking and Payment is a defect. Duplicate `Money` implementations that mean the same thing are merely untidy. The Shared Kernel resolves semantic divergence, not untidiness.

### Criterion 2 — Stable Definition

The concept's definition is stable. It does not change with business rules, market conditions, or feature delivery. Concepts that evolve with product requirements do not belong in the Shared Kernel — they belong in the context that owns them.

If a proposed concept has changed definition in the last six months, it fails this criterion.

### Criterion 3 — No Owning Context

The concept has no natural home in any single bounded context. If the concept clearly belongs to Booking, it belongs in Booking — not the Shared Kernel. The Shared Kernel is for concepts that are genuinely pre-contextual: they exist before any context is defined and remain valid regardless of which contexts exist.

### Criterion 4 — Zero Business Logic

The concept carries no business logic. It is a structural primitive — a value object, an interface, an enumeration, a constraint type. The moment a concept requires business rules to operate, it has an owner and that owner is a bounded context.

A `Money` type that enforces non-negative value is a constraint. A `Money` type that calculates VAT is business logic. The former belongs in the Shared Kernel. The latter belongs in Tax or Payment.

### Criterion 5 — Cross-Cutting Implementation Cost

The cost of independent implementation in each consuming context — including the risk of divergence, the testing burden, and the maintenance overhead — materially exceeds the coupling cost of a Shared Kernel entry.

This criterion exists to prevent premature generalisation. A concept used by three contexts in identical form may still not belong in the Shared Kernel if each context can implement it trivially and independently with no divergence risk.

---

## 4. Forbidden Items

The following categories are **permanently ineligible** for the Shared Kernel regardless of how many contexts reference them.

| Category | Reason | Correct Location |
|---|---|---|
| Business rules or domain policies | Belong to a specific context | The owning bounded context |
| Aggregate roots or entities | Never shared across contexts | The owning bounded context |
| Repository interfaces | Infrastructure boundary, not universal | Infrastructure layer of owning context |
| Application service interfaces | Application layer concern | Application layer of owning context |
| DTOs or API contracts | Cross-context communication uses events | Integration event definitions |
| Context-specific value objects | Owned concepts | The owning bounded context |
| Configuration values | Operational concern | Configuration service |
| Infrastructure implementations | Not universal primitives | Infrastructure layer |
| `ReplayClock` or equivalent infrastructure time utilities | Not part of the ubiquitous language | `Infrastructure.EventReplay` |
| Logging abstractions | Cross-cutting infrastructure concern | Infrastructure layer; use `ILogger<T>` from the platform |
| Shared exception base classes | Produces hidden coupling | Each context defines its own exception hierarchy |
| Anything referencing a specific bounded context by name | Circular dependency | Not applicable — redesign |

---

## 5. Current Approved Contents

The following items are the complete and current Shared Kernel inventory as of Phase 3 stabilization. Nothing else is in the Shared Kernel. Items not on this list are not Shared Kernel members regardless of where they physically reside.

### 5.1 Money

```csharp
namespace UTOP.SharedKernel;

public sealed record Money(decimal Amount, Currency Currency)
{
    public Money { Amount = Amount >= 0 ? Amount : throw new ArgumentOutOfRangeException(nameof(Amount)); }
    public Money Add(Money other) { /* same currency guard */ }
    public Money Subtract(Money other) { /* same currency guard; non-negative result guard */ }
    public static Money Zero(Currency currency) => new(0m, currency);
}

public enum Currency { SAR, USD, EUR, GBP, INR, AED, MYR, IDR /* extensible — see §7 */ }
```

**Admission rationale**: Referenced by Booking, Payment, Inventory, and Pilgrimage. Semantic divergence risk is high — a `Money` that allows negative amounts in Booking but not in Payment is a defect waiting to happen. No owning context. Zero business logic (non-negativity is a constraint, not a rule). Stable definition.

**Constraints**: `Money` performs arithmetic only. It does not convert currencies. Currency conversion belongs to a dedicated FX service (not yet defined — to be resolved in LLD).

### 5.2 DateRange

```csharp
namespace UTOP.SharedKernel;

public sealed record DateRange(DateOnly Start, DateOnly End)
{
    public DateRange
    {
        if (End < Start) throw new ArgumentException("End must be on or after Start.");
    }
    
    public bool Contains(DateOnly date) => date >= Start && date <= End;
    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;
    public int DurationInDays => End.DayNumber - Start.DayNumber;
}
```

**Admission rationale**: Referenced by Booking (travel window), Inventory (availability window), Scheduling (service window), and Pilgrimage (Miqat window). The semantics of a date range are identical in all four contexts — a start date, an end date, non-negative duration, overlap detection. No business logic. Stable.

**Constraints**: `DateRange` operates on `DateOnly` — it is calendar-range only. Time-precision ranges (e.g., a booking slot with hour-level granularity) are context-specific and do not belong here.

### 5.3 Location

```csharp
namespace UTOP.SharedKernel;

public sealed record Location(
    string Code,           // IATA airport code, GTFS stop ID, or platform-defined location code
    LocationType Type,
    string? DisplayName    // Optional; presentation only
);

public enum LocationType { Airport, RailStation, BusTerminal, SeaPort, Accommodation, PilgrimagePoint, City }
```

**Admission rationale**: Referenced by Booking, Inventory, Scheduling, Routing, and Pilgrimage. A `Location` in the route planning sense is identical across all contexts — an identifier, a type, an optional display label. No owning context. No business logic.

**Constraints**: `Location` is an identity and classification primitive. It does not carry geographic coordinates, operating hours, capacity, or any enrichment — those are owned by the Location Management context (or whichever context needs them). Enrichment is looked up; it is not stored in the Shared Kernel record.

### 5.4 CorrelationId

```csharp
namespace UTOP.SharedKernel;

public readonly record struct CorrelationId(Guid Value)
{
    public static CorrelationId New() => new(Guid.NewGuid());
    public static CorrelationId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
```

**Admission rationale**: Required on every command, event, and API request for distributed tracing and audit. Referenced by all contexts. Generating a `Guid` is not the concern — the type discipline of carrying a `CorrelationId` rather than a raw `Guid` is. No business logic. Fully stable.

**Constraints**: `CorrelationId` wraps a `Guid`. It is not a `string`. It is not a composite key. Contexts that need causation or conversation identifiers define their own typed wrappers (`CausationId`, `ConversationId`) following the same pattern — they do not reuse `CorrelationId` for a different semantic purpose.

### 5.5 PassengerCount

```csharp
namespace UTOP.SharedKernel;

public readonly record struct PassengerCount(int Adults, int Children, int Infants)
{
    public PassengerCount
    {
        if (Adults < 1) throw new ArgumentOutOfRangeException(nameof(Adults), "At least one adult required.");
        if (Children < 0) throw new ArgumentOutOfRangeException(nameof(Children));
        if (Infants < 0) throw new ArgumentOutOfRangeException(nameof(Infants));
        if (Infants > Adults) throw new ArgumentException("Infants may not exceed adults (lap infant rule).");
    }
    
    public int Total => Adults + Children + Infants;
}
```

**Admission rationale**: Referenced by Booking, Inventory, Pricing, and Pilgrimage. The composition of a travel party (adults, children, infants) and the lap-infant constraint are universal across all travel domains — they derive from IATA and international aviation regulations, not from UTOP business rules. No owning context. Stable.

**Constraints**: `PassengerCount` enforces structural validity only. It does not apply age bands, nationality rules, or visa requirements — those are Pricing and Compliance context concerns.

### 5.6 IClock

```csharp
namespace UTOP.SharedKernel.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

With implementations `SystemClock` (production singleton) and `FakeClock` (test). Full specification in UTOP-ARCH-009 §3.

**Admission rationale**: Every context that creates domain events, checks expiry, or schedules work depends on a testable time source. `IClock` is the definition of the ubiquitous language concept "now." No business logic. Fully stable. `ReplayClock` is explicitly excluded — see UTOP-ARCH-009 §3.5.

### 5.7 Supporting Types

The following small types support the above members and are admitted as part of their parent concept:

| Type | Parent | Notes |
|---|---|---|
| `Currency` (enum) | `Money` | Extensible via §7 governance process |
| `LocationType` (enum) | `Location` | Extensible via §7 governance process |
| `GeoCoordinate` | `Location` (and `DailyPrayerSchedule`) | Lat/lon pair; no logic |
| `DailyPrayerSchedule` | `IClock` / Temporal | Full spec in UTOP-ARCH-009 §8 |
| `PrayerWindow` | `DailyPrayerSchedule` | Full spec in UTOP-ARCH-009 §8 |
| `Prayer` (enum) | `DailyPrayerSchedule` | Full spec in UTOP-ARCH-009 §8 |
| `IBusinessCalendar` | Temporal | Owned by Localization; interface lives in Shared Kernel |
| `CalendarContext` | `IBusinessCalendar` | Parameter record; no logic |
| `LocalizedTime` | Temporal / Localization | Display record; UtcSource for audit only |
| `CorrelationId` | — | Standalone |

---

## 6. Anti-Bloat Policy

### 6.1 The Default Answer is No

When in doubt about whether something belongs in the Shared Kernel, the answer is no. The proposing team bears the full burden of proof across all five admission criteria. Partial satisfaction is rejection.

### 6.2 Size Limit

The Shared Kernel is not a library. It is a precision instrument. If the Shared Kernel grows beyond **fifteen top-level types** (not counting enums and small supporting records), that is a signal that the anti-bloat policy has been compromised. A review is mandatory at that threshold.

Current count: **10 top-level types** (Money, DateRange, Location, CorrelationId, PassengerCount, IClock, GeoCoordinate, DailyPrayerSchedule, IBusinessCalendar, LocalizedTime). Headroom: 5.

### 6.3 Periodic Review

The Shared Kernel contents are reviewed at the start of every major phase (LLD kickoff, each implementation milestone, pre-release). The review asks one question per item: *does this still satisfy all five admission criteria?* Items that no longer do are candidates for extraction (see §8).

### 6.4 No Convenience Additions

"It would be convenient to put this in the Shared Kernel" is not an admission argument. Convenience is the exact reasoning that produces bloat. The test is necessity, not convenience.

### 6.5 No Transitive Additions

An item admitted to the Shared Kernel does not automatically admit its dependencies. Each dependency is evaluated independently. If a dependency fails any admission criterion, it does not enter the Shared Kernel — the admitted item must be redesigned to remove the dependency.

---

## 7. Versioning Rules

### 7.1 Shared Kernel Version

The Shared Kernel carries its own semantic version, independent of platform version. Current version: **1.0.0**.

Format: `MAJOR.MINOR.PATCH`

| Change Type | Version Increment | Coordination Required |
|---|---|---|
| New top-level type admitted | MINOR | Announce; consuming contexts opt in |
| New enum value added | MINOR | Announce; consuming contexts handle unknown values |
| Bug fix with no signature change | PATCH | Low friction; inform consuming contexts |
| Constraint tightened (non-breaking in practice) | MINOR | Announce; consuming contexts verify compliance |
| Type signature changed | MAJOR | Full breaking-change protocol (see §7.2) |
| Type removed | MAJOR | Full breaking-change protocol (see §7.2) |
| Semantic meaning changed | MAJOR | Full breaking-change protocol (see §7.2) |

### 7.2 Breaking Change Protocol

A breaking change (MAJOR version increment) follows this mandatory sequence:

1. **Proposal**: Architecture Board issues a breaking-change notice with the proposed change, rationale, and migration path.
2. **Impact assessment**: All consuming contexts identify affected code and estimate migration effort. Deadline: one sprint.
3. **Migration window**: A migration window is agreed — minimum two sprints. Both old and new versions coexist during this window if feasible.
4. **Parallel support**: Where feasible, the Shared Kernel supports both the old and new form during the migration window. The old form is marked `[Obsolete]` with a migration message.
5. **Hard cutover**: At the end of the migration window, the old form is removed. No extensions.
6. **Tag**: The Shared Kernel package is tagged at the new MAJOR version.

No context may block a breaking change indefinitely. If a context cannot complete migration within the agreed window, it escalates to the Architecture Board — the migration window may be extended once. A second extension requires Architecture Board approval and a documented reason.

### 7.3 Enum Extension Policy

Enum values in the Shared Kernel (`Currency`, `LocationType`) are extended by the Architecture Board only. A bounded context that needs a new currency or location type submits a request. The Architecture Board evaluates whether the new value is genuinely universal or context-specific. Context-specific enum values are defined in the requesting context, not in the Shared Kernel.

All consuming contexts that switch on Shared Kernel enums MUST handle unknown values gracefully. A `default` case that throws is acceptable. A `default` case that silently ignores is not — unknown values must be logged at minimum.

---

## 8. Extraction Criteria

Extraction is the process by which a Shared Kernel item graduates out of the Shared Kernel into its own bounded context or into the context that owns it. Extraction is a promotion, not a demotion.

### 8.1 Triggers for Extraction Review

An item is a candidate for extraction when any of the following is true:

| Trigger | Explanation |
|---|---|
| The item has accumulated business logic | It now has an owning context by definition |
| Fewer than three contexts reference it | It no longer satisfies Criterion 1 |
| One context has become the primary owner | The concept has found its natural home |
| The item is causing cross-context coupling friction | The coupling cost now exceeds the divergence risk |
| A new bounded context emerges that naturally owns this concept | Ownership has become clear |

### 8.2 Extraction Process

1. **Identify the target context** — which context is the natural owner?
2. **Notify consuming contexts** — they will need to depend on the new owner via an interface or integration event rather than a direct Shared Kernel import.
3. **Define the new interface boundary** — consuming contexts receive the concept via service interface, query, or event payload. Direct type sharing ends.
4. **Migration window** — same structure as breaking-change protocol (§7.2).
5. **Remove from Shared Kernel** — MAJOR version increment.
6. **Document** — the extracted item's history is preserved in the Shared Kernel changelog.

### 8.3 Tracked Extraction Candidates

The following items are currently under observation. They are not yet extraction candidates but will be reviewed at LLD kickoff:

| Item | Observation | Review Trigger |
|---|---|---|
| `DailyPrayerSchedule` | May belong in a dedicated Prayer Time service if that service becomes a first-class bounded context | LLD kickoff — if Prayer Time service is scoped as a context |
| `IBusinessCalendar` | Currently interface-in-Shared-Kernel, implementation-in-Localization. If business calendar logic grows significantly, it may warrant its own supporting sub-domain with a published API | LLD kickoff — if calendar rules expand beyond jurisdictional lookup |
| `LocalizedTime` | Type-system enforcement deferred to LLD (UTOP-LLD-LOCALTIME-01). If enforcement requires significant Localization-specific machinery, the type may migrate fully into Localization and be consumed via API only | LLD — UTOP-LLD-LOCALTIME-01 resolution |

---

## 9. Governance Process — Adding a New Item

Any team proposing a Shared Kernel addition follows this process:

**Step 1 — ADR Draft**  
Submit an Architecture Decision Record with: the proposed type, the full admission criteria evaluation (all five), the list of consuming contexts, and the proposed version increment.

**Step 2 — Architecture Board Review**  
The Architecture Board reviews against all five criteria. Decision: Admit / Reject / Defer. Rejection includes the criterion that failed and a recommended alternative (context ownership, duplication, or integration event).

**Step 3 — If Admitted**  
- Type is defined in the Shared Kernel package
- Version is incremented per §7.1
- UTOP-ARCH-006 is updated
- This document (§5) is updated
- Consuming contexts are notified

**Step 4 — If Rejected**  
The ADR is closed with the rejection reason. The proposing team implements the concept in their own context. The rejection is not revisited unless material circumstances change (new contexts emerge, the concept's scope changes).

---

## 10. Prohibited Patterns (Enforcement Checklist)

| Pattern | Status | Correct Alternative |
|---|---|---|
| Adding a type to the Shared Kernel without ADR | **FORBIDDEN** | Submit ADR; await Architecture Board decision |
| Shared Kernel type containing business logic | **FORBIDDEN** | Move logic to owning context |
| Shared Kernel type referencing a specific bounded context | **FORBIDDEN** | Redesign — this is circular coupling |
| Using a Shared Kernel type as a DTO across context boundaries | **FORBIDDEN** | Define an integration event payload |
| Importing `UTOP.SharedKernel` from an infrastructure layer without passing through domain | **FORBIDDEN** | Domain imports Shared Kernel; infrastructure imports domain |
| Extending a Shared Kernel enum in a bounded context assembly | **FORBIDDEN** | Request extension via Architecture Board; use local enum in the interim |
| `ReplayClock` in `UTOP.SharedKernel.Time` | **FORBIDDEN** | `UTOP.Infrastructure.EventReplay` |
| Shared Kernel types carrying nullable navigation properties | **FORBIDDEN** | Shared Kernel types are self-contained value objects |
| Skipping the breaking-change protocol for a MAJOR change | **FORBIDDEN** | Follow §7.2 without exception |

---

## 11. Related Documents

- UTOP-ARCH-003: Bounded Context Map
- UTOP-ARCH-006: Shared Kernel Contents (catalogue; this document governs it)
- UTOP-ARCH-008: Context Ownership Matrix
- UTOP-ARCH-009: Temporal Semantics (IClock, DailyPrayerSchedule, IBusinessCalendar, LocalizedTime specifications)

---

*Document owner: UTOP Architecture Board*  
*Baselined: Phase 3 Stabilization*  
*Next review: LLD Kickoff*
