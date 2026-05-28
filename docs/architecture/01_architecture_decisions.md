# Architecture Decision Records (ADRs)
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0  
**Status:** LOCKED — Ready for Implementation  
**Phase:** Phase 3 — System Architecture & Design  
**Classification:** Project Internal — Binding Architectural Specification  

---

## ADR Index

| ADR # | Title | Status | Date |
|-------|-------|--------|------|
| ADR-001 | Bounded Context Decomposition | Accepted | Phase 3 |
| ADR-002 | Service Architecture Pattern | Accepted | Phase 3 |
| ADR-003 | Data Ownership and Consistency Model | Accepted | Phase 3 |
| ADR-004 | Orchestration Pattern (Saga + Event-Driven) | Accepted | Phase 3 |
| ADR-005 | Persistence and Caching Strategy | Accepted | Phase 3 |
| ADR-006 | Adapter and Integration Strategy | Accepted | Phase 3 |
| ADR-007 | Observability and Logging Architecture | Accepted | Phase 3 |
| ADR-008 | Security Architecture | Accepted | Phase 3 |

---

## ADR-001: Bounded Context Decomposition

### Status
Accepted

### Context
UTOP's SRS defines 12 functional areas with complex cross-domain interactions (pilgrimage workflows affecting resource allocation, cost splitting affecting notifications, AI/ML affecting pricing decisions). Without explicit bounded context decomposition, the system risks:
- God services owning too many responsibilities
- Shared mutable state causing consistency problems
- Tight coupling preventing independent evolution of modules
- Orchestration spaghetti when workflows span multiple domains

The SRS review feedback explicitly identified bounded context decomposition as the most critical Phase 3 deliverable.

### Options Considered

**Option A: Single Monolith**
- All domains in one service
- Simple deployment, simple debugging
- Problem: UTOP's complexity (12 FRs, orchestration-heavy, multi-role) will create an unmaintainable monolith within weeks of implementation

**Option B: Microservices (one per FR)**
- 12+ independent services
- Maximum isolation
- Problem: Over-engineering for a solo development context; operational complexity far exceeds benefit; no team to manage distributed deployment

**Option C: Modular Monolith with Bounded Contexts (Selected)**
- Single deployable unit (initially)
- Clear internal module boundaries enforced via namespaces and interfaces
- Each bounded context is an independent module with its own domain model
- Modules communicate via well-defined interfaces (not direct object references)
- Future: Each module can be extracted to a separate service without changing its internal logic

### Decision
**Adopt Modular Monolith with clear Bounded Contexts.**

Bounded contexts are defined as follows:

| Bounded Context | Responsibility | Key Aggregates |
|-----------------|---------------|----------------|
| **Booking** | Multi-modal travel booking lifecycle | Booking, Itinerary, Passenger |
| **Accommodation** | Hotel and ancillary service coordination | Accommodation, Room, AncillaryService |
| **ResourceAllocation** | Vehicle, staff, and resource assignment | Resource, AllocationPolicy, AllocationDecision |
| **TravelCategory** | Category-specific rules and adaptations | TravelCategory, CategoryRule, CategoryConstraint |
| **Pilgrimage** | Religious travel compliance and coordination | PilgrimageGroup, PrayerSchedule, SacredSite |
| **GroupManagement** | Group creation, membership, coordination | Group, GroupMember, GroupItinerary |
| **CostSplitting** | Fair cost calculation and payment tracking | CostLedger, CostShare, PaymentStatus |
| **Notifications** | Multi-channel notification delivery | Notification, NotificationTemplate, DeliveryStatus |
| **KnowledgeBase** | Micro-learning and operational guidance | KnowledgeModule, LearningPath, CompletionRecord |
| **Analytics** | Reporting pipelines and dashboard data | Report, Dashboard, DataAggregation |
| **AIRecommendation** | ML-driven decision support | Recommendation, ModelOutput, DecisionLog |
| **Identity** | User management and RBAC | User, Role, Permission, Session |
| **Localization** | i18n, language, and regional adaptation | Locale, Translation, RegionalRule |

### Consequences
- **Positive:** Clear ownership, independent evolution, no shared state between contexts
- **Positive:** Each context has its own domain model (no anemic shared entities)
- **Positive:** Future microservices extraction is clean (boundaries already enforced)
- **Negative:** Requires discipline to enforce boundaries (no cross-context direct calls)
- **Negative:** Cross-context queries (e.g., analytics across booking and resource) require event aggregation

