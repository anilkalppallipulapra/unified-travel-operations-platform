# UTOP Solution Structure — v2 (Implementation Kickoff)

**Status**: Backend structure locked. Frontend structure locked.
**Phase**: Phase 5 — Implementation kickoff, .NET 10 solution scaffold + React/TS frontend scaffold
**Supersedes**: `02_high_level_design.md` §2.1 (Frontend Role-Based UI Structure) and §3.1 (Solution Structure) — both diagrams predate Phase 3 stabilization; §3.1 was missing one context and understating the Shared Kernel, §2.1 is missing feature folders for 4 of the 13 contexts.
**Feeds from**: UTOP-ARCH-001 (ADR-001, Modular Monolith), UTOP-ARCH-008 (context ownership matrix), UTOP-ARCH-010 (shared kernel governance)

---

## 1. What's confirmed and why

- **Modular Monolith, single deployable unit, 13 bounded contexts** — ADR-001. Each context gets its own project so it stays extractable to a separate service later without a rewrite.
- **13 contexts split into three structural patterns**, not one uniform shape, because they don't do the same job:
  - **Full DDD scaffold** (has real aggregate lifecycle, state transitions, sagas): Booking, Accommodation, ResourceAllocation, Pilgrimage, GroupManagement, CostSplitting, Notifications, KnowledgeBase, AIRecommendation, Identity, Localization — 11 contexts.
  - **Lightweight module** (rules/config, no lifecycle, no aggregate): TravelCategory — 1 context.
  - **Projection-host** (no writes, no aggregates, consumes everything, forbidden from being a source of truth): Analytics — 1 context.
- **Shared Kernel corrected to the actual ratified 10 types** (ARCH-010 §5), not the 3-type snapshot from the old HLD diagram.
- **Frontend feature folders mapped 1:1 to all 13 backend contexts.** The old HLD diagram (§2.1) had 9 context folders plus a generic `admin` bucket — missing AIRecommendation, Identity, Localization, and TravelCategory entirely as feature folders. Those four existed, if at all, only inside `shared/` (`auth`, `i18n`) — which is the runtime *consumption* layer (the app using an auth token, the app rendering translated text), not the admin-facing management UI those contexts actually need (role assignment, translation-key editing, category rule config, recommendation review/approval). Same distinction as Shared Kernel vs. owning-context on the backend, applied to the frontend.

---

## 2. Full backend tree