### Enforcement Rules
- **No direct references** between bounded context domain models
- **Cross-context communication** via published domain events (RabbitMQ) or service interfaces
- **No shared database tables** between contexts (each context owns its schema)
- **Each context** has its own folder/namespace: `UTOP.[ContextName]`

---

## ADR-002: Service Architecture Pattern

### Status
Accepted

### Context
Having defined bounded contexts, we need to decide how to structure each context internally. The SRS requires SOLID principles, clean separation, and extensibility. The SRS review identified the risk of "controller-heavy monoliths" and "database-centric logic."

### Options Considered

**Option A: Layered Architecture (Traditional N-Tier)**
- Presentation → Business Logic → Data Access
- Simple, well-understood
- Problem: Business logic bleeds into controllers; database schema drives domain model (anemic domain)

**Option B: CQRS (Command Query Responsibility Segregation)**
- Separate read and write models
- Excellent for analytics/reporting separation
- Problem: Adds complexity for simple operations; overkill for some bounded contexts

**Option C: Clean Architecture with DDD (Selected)**
- Domain layer at center (no external dependencies)
- Application layer (use cases, commands, queries)
- Infrastructure layer (persistence, messaging, external services)
- API layer (controllers, DTOs, mapping)
- Domain model is the authority; database adapts to domain (not vice versa)

### Decision
**Adopt Clean Architecture with DDD principles for all bounded contexts.**

```
UTOP.[Context]/
├── Domain/
│   ├── Aggregates/          (Aggregate roots)
│   ├── Entities/            (Domain entities)
│   ├── ValueObjects/        (Immutable value types)
│   ├── Events/              (Domain events)
│   ├── Services/            (Domain services)
│   ├── Repositories/        (Repository interfaces — no implementation)
│   └── Exceptions/          (Domain-specific exceptions)
├── Application/
│   ├── Commands/            (Write operations — CQRS commands)
│   ├── Queries/             (Read operations — CQRS queries)
│   ├── Handlers/            (Command and query handlers)
│   ├── DTOs/                (Data transfer objects)
│   └── Interfaces/          (Application service interfaces)
├── Infrastructure/
│   ├── Persistence/         (EF Core DbContext, Repository implementations)
│   ├── Messaging/           (RabbitMQ publishers and consumers)
│   ├── ExternalServices/    (Stub implementations + adapter interfaces)
│   └── Logging/             (Structured logging implementation)
└── API/
    ├── Controllers/         (ASP.NET Core controllers)
    ├── Middleware/           (Auth, logging, error handling)
    └── Mapping/             (Domain ↔ DTO mapping)
```

**CQRS applied selectively:**
- Commands (write operations): CreateBooking, ConfirmAllocation, SplitCosts, etc.
- Queries (read operations): GetBookingDetails, GetAnalyticsDashboard, GetAuditTrail, etc.
- Analytics context uses read-optimized projections (separate read models)

### Consequences
- **Positive:** Domain model is pure (no EF Core attributes in domain entities)
- **Positive:** Business rules enforced in domain layer (not scattered in controllers)
- **Positive:** Infrastructure is replaceable (EF Core → Dapper → anything)
- **Positive:** Testable without database (domain logic has no infrastructure dependencies)
- **Negative:** More files and folders per context; requires discipline

### Dependency Rule
**Dependencies point inward only:**
```
API → Application → Domain ← Infrastructure
```
Domain has zero external dependencies. Infrastructure depends on Domain (implements its interfaces). Application depends on Domain. API depends on Application.

---

## ADR-003: Data Ownership and Consistency Model

### Status
Accepted

### Context
The SRS review identified missing consistency definitions as a critical gap. UTOP workflows span multiple bounded contexts (booking confirmation triggers resource allocation triggers notification triggers cost recalculation). Defining consistency boundaries prevents data corruption and race conditions.

### Options Considered

**Option A: Strong Consistency Everywhere**
- All cross-context operations in a single database transaction
- Simple to reason about
- Problem: Violates bounded context independence; creates distributed transaction problems; tight coupling between contexts

**Option B: Eventual Consistency Everywhere**
- All cross-context operations via events; each context eventually consistent
- Maximum decoupling
- Problem: Complex to implement; difficult to reason about for all operations; some operations genuinely require strong consistency (e.g., payment confirmation)

**Option C: Hybrid — Strong Within Context, Eventual Across Contexts (Selected)**
- Within a bounded context: Strong consistency (single transaction, single DB)
- Across bounded contexts: Eventual consistency via domain events (RabbitMQ)
- Sagas for complex cross-context workflows with compensation

### Decision
**Adopt hybrid consistency model.**

#### Strong Consistency (Within Context)
All operations within a single bounded context execute in a single database transaction:
- Creating a booking + recording the booking event → same transaction
- Updating cost shares + recording the ledger entry → same transaction
- Allocating a resource + logging the decision → same transaction

#### Eventual Consistency (Across Contexts)
Cross-context operations via published domain events:

```
BookingContext publishes → BookingConfirmed event
ResourceAllocationContext consumes → allocates resource, publishes ResourceAllocated
NotificationContext consumes → sends confirmation notification
CostSplittingContext consumes → triggers cost recalculation (if group)
AnalyticsContext consumes → records booking metric
```

#### Saga Pattern for Distributed Workflows
Complex multi-step workflows use sagas with compensating transactions:

**Booking Saga:**
```
Step 1: CreateBooking (Booking Context)
Step 2: ValidateAvailability (stub call)
Step 3: CalculatePrice (Booking Context)
Step 4: ReserveCapacity (ResourceAllocation Context)
Step 5: ConfirmBooking (Booking Context)
Step 6: PublishBookingConfirmed (→ triggers Notification, Analytics, CostSplitting)

Compensations:
- If Step 4 fails → ReleaseReservation (Step 3 reverse)
- If Step 3 fails → CancelPriceCalculation
- If Step 2 fails → ReturnUnavailableResponse
```

#### Data Ownership Rules

| Data | Owned By | Accessed By |
|------|----------|-------------|
| Booking details | Booking Context | Analytics (via events), Notification (via events) |
| Resource inventory | ResourceAllocation Context | Booking (via service interface) |
| Group membership | GroupManagement Context | CostSplitting (via events) |
| Cost ledger | CostSplitting Context | Analytics (via events) |
| Prayer schedules | Pilgrimage Context | Booking (via service interface for validation) |
| Translations | Localization Context | All contexts (via localization service) |
| Audit logs | Logging/Observability | All contexts (write only; read by admin/analyst) |
| User/role data | Identity Context | All contexts (read-only via auth middleware) |

### Consequences
- **Positive:** Each context's data is protected from unauthorized writes
- **Positive:** Strong consistency where it matters (financial operations)
- **Positive:** Eventual consistency keeps contexts decoupled
- **Negative:** Event-driven eventual consistency requires idempotent consumers (events may be delivered more than once)
- **Mitigation:** All event consumers implement idempotency checks (event ID deduplication)

---

## ADR-004: Orchestration Pattern

### Status
Accepted

### Context
UTOP has complex multi-step workflows (pilgrimage booking, group cost splitting, resource allocation under conflict). These workflows span multiple bounded contexts and must be reliable, observable, and recoverable from partial failures. The SRS review identified missing transaction semantics as a critical gap.

### Options Considered

**Option A: Choreography Only**
- Each service reacts to events independently
- Maximum decoupling
- Problem: No central visibility of workflow state; difficult to debug; "where did my booking go?" is unanswerable

**Option B: Orchestration Only (Central Saga Orchestrator)**
- One orchestrator controls all workflow steps
- Clear visibility
- Problem: Orchestrator becomes a God service; tight coupling to all contexts

**Option C: Hybrid — Choreography for Simple Flows, Orchestration for Complex Sagas (Selected)**
- Simple event flows (booking confirmed → notify): choreography
- Complex multi-step workflows (pilgrimage booking, resource conflict resolution): orchestrated saga

### Decision
**Adopt hybrid orchestration pattern.**

#### Simple Event Flows (Choreography)
```
BookingConfirmed → [NotificationContext handles]
                 → [AnalyticsContext handles]
                 → [CostSplittingContext handles if group]
```
No central orchestrator. Each context subscribes to events and reacts independently.

#### Complex Sagas (Orchestrated)
Implemented as Saga classes in the Application layer of the owning context:

**PilgrimageSaga:**
```
1. ValidatePilgrimageType
2. CheckPrayerScheduleCompliance
3. ValidateSacredSiteAccess
4. AssignPilgrimageGuide
5. BookMultiLegJourney (calls Booking context)
6. BookAccommodationNearSite (calls Accommodation context)
7. EnforceGroupCohesion
8. PublishPilgrimageConfirmed

Compensations:
- Guide unavailable → suggest alternatives, escalate to manager
- Sacred site closed → return conflict with alternatives
- Accommodation unavailable → return options, await operator selection
```

**GroupCostSplittingSaga:**
```
1. CalculateBaseShares
2. ApplyDiscounts
3. HandleLateMemberJoin (if applicable)
4. RecalculateAllShares
5. NotifyAllMembers (cost breakdown)
6. TrackPaymentStatus
7. HandleRefundsIfMemberLeaves
8. PublishCostSettlementComplete
```

**ResourceAllocationSaga:**
```
1. IdentifyResourceRequirements
2. ApplyPriorityStrategy
3. CheckResourceAvailability
4. DetectConflicts (if any)
5. ResolveConflictsOrEscalate
6. AllocateResource
7. PublishResourceAllocated
8. LogAllocationDecision (with rationale)
```

#### Idempotency
All saga steps implement idempotency:
- Each step has a unique idempotency key (saga ID + step name)
- Duplicate execution of the same step is detected and skipped
- Idempotency keys stored in database (saga_execution_log table)

#### Saga State Persistence
Saga state persisted to database at every step:
- Saga ID, current step, input, output, status (Running/Completed/Failed/Compensating)
- Enables restart from failure point (no re-execution from beginning)

### Consequences
- **Positive:** Complex workflows are observable (saga state in database)
- **Positive:** Partial failure recovery (restart from failed step, not beginning)
- **Positive:** Simple flows remain simple (choreography)
- **Negative:** Saga implementation is non-trivial (requires discipline)
- **Mitigation:** Use MassTransit saga state machine library for .NET (mature, well-tested)

---

## ADR-005: Persistence and Caching Strategy

### Status
Accepted

### Context
UTOP has diverse data access patterns:
- **Transactional:** Booking creation, payment processing, resource allocation (ACID required)
- **Analytical:** Dashboard queries, report generation (read-heavy, aggregate-friendly)
- **Audit:** Log queries (append-only, immutable, queryable)
- **Cache:** Session data, frequently-read reference data (low latency required)

A single persistence strategy cannot optimally serve all patterns.

### Decision
**Adopt polyglot persistence with PostgreSQL as primary, Redis for caching.**

#### PostgreSQL (Primary)
- All bounded context operational data
- All audit logs (immutable append-only tables)
- Saga state persistence
- Translation strings
- Schema per bounded context (schema isolation):
  ```
  utop_booking.*
  utop_accommodation.*
  utop_resource.*
  utop_pilgrimage.*
  utop_group.*
  utop_cost.*
  utop_notification.*
  utop_knowledge.*
  utop_identity.*
  utop_localization.*
  utop_analytics.*
  utop_ai.*
  utop_audit.*
  utop_saga.*
  ```
- JSONB columns for flexible schema (saga state, AI model outputs, category-specific rules)

#### Redis (Cache)
- **Session data:** User sessions (30-minute TTL, auto-expire)
- **Reference data:** Prayer times (1-hour TTL), exchange rates (15-minute TTL), localization strings (1-hour TTL)
- **Frequently read:** Active resource inventory snapshots (5-minute TTL)
- **Rate limiting:** API rate limit counters per user
- **Idempotency keys:** Short-lived deduplication keys (24-hour TTL)

#### Read Models for Analytics (PostgreSQL — Separate Schema)
Analytics uses materialized views and read-optimized projections:
- Updated via domain events (eventual consistency)
- Pre-aggregated for dashboard performance
- Separate from operational tables (no joins across operational and analytics schemas)

#### Entity Framework Core (ORM)
- Used for all operational data access
- Database-first schema migrations managed via EF Core Migrations
- No EF Core attributes in domain entities (Fluent API configuration only)
- Repository pattern wraps all data access (no DbContext in application layer)

#### Data Retention
- Operational data: Indefinite (soft-delete only)
- Audit logs: 24 months active + archive
- Session data: 30 minutes (Redis auto-expire)
- Analytics projections: Rolling 36 months