```
UTOP.sln
├── src/
│   ├── UTOP.Shared/
│   │   ├── Domain/
│   │   │   ├── Events/                     (base domain event classes)
│   │   │   ├── Exceptions/                 (base domain exceptions)
│   │   │   └── ValueObjects/
│   │   │       ├── Money.cs
│   │   │       ├── Location.cs
│   │   │       ├── LocationType.cs
│   │   │       ├── CorrelationId.cs
│   │   │       ├── PassengerCount.cs
│   │   │       ├── GeoCoordinate.cs
│   │   │       ├── DailyPrayerSchedule.cs
│   │   │       ├── PrayerWindow.cs
│   │   │       ├── Prayer.cs
│   │   │       ├── LocalizedTime.cs
│   │   │       └── Currency.cs
│   │   ├── Time/
│   │   │   ├── IClock.cs
│   │   │   ├── SystemClock.cs
│   │   │   ├── FakeClock.cs
│   │   │   ├── IBusinessCalendar.cs
│   │   │   └── CalendarContext.cs
│   │   └── Infrastructure/
│   │       ├── Logging/                    (structured logging setup)
│   │       ├── Messaging/                  (RabbitMQ base classes)
│   │       └── Security/                   (JWT, encryption utilities)
│   │
│   ├── UTOP.Booking/                       (full DDD — see §2.1 template)
│   ├── UTOP.Accommodation/                 (full DDD, same template)
│   ├── UTOP.ResourceAllocation/            (full DDD, same template)
│   ├── UTOP.Pilgrimage/                    (full DDD, same template)
│   ├── UTOP.GroupManagement/               (full DDD, same template)
│   ├── UTOP.CostSplitting/                 (full DDD, same template)
│   ├── UTOP.Notifications/                 (full DDD, same template)
│   ├── UTOP.KnowledgeBase/                 (full DDD, same template)
│   ├── UTOP.AIRecommendation/              (full DDD, same template)
│   ├── UTOP.Identity/                      (full DDD, same template)
│   ├── UTOP.Localization/                  (full DDD, same template)
│   │
│   ├── UTOP.TravelCategory/                (lightweight module — see §2.2)
│   ├── UTOP.Analytics/                     (projection-host — see §2.3)
│   │
│   └── UTOP.API/                           (entry point — API host)
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Middleware/
│           ├── AuthMiddleware.cs
│           ├── CorrelationIdMiddleware.cs
│           ├── LocalizationMiddleware.cs
│           └── ErrorHandlingMiddleware.cs
│
└── tests/
    ├── UTOP.Shared.Tests/
    ├── UTOP.Booking.Tests/
    ├── UTOP.Accommodation.Tests/
    ├── UTOP.ResourceAllocation.Tests/
    ├── UTOP.Pilgrimage.Tests/
    ├── UTOP.GroupManagement.Tests/
    ├── UTOP.CostSplitting.Tests/
    ├── UTOP.Notifications.Tests/
    ├── UTOP.KnowledgeBase.Tests/
    ├── UTOP.AIRecommendation.Tests/
    ├── UTOP.Identity.Tests/
    ├── UTOP.Localization.Tests/
    ├── UTOP.TravelCategory.Tests/
    ├── UTOP.Analytics.Tests/
    └── UTOP.Integration.Tests/              (cross-context event flow, saga tests)
```

### 2.1 Full DDD context template (applies to the 11 listed above)

```
UTOP.<ContextName>/
├── Domain/
│   ├── Aggregates/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/                  (domain events, not integration events)
│   ├── Services/                (domain services)
│   └── Repositories/            (interfaces only — IBookingRepository etc.)
├── Application/
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   ├── Sagas/                   (only where the context actually orchestrates one)
│   └── Interfaces/              (ports — e.g. IBookingProvider)
├── Infrastructure/
│   ├── Persistence/
│   │   ├── <Context>DbContext.cs
│   │   ├── <Context>Repository.cs
│   │   └── Migrations/
│   └── ExternalServices/
│       ├── Stubs/
│       └── Adapters/
└── API/
    ├── Controllers/
    └── Mapping/
```

### 2.2 TravelCategory — lightweight module

No aggregate, no lifecycle, no saga. It's rule/config data with a validation service in front of it — the full DDD scaffold would be ceremony over substance here.

```
UTOP.TravelCategory/
├── Domain/
│   ├── ValueObjects/            (CategoryRule, ConstraintDefinition)
│   └── Services/                (CategoryValidationService)
├── Application/
│   ├── Queries/                 (GetCategoryRulesQuery)
│   └── Handlers/
├── Infrastructure/
│   └── Persistence/             (CategoryRuleRepository, Migrations/)
└── API/
    └── Controllers/             (CategoryController)
```

### 2.3 Analytics — projection-host

Per ARCH-008: no aggregate state, no writes, no source-of-truth role, consumes all integration events, publishes none. The folder shape below makes writing domain logic here structurally awkward on purpose — there's no `Domain/Aggregates/` for someone to reach for.

```
UTOP.Analytics/
├── Consumers/                   (one consumer per source context, or grouped)
├── Projections/
│   ├── ReadModels/              (projected DTOs/records, not entities)
│   ├── ProjectionHandlers/      (event → read model update)
│   └── Rebuild/                 (rebuild-from-stream — ARCH-008 rebuild-safety requirement)
├── Infrastructure/
│   └── Persistence/             (read-model store)
└── API/
    └── Controllers/             (dashboard/report queries — read-only)
```

---

## 3. Full frontend tree

Role-based layout/routing shell stays as designed in the old HLD — that part wasn't stale, it's genuinely role-driven, not context-driven, so it doesn't need a 1:1 context mapping. The fix is entirely in `features/`.