### Consequences
- **Positive:** PostgreSQL ACID compliance for transactional integrity
- **Positive:** Schema isolation prevents cross-context data access
- **Positive:** Redis eliminates repeated database reads for reference data
- **Negative:** Two data stores to operate and back up
- **Mitigation:** Docker Compose manages both; health checks verify both on startup

---

## ADR-006: Adapter and Integration Strategy

### Status
Accepted

### Context
UTOP's core value is extensibility — real integrations must plug in without touching core code. The SRS defines 8 external interface requirements (EIR1-EIR8), all initially stubbed. The adapter pattern must be rigorously enforced so the 5% adapter work doesn't require changes to the 95% core.

### Decision
**Adopt the Ports and Adapters (Hexagonal Architecture) pattern for all external integrations.**

#### Ports (Interfaces — defined in Domain or Application layer)
```csharp
// IBookingProvider.cs (in Application/Interfaces/)
public interface IBookingProvider
{
    Task<IEnumerable<JourneyOption>> SearchJourneysAsync(JourneySearchRequest request);
    Task<BookingConfirmation> ConfirmBookingAsync(BookingRequest request);
    Task<CancellationResult> CancelBookingAsync(string bookingReference);
}

// IPaymentGateway.cs
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<RefundResult> ProcessRefundAsync(RefundRequest request);
}

// INotificationService.cs
public interface INotificationService
{
    Task<DeliveryResult> SendEmailAsync(EmailNotification notification);
    Task<DeliveryResult> SendSmsAsync(SmsNotification notification);
    Task<DeliveryResult> SendPushAsync(PushNotification notification);
}

// IRecommendationEngine.cs
public interface IRecommendationEngine
{
    Task<PricingRecommendation> GetPricingRecommendationAsync(PricingContext context);
    Task<AllocationRecommendation> GetAllocationRecommendationAsync(AllocationContext context);
    Task<DemandForecast> GetDemandForecastAsync(ForecastContext context);
}

// IPrayerTimeProvider.cs
public interface IPrayerTimeProvider
{
    Task<PrayerSchedule> GetPrayerTimesAsync(Location location, DateOnly date, PrayerMethod method);
}

// IAccommodationProvider.cs
public interface IAccommodationProvider
{
    Task<IEnumerable<AccommodationOption>> SearchAccommodationsAsync(AccommodationSearchRequest request);
    Task<AccommodationConfirmation> ConfirmAccommodationAsync(AccommodationRequest request);
}
```

#### Stub Adapters (Initial Implementation)
```csharp
// StubBookingProvider.cs (in Infrastructure/ExternalServices/Stubs/)
public class StubBookingProvider : IBookingProvider
{
    private readonly ILogger<StubBookingProvider> _logger;

    public async Task<IEnumerable<JourneyOption>> SearchJourneysAsync(JourneySearchRequest request)
    {
        _logger.LogInformation("STUB: SearchJourneys called. Origin={Origin}, Destination={Dest}, Date={Date}",
            request.Origin, request.Destination, request.Date);

        // Deterministic stub data based on route
        return await Task.FromResult(StubData.GetJourneyOptions(request.Origin, request.Destination));
    }

    public async Task<BookingConfirmation> ConfirmBookingAsync(BookingRequest request)
    {
        _logger.LogInformation("STUB: ConfirmBooking called. Reference={Ref}", request.Reference);
        return await Task.FromResult(new BookingConfirmation
        {
            ConfirmationId = $"STUB-{Guid.NewGuid():N}",
            Status = ConfirmationStatus.Confirmed,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

#### Real Adapters (Future — Example Structure)
```csharp
// AmadeusBookingAdapter.cs (in Infrastructure/ExternalServices/Adapters/)
public class AmadeusBookingAdapter : IBookingProvider
{
    private readonly HttpClient _httpClient;
    private readonly AmadeusConfiguration _config;
    private readonly ILogger<AmadeusBookingAdapter> _logger;

    // Real implementation calling Amadeus API
    // Zero changes to domain or application layer
}
```

#### Dependency Injection Registration
```csharp
// Program.cs — swap stubs for real adapters via configuration
if (configuration["Integration:BookingProvider"] == "Amadeus")
    services.AddScoped<IBookingProvider, AmadeusBookingAdapter>();
else
    services.AddScoped<IBookingProvider, StubBookingProvider>();
```

#### Adapter Replacement Rules
1. **Core code never changes** when swapping adapters
2. **Adapters implement the interface** and nothing else
3. **Configuration drives which adapter is active** (not code changes)
4. **Stubs log all calls** for observability during testing
5. **Stubs return deterministic data** (not random) for repeatable tests

### Consequences
- **Positive:** Real integrations plug in via configuration — no core code changes
- **Positive:** Stubs are fully functional for testing and demonstration
- **Positive:** Multiple adapters can coexist (A/B testing different providers)
- **Negative:** Every external dependency requires an interface + stub implementation upfront
- **Mitigation:** This effort is front-loaded but pays off permanently

---

## ADR-007: Observability and Logging Architecture

### Status
Accepted

### Context
The SRS (FR8) mandates comprehensive logging for auditability, traceability, and debugging. The SRS review praised logging thinking but noted missing elements: domain events, distributed tracing propagation, replayability, and audit correlation.

### Decision
**Adopt structured, correlated, multi-layer observability.**

#### Log Levels and Usage
```
DEBUG   — Internal decision points, variable values (development only)
INFO    — Workflow steps, successful operations, external calls
WARN    — Recoverable issues, stub fallbacks, degraded behavior
ERROR   — Failed operations, exceptions, unhandled errors
CRITICAL — System-threatening conditions (data corruption, auth failure)
```

#### Structured Log Format (JSON)
```json
{
  "timestamp": "2026-01-15T09:17:43.123456Z",
  "level": "INFO",
  "correlation_id": "bk-2026-0115-a7k3x",
  "saga_id": "saga-booking-2026-0115-001",
  "user_id": "op-john-123",
  "user_role": "Operator",
  "bounded_context": "Booking",
  "action": "BookingConfirmed",
  "booking_id": "UTOP-BUS-20260115-A7K3X",
  "input": {
    "origin": "Delhi",
    "destination": "Agra",
    "mode": "Bus",
    "passengers": 2
  },
  "output": {
    "status": "Confirmed",
    "price": 2500.00,
    "currency": "INR"
  },
  "duration_ms": 142,
  "decision_rationale": "All validation passed; capacity available; price calculated at base rate",
  "external_calls": [
    { "service": "StubBookingProvider", "duration_ms": 12, "status": "Success" }
  ]
}
```

#### Correlation ID Strategy
- **Correlation ID** generated at API entry point (booking request, search, etc.)
- **Propagated** through all layers (middleware injects into logging context)
- **Included** in all log entries and domain events
- **Returned** in API responses (for client-side tracking)
- **Queryable** in audit trail (find all logs for one booking)

#### Audit Log (Immutable)
Separate from operational logs. Append-only table in PostgreSQL:
```sql
CREATE TABLE utop_audit.audit_log (
    id              BIGSERIAL PRIMARY KEY,
    timestamp       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    correlation_id  VARCHAR(100) NOT NULL,
    user_id         VARCHAR(100) NOT NULL,
    user_role       VARCHAR(50) NOT NULL,
    action          VARCHAR(200) NOT NULL,
    entity_type     VARCHAR(100),
    entity_id       VARCHAR(200),
    before_state    JSONB,
    after_state     JSONB,
    ip_address      INET,
    decision_log    TEXT
);