```
frontend/
├── src/
│   ├── app/
│   │   ├── layouts/
│   │   │   ├── OperatorLayout.tsx        (Operator-specific navigation and workspace)
│   │   │   ├── ManagerLayout.tsx         (Manager-specific dashboard and controls)
│   │   │   ├── AnalystLayout.tsx         (Analytics-focused layout)
│   │   │   ├── AdminLayout.tsx           (Admin configuration layout)
│   │   │   └── IntegrationLayout.tsx     (Integration engineer tools)
│   │   └── routes/
│   │       ├── OperatorRoutes.tsx
│   │       ├── ManagerRoutes.tsx
│   │       ├── AnalystRoutes.tsx
│   │       ├── AdminRoutes.tsx
│   │       └── IntegrationRoutes.tsx
│   │
│   ├── features/                         (one folder per backend context — 13, no exceptions)
│   │   ├── booking/
│   │   │   ├── components/
│   │   │   ├── pages/
│   │   │   └── services/
│   │   ├── accommodation/                (same shape)
│   │   ├── resource-allocation/          (same shape)
│   │   ├── pilgrimage/                   (same shape)
│   │   ├── group-management/             (same shape)
│   │   ├── cost-splitting/               (same shape)
│   │   ├── notifications/                (same shape)
│   │   ├── knowledge-base/                (same shape)
│   │   ├── analytics/                     (same shape — dashboards/read-only views, matches backend's projection-host nature)
│   │   ├── ai-recommendation/              (NEW — recommendation review/approval UI for managers)
│   │   ├── identity/                       (NEW — user/role management admin screens; distinct from shared/auth below)
│   │   ├── localization/                   (NEW — translation-key and locale-definition admin screens; distinct from shared/i18n below)
│   │   └── travel-category/                (NEW — category rule/constraint config admin screens)
│   │
│   ├── shared/
│   │   ├── components/                    (reusable UI components)
│   │   ├── hooks/                         (custom React hooks)
│   │   ├── services/                      (API client services)
│   │   ├── i18n/                          (runtime translation loading/consumption — NOT translation-key management, that's features/localization/)
│   │   ├── auth/                          (auth context, route guards — NOT user/role management, that's features/identity/)
│   │   └── types/                         (TypeScript type definitions)
│   │
│   └── main.tsx
```

No structural distinction needed between "full," "lightweight," and "projection-host" feature folders the way there was on the backend — every feature folder here is already the same flat `components/pages/services` shape, which is naturally lightweight. TravelCategory's frontend slice doesn't need special-casing; the backend did because DDD layering has ceremony to strip out, and the frontend never had that ceremony to begin with.

---

## 4. Open items (not yet resolved — do not scaffold these parts yet)

| Item | Status | Blocks |
|---|---|---|
| `UTOP.API` per-context route/module wiring detail | Deferred — depends on per-context LLDs as they're produced | Not a blocker for initial scaffold |
| Test project shape for `UTOP.Integration.Tests` (which sagas, which event flows) | Deferred to LLD-per-context as they land | Not a blocker for initial scaffold |
| Frontend test structure (Jest/RTL/Playwright — none specified yet) | Not yet reviewed | Not a blocker for initial scaffold |
| Backend `src/`+`tests/` vs `frontend/` folder asymmetry | Deferred — keep as-is until first vertical slice (Booking aggregate) is implemented and proven | Restructure to `backend/src`, `backend/tests` afterward; must also update any docker-compose paths, CI config, and this document's own tree diagrams at that time |

---

## 5. Provenance

Built across this session from: UTOP-ARCH-001 (`01_architecture_decisions.md`, ADR-001), UTOP-ARCH-008 (context ownership matrix, pasted directly), UTOP-ARCH-010 (`10-shared-kernel-governance.md` §5–6), and `02_high_level_design.md` §2.1 and §3.1 (superseded baselines). Decisions on TravelCategory and Analytics backend structure, and on frontend feature-folder mapping, made by Architect role under user delegation ("choose what is best") on 2026-07-15.