-- Prevent updates/deletes
CREATE RULE no_update AS ON UPDATE TO utop_audit.audit_log DO INSTEAD NOTHING;
CREATE RULE no_delete AS ON DELETE TO utop_audit.audit_log DO INSTEAD NOTHING;
```

#### Distributed Tracing
- **Trace ID** propagated across all saga steps
- **Span ID** per individual operation within a trace
- **ELK Stack** aggregates logs by trace ID for full workflow visibility
- **Kibana** provides log search, dashboard, and workflow trace visualization

#### Domain Event Logging
All published domain events logged:
```json
{
  "event_type": "BookingConfirmed",
  "event_id": "evt-2026-0115-789xyz",
  "correlation_id": "bk-2026-0115-a7k3x",
  "aggregate_id": "UTOP-BUS-20260115-A7K3X",
  "aggregate_type": "Booking",
  "published_at": "2026-01-15T09:17:43.250Z",
  "payload": { ... }
}
```

### Consequences
- **Positive:** Every operation is fully traceable from API call to database write
- **Positive:** Audit trail is immutable and tamper-evident
- **Positive:** Correlation IDs enable cross-context workflow debugging
- **Negative:** High log volume requires careful storage management
- **Mitigation:** Log retention policies (12 months active, 24 months archive)

---

## ADR-008: Security Architecture

### Status
Accepted

### Context
The SRS (NFR2, NFR9) mandates RBAC, encryption, audit trails, session management, and compliance (GDPR, DPDP). Security must be built in (not bolted on).

### Decision
**Adopt defence-in-depth security architecture.**

#### Authentication
- **JWT (JSON Web Tokens)** for API authentication
- JWT issued on login; contains user ID, role, locale, session ID
- Access token TTL: 30 minutes
- Refresh token TTL: 7 days (stored in HttpOnly cookie)
- Token signing: RS256 (asymmetric — private key signs, public key verifies)

#### Authorization (RBAC)
Enforced at API layer via middleware:

```
| Action                        | Operator | Manager | Analyst | Admin | IntegrationEngineer |
|-------------------------------|----------|---------|---------|-------|---------------------|
| Create/amend booking          | ✓        | ✓       | ✗       | ✗     | ✗                   |
| View own bookings             | ✓        | ✓       | ✓       | ✓     | ✗                   |
| Override resource allocation  | ✗        | ✓       | ✗       | ✗     | ✗                   |
| Generate analytics reports    | Limited  | ✓       | ✓       | ✓     | ✗                   |
| View audit trails             | ✗        | ✗       | ✓       | ✓     | ✗                   |
| Manage users/roles            | ✗        | ✗       | ✗       | ✓     | ✗                   |
| Configure integrations        | ✗        | ✗       | ✗       | ✓     | ✓                   |
| Manage knowledge base         | ✗        | ✗       | ✗       | ✓     | ✗                   |
| View AI recommendations       | ✗        | ✓       | ✓       | ✓     | ✗                   |
| Manage translations           | ✗        | ✗       | ✗       | ✓     | ✗                   |
```

#### Encryption
- **At rest:** AES-256 for sensitive data (PII, payment tokens)
- **In transit:** TLS 1.3 minimum for all API communication
- **Secrets:** Environment variables (never in source code); .NET Secret Manager for development
- **Passwords:** Bcrypt with work factor 12 (never stored plain text)

#### Data Classification
- **Public:** Journey search results, accommodation listings
- **Internal:** Booking details, resource allocation decisions
- **Confidential:** Passenger PII (name, passport, contact), payment tokens
- **Restricted:** Audit logs, security logs, admin configuration

#### PII Handling
- PII stored encrypted (AES-256) in database
- PII never logged (masked in log entries: `passenger: "J*** D***"`)
- PII access requires explicit authorization (Confidential clearance)
- Right to erasure: Soft-delete with PII overwrite (GDPR/DPDP compliance)

#### Session Security
- Session timeout: 30 minutes idle
- Concurrent session limit: 3 per user
- Session invalidation on logout (JWT blacklist in Redis)
- CSRF protection: SameSite=Strict cookies for session tokens

### Consequences
- **Positive:** Defence-in-depth; multiple layers prevent single-point breaches
- **Positive:** RBAC enforced at middleware level (not in business logic)
- **Positive:** PII protection built-in from start (not retrofitted)
- **Negative:** JWT blacklist in Redis requires Redis availability for logout
- **Mitigation:** Redis failure falls back to short token TTL (30 minutes maximum exposure)

---

## ADR Summary and Sign-Off

| ADR # | Decision | Rationale |
|-------|----------|-----------|
| ADR-001 | Modular Monolith with Bounded Contexts | Solo development; future microservices extraction without rework |
| ADR-002 | Clean Architecture with DDD | Domain model as authority; testable without infrastructure |
| ADR-003 | Hybrid Consistency (Strong within, Eventual across) | Correctness where needed; decoupling where possible |
| ADR-004 | Hybrid Orchestration (Choreography + Saga) | Simple flows stay simple; complex flows are observable |
| ADR-005 | PostgreSQL + Redis | ACID for transactions; Redis for low-latency reads |
| ADR-006 | Ports and Adapters | Stubs replaceable by real integrations without core changes |
| ADR-007 | Structured Correlated Logging + Immutable Audit | Full traceability; tamper-evident compliance |
| ADR-008 | Defence-in-Depth Security | RBAC + encryption + PII protection built-in |

**Status: ALL ADRs ACCEPTED — Ready for HLD**

---

**End of Architecture Decision Records**
