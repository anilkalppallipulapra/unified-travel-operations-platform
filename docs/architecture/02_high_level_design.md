# High-Level Design (HLD)
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0  
**Status:** LOCKED — Ready for LLD  
**Phase:** Phase 3 — System Architecture & Design  
**Classification:** Project Internal — Binding Architectural Specification  

---

## 1. System Architecture Overview

UTOP is a **Modular Monolith** with 13 bounded contexts, communicating internally via domain events (RabbitMQ) and service interfaces. All external integrations are abstracted via the Ports and Adapters pattern (stub implementations initially).

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        UTOP SYSTEM BOUNDARY                                  │
│                                                                               │
│  ┌──────────────┐    ┌──────────────────────────────────────────────────┐   │
│  │   Frontend   │    │              API Gateway Layer                    │   │
│  │  React/TS    │───▶│  ASP.NET Core — Auth Middleware — RBAC — CORS    │   │
│  │  5 Role UIs  │    └──────────────────────────────────────────────────┘   │
│  └──────────────┘                         │                                   │
│                                           ▼                                   │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │                    BOUNDED CONTEXT LAYER                                │  │
│  │                                                                          │  │
│  │  ┌──────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │  │
│  │  │ Booking  │  │Accommodation │  │  Resource    │  │  Pilgrimage  │  │  │
│  │  │ Context  │  │   Context    │  │ Allocation   │  │   Context    │  │  │
│  │  └────┬─────┘  └──────┬───────┘  │   Context    │  └──────┬───────┘  │  │
│  │       │               │          └──────┬───────┘         │           │  │
│  │  ┌────▼─────┐  ┌──────▼───────┐  ┌──────▼───────┐  ┌──────▼───────┐  │  │
│  │  │  Group   │  │ CostSplitting│  │ Notification │  │ KnowledgeBase│  │  │
│  │  │ Context  │  │   Context    │  │   Context    │  │   Context    │  │  │
│  │  └────┬─────┘  └──────┬───────┘  └──────┬───────┘  └──────────────┘  │  │
│  │       │               │                  │                              │  │
│  │  ┌────▼─────┐  ┌──────▼───────┐  ┌──────▼───────┐  ┌──────────────┐  │  │
│  │  │Analytics │  │  AIRecommend │  │   Identity   │  │ Localization │  │  │
│  │  │ Context  │  │   Context    │  │   Context    │  │   Context    │  │  │
│  │  └────┬─────┘  └──────────────┘  └──────────────┘  └──────────────┘  │  │
│  └───────┼────────────────────────────────────────────────────────────────┘  │
│           │                                                                    │
│           ▼                                                                    │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                    INFRASTRUCTURE LAYER                                  │ │
│  │                                                                           │ │
│  │  ┌────────────┐  ┌──────────┐  ┌──────────┐  ┌────────────────────┐   │ │
│  │  │ PostgreSQL │  │  Redis   │  │RabbitMQ  │  │    ELK Stack       │   │ │
│  │  │ (Primary   │  │ (Cache + │  │(Events + │  │ (Logs + Metrics +  │   │ │
│  │  │  + Audit)  │  │ Sessions)│  │  Sagas)  │  │    Dashboards)     │   │ │
│  │  └────────────┘  └──────────┘  └──────────┘  └────────────────────┘   │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                                │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                 EXTERNAL ADAPTER LAYER (Stub → Real)                     │ │
│  │                                                                           │ │
│  │  [BookingAPI] [AccommodationAPI] [PaymentGateway] [NotificationService]  │ │
│  │  [AIModels]   [PrayerTimeAPI]   [SMSProvider]    [EmailProvider]         │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Frontend Architecture

### 2.1 Role-Based UI Structure

```
frontend/
├── src/
│   ├── app/
│   │   ├── layouts/
│   │   │   ├── OperatorLayout.tsx       (Operator-specific navigation and workspace)
│   │   │   ├── ManagerLayout.tsx        (Manager-specific dashboard and controls)
│   │   │   ├── AnalystLayout.tsx        (Analytics-focused layout)
│   │   │   ├── AdminLayout.tsx          (Admin configuration layout)
│   │   │   └── IntegrationLayout.tsx    (Integration engineer tools)
│   │   └── routes/
│   │       ├── OperatorRoutes.tsx
│   │       ├── ManagerRoutes.tsx
│   │       ├── AnalystRoutes.tsx
│   │       ├── AdminRoutes.tsx
│   │       └── IntegrationRoutes.tsx
│   │
│   ├── features/                        (Feature-based organization)
│   │   ├── booking/
│   │   │   ├── components/
│   │   │   ├── pages/
│   │   │   └── services/
│   │   ├── accommodation/
│   │   ├── resource-allocation/
│   │   ├── pilgrimage/
│   │   ├── group-management/
│   │   ├── cost-splitting/
│   │   ├── notifications/
│   │   ├── knowledge-base/
│   │   ├── analytics/
│   │   └── admin/
│   │
│   ├── shared/
│   │   ├── components/                  (Reusable UI components)
│   │   ├── hooks/                       (Custom React hooks)
│   │   ├── services/                    (API client services)
│   │   ├── i18n/                        (Localization setup)
│   │   ├── auth/                        (Auth context, guards)
│   │   └── types/                       (TypeScript type definitions)
│   │
│   └── main.tsx
```

### 2.2 Role-Specific UI Flows

**Operator Interface:**
```
Login → Operator Dashboard
         ├── New Booking (FR1, FR3)
         │    ├── Select Mode → Search → Select → Validate → Price → Confirm
         │    └── Add Accommodation (FR2) → Select → Price → Confirm
         ├── Active Bookings (view, amend, cancel)
         ├── Pilgrimage Booking (FR11)
         │    └── Special workflow with prayer schedule compliance UI
         ├── Group Booking (FR12)
         │    └── Create group → Invite → Cost Split → Payment tracking
         └── Knowledge Base (FR5) — contextual help
```

**Manager Interface:**
```
Login → Manager Dashboard
         ├── Resource Allocation Overview (FR4)
         │    ├── Pending allocations
         │    ├── Conflicts requiring manual decision
         │    └── Override interface
         ├── AI Recommendations (FR7)
         │    ├── Pricing recommendations
         │    └── Allocation recommendations
         ├── Team Performance Overview
         └── Pilgrimage Compliance Check (FR11, UC9)
```

**Analyst Interface:**
```
Login → Analytics Dashboard (FR6)
         ├── Pre-built Reports (daily, weekly, monthly)
         ├── Ad-hoc Query Builder
         ├── Data Visualization (charts, tables)
         └── Export (CSV, PDF)
```

**Admin Interface:**
```
Login → Admin Panel
         ├── User Management (create, assign roles, deactivate)
         ├── Localization Management (FR10)
         │    └── Translation editor per language
         ├── Knowledge Base Management (FR5)
         │    └── Create/edit learning modules
         ├── System Configuration
         │    ├── Allocation policy rules
         │    ├── Notification templates
         │    └── Integration adapter configuration
         └── Audit Trail Inspection
```

---

## 3. Backend Architecture

### 3.1 Solution Structure (.NET 10)

```
UTOP.sln
├── src/
│   ├── UTOP.Shared/                     (Cross-cutting concerns)
│   │   ├── Domain/
│   │   │   ├── Events/                  (Base domain event classes)
│   │   │   ├── Exceptions/              (Base domain exceptions)
│   │   │   └── ValueObjects/            (Shared value objects: Money, DateRange, Location)
│   │   └── Infrastructure/
│   │       ├── Logging/                 (Structured logging setup)
│   │       ├── Messaging/               (RabbitMQ base classes)
│   │       └── Security/               (JWT, encryption utilities)
│   │
│   ├── UTOP.Booking/                    (Booking bounded context)
│   │   ├── Domain/
│   │   │   ├── Aggregates/
│   │   │   │   └── Booking.cs
│   │   │   ├── Entities/
│   │   │   │   ├── Itinerary.cs
│   │   │   │   └── Passenger.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── BookingId.cs
│   │   │   │   ├── JourneyRoute.cs
│   │   │   │   └── Price.cs
│   │   │   ├── Events/
│   │   │   │   ├── BookingCreated.cs
│   │   │   │   ├── BookingConfirmed.cs
│   │   │   │   ├── BookingCancelled.cs
│   │   │   │   └── BookingAmended.cs
│   │   │   ├── Services/
│   │   │   │   └── PricingService.cs
│   │   │   └── Repositories/
│   │   │       └── IBookingRepository.cs
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateBookingCommand.cs
│   │   │   │   ├── ConfirmBookingCommand.cs
│   │   │   │   ├── CancelBookingCommand.cs
│   │   │   │   └── AmendBookingCommand.cs
│   │   │   ├── Queries/
│   │   │   │   ├── GetBookingQuery.cs
│   │   │   │   └── SearchBookingsQuery.cs
│   │   │   ├── Handlers/
│   │   │   │   ├── CreateBookingHandler.cs
│   │   │   │   └── ConfirmBookingHandler.cs
│   │   │   ├── Sagas/
│   │   │   │   └── BookingSaga.cs
│   │   │   └── Interfaces/
│   │   │       └── IBookingProvider.cs  (Port — external booking API)
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/
│   │   │   │   ├── BookingDbContext.cs
│   │   │   │   ├── BookingRepository.cs
│   │   │   │   └── Migrations/
│   │   │   └── ExternalServices/
│   │   │       ├── Stubs/
│   │   │       │   └── StubBookingProvider.cs
│   │   │       └── Adapters/
│   │   │           └── (real adapters go here)
│   │   └── API/
│   │       ├── Controllers/
│   │       │   └── BookingController.cs
│   │       └── Mapping/
│   │           └── BookingMappingProfile.cs
│   │
│   ├── UTOP.Accommodation/              (same structure)
│   ├── UTOP.ResourceAllocation/         (same structure)
│   ├── UTOP.Pilgrimage/                 (same structure)
│   ├── UTOP.GroupManagement/            (same structure)
│   ├── UTOP.CostSplitting/              (same structure)
│   ├── UTOP.Notifications/              (same structure)
│   ├── UTOP.KnowledgeBase/              (same structure)
│   ├── UTOP.Analytics/                  (same structure)
│   ├── UTOP.AIRecommendation/           (same structure)
│   ├── UTOP.Identity/                   (same structure)
│   ├── UTOP.Localization/               (same structure)
│   └── UTOP.API/                        (Entry point — API host)
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
    ├── UTOP.Booking.Tests/
    ├── UTOP.Accommodation.Tests/
    └── (one test project per context)
```

---

## 4. Data Flow Architecture

### 4.1 Request Flow (Operator Creates Booking)

```
Operator (Browser)
    │
    │ POST /api/bookings
    │
    ▼
API Gateway (ASP.NET Core)
    │ AuthMiddleware: Validate JWT, extract role
    │ CorrelationIdMiddleware: Generate correlation ID
    │ LocalizationMiddleware: Set locale from user preference
    │
    ▼
BookingController
    │ Map HTTP request → CreateBookingCommand
    │
    ▼
CreateBookingHandler (Application Layer)
    │ Validate command
    │ Call StubBookingProvider.SearchJourneysAsync()
    │ Call PricingService.CalculatePrice()
    │ Create Booking aggregate
    │ Call IBookingRepository.SaveAsync()
    │   └── PostgreSQL write (utop_booking schema)
    │ Publish BookingCreated event → RabbitMQ
    │ Write structured log (correlation ID, all details)
    │ Write audit log (immutable)
    │
    ▼
RabbitMQ (Event Bus)
    │
    ├── ResourceAllocationContext subscribes → ResourceAllocationHandler
    │       └── Runs ResourceAllocationSaga
    │
    ├── NotificationContext subscribes → NotificationHandler
    │       └── Sends booking confirmation email/SMS (stub)
    │
    └── AnalyticsContext subscribes → AnalyticsProjectionHandler
            └── Updates analytics read models
    │
    ▼
BookingController returns HTTP 201 with:
    {
      "bookingId": "UTOP-BUS-20260115-A7K3X",
      "correlationId": "bk-2026-0115-a7k3x",
      "status": "Confirmed",
      "price": 2500.00,
      "currency": "INR"
    }
```

### 4.2 Cross-Context Event Flow

```
BOOKING CONFIRMED
    │
    ├──▶ RESOURCE ALLOCATION CONTEXT
    │         ResourceAllocationSaga:
    │         1. Identify resources needed
    │         2. Apply priority strategy
    │         3. Check availability
    │         4. Detect conflicts
    │         5. Allocate or escalate
    │         6. Publish ResourceAllocated
    │
    ├──▶ NOTIFICATION CONTEXT
    │         NotificationHandler:
    │         1. Load notification template
    │         2. Apply localization
    │         3. Call StubNotificationService
    │         4. Log delivery status
    │
    ├──▶ ANALYTICS CONTEXT
    │         AnalyticsProjectionHandler:
    │         1. Update booking count metric
    │         2. Update revenue metric
    │         3. Update destination popularity
    │         4. Update operator performance metric
    │
    └──▶ COST SPLITTING CONTEXT (if group booking)
              GroupCostSplittingSaga:
              1. Identify group members
              2. Calculate fair shares
              3. Notify all members
              4. Track payment status
```

### 4.3 Pilgrimage Booking Flow

```
Operator: Create Pilgrimage Booking
    │
    ▼
PilgrimageController → CreatePilgrimageCommand
    │
    ▼
PilgrimageSaga:
    │
    ├── Step 1: ValidatePilgrimageType
    │           └── Check: religion, type, dates valid
    │
    ├── Step 2: CheckPrayerScheduleCompliance
    │           └── Call IPrayerTimeProvider (stub)
    │           └── Validate transport schedule vs prayer times
    │           └── If conflict → suggest alternative times
    │
    ├── Step 3: ValidateSacredSiteAccess
    │           └── Check sacred site hours in database
    │           └── Validate group eligibility (religion-specific access)
    │
    ├── Step 4: AssignPilgrimageGuide
    │           └── Query available guides (language, certification, dates)
    │           └── Reserve guide (tentative)
    │
    ├── Step 5: BookMultiLegJourney
    │           └── Leg 1: Origin → Gateway (BookingContext)
    │           └── Leg 2: Gateway → Sacred Site (BookingContext)
    │           └── Leg 3: Sacred Site → Origin (BookingContext)
    │
    ├── Step 6: BookAccommodationNearSite
    │           └── Search near sacred site (AccommodationContext)
    │           └── Validate prayer facility proximity
    │           └── Book accommodation
    │
    ├── Step 7: EnforceGroupCohesion
    │           └── Register group (GroupManagement)
    │           └── Mark all legs as group-coordinated
    │
    └── Step 8: PublishPilgrimageConfirmed
                └── Triggers: Notifications, Analytics, CostSplitting
```

---

## 5. API Design

### 5.1 API Structure

Base URL: `/api/v1/`

All endpoints:
- Require Authorization header: `Bearer {jwt_token}`
- Return `X-Correlation-Id` header
- Accept `Accept-Language` header (locale)
- Return standard error envelope on failure

### 5.2 Core API Endpoints

#### Booking Context
```
POST   /api/v1/bookings/search              Search for journey options
POST   /api/v1/bookings                     Create a new booking
GET    /api/v1/bookings/{bookingId}         Get booking details
PUT    /api/v1/bookings/{bookingId}         Amend a booking
DELETE /api/v1/bookings/{bookingId}         Cancel a booking
GET    /api/v1/bookings/{bookingId}/audit   Get booking audit trail
```

#### Accommodation Context
```
POST   /api/v1/accommodations/search        Search for accommodation options
POST   /api/v1/accommodations               Book accommodation
GET    /api/v1/accommodations/{id}          Get accommodation details
PUT    /api/v1/accommodations/{id}          Amend accommodation booking
DELETE /api/v1/accommodations/{id}          Cancel accommodation booking
```

#### Resource Allocation Context
```
GET    /api/v1/resources                    List all resources with availability
GET    /api/v1/allocations                  List all current allocations
POST   /api/v1/allocations/override         Manager override allocation
GET    /api/v1/allocations/{bookingId}      Get allocation for booking
```

#### Pilgrimage Context
```
POST   /api/v1/pilgrimages                  Create pilgrimage booking
GET    /api/v1/pilgrimages/{id}             Get pilgrimage details
POST   /api/v1/pilgrimages/{id}/compliance  Run compliance check
GET    /api/v1/pilgrimages/{id}/schedule    Get full itinerary with prayer times
```

#### Group & Cost Splitting Context
```
POST   /api/v1/groups                       Create group
POST   /api/v1/groups/{id}/members          Add member to group
DELETE /api/v1/groups/{id}/members/{userId} Remove member from group
GET    /api/v1/groups/{id}/costs            Get cost breakdown
POST   /api/v1/groups/{id}/costs/recalculate Recalculate costs
GET    /api/v1/groups/{id}/payments         Get payment status
```

#### Analytics Context
```
GET    /api/v1/analytics/reports            List available reports
POST   /api/v1/analytics/reports/generate   Generate report with parameters
GET    /api/v1/analytics/dashboard          Get dashboard data
POST   /api/v1/analytics/export             Export report (CSV/PDF)
```

#### AI Recommendation Context
```
POST   /api/v1/recommendations/pricing      Get pricing recommendation
POST   /api/v1/recommendations/allocation   Get allocation recommendation
POST   /api/v1/recommendations/forecast     Get demand forecast
```

#### Identity Context
```
POST   /api/v1/auth/login                   Authenticate user
POST   /api/v1/auth/refresh                 Refresh access token
POST   /api/v1/auth/logout                  Invalidate session
GET    /api/v1/users                        List users (Admin only)
POST   /api/v1/users                        Create user (Admin only)
PUT    /api/v1/users/{id}/role              Assign role (Admin only)
```

#### Localization Context
```
GET    /api/v1/locales                      List supported locales
GET    /api/v1/translations/{locale}        Get all translations for locale
PUT    /api/v1/translations/{locale}/{key}  Update translation (Admin only)
```

#### Knowledge Base Context
```
GET    /api/v1/knowledge                    List knowledge modules
GET    /api/v1/knowledge/{id}               Get module content
POST   /api/v1/knowledge/{id}/complete      Mark module as completed
GET    /api/v1/knowledge/contextual         Get contextual help for current action
```

#### Notifications Context
```
GET    /api/v1/notifications                Get user notifications
PUT    /api/v1/notifications/{id}/read      Mark notification as read
PUT    /api/v1/notifications/preferences    Update notification preferences
```

### 5.3 Standard Response Envelope

**Success:**
```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "correlationId": "bk-2026-0115-a7k3x",
    "timestamp": "2026-01-15T09:17:43Z",
    "locale": "en-US"
  }
}
```

**Error:**
```json
{
  "success": false,
  "error": {
    "code": "BOOKING_UNAVAILABLE",
    "message": "No seats available on the selected route",
    "details": "Comfort Coach 9:00 AM Delhi-Agra on Jan 15 is fully booked",
    "suggestions": ["Try 11:00 AM departure", "Try AC Sleeper class"],
    "correlationId": "bk-2026-0115-a7k3x"
  }
}
```

---

## 6. Event Architecture

### 6.1 Domain Events Catalog

| Event | Publisher | Subscribers | Payload Key Fields |
|-------|-----------|-------------|-------------------|
| BookingCreated | Booking | Analytics, Notifications | bookingId, mode, route, price |
| BookingConfirmed | Booking | ResourceAllocation, Notifications, Analytics, CostSplitting | bookingId, passengers, category |
| BookingCancelled | Booking | ResourceAllocation, Notifications, Analytics, CostSplitting | bookingId, cancellationReason, refundAmount |
| BookingAmended | Booking | ResourceAllocation, Notifications | bookingId, amendments |
| ResourceAllocated | ResourceAllocation | Notifications, Analytics | bookingId, resourceId, allocationStrategy |
| ResourceConflictDetected | ResourceAllocation | Notifications (manager) | conflictingBookings, availableResources |
| AllocationOverridden | ResourceAllocation | Analytics, Audit | bookingId, managerId, originalAllocation, newAllocation |
| AccommodationBooked | Accommodation | Notifications, Analytics | accommodationId, bookingId, checkIn, checkOut |
| PilgrimageConfirmed | Pilgrimage | Notifications, Analytics, GroupManagement | pilgrimageId, sacredSites, guideId |
| PilgrimageComplianceChecked | Pilgrimage | Notifications (manager) | pilgrimageId, passed, violations |
| GroupCreated | GroupManagement | Notifications | groupId, coordinatorId, travelDates |
| GroupMemberJoined | GroupManagement | CostSplitting, Notifications | groupId, memberId |
| GroupMemberLeft | GroupManagement | CostSplitting, Notifications | groupId, memberId, refundDue |
| CostShareCalculated | CostSplitting | Notifications | groupId, perMemberCosts, totalCost |
| CostShareRecalculated | CostSplitting | Notifications | groupId, recalculationReason, updatedShares |
| PaymentConfirmed | CostSplitting | Notifications, Analytics | groupId, memberId, amount |
| NotificationSent | Notifications | Analytics | notificationId, channel, recipient, status |
| NotificationFailed | Notifications | Analytics | notificationId, channel, failureReason, retryCount |
| RecommendationGenerated | AIRecommendation | Analytics | recommendationType, confidence, accepted |
| RecommendationAccepted | AIRecommendation | Analytics | recommendationId, managerId, outcome |
| UserLoggedIn | Identity | Analytics, Audit | userId, role, sessionId, ipAddress |
| UserLoggedOut | Identity | Analytics, Audit | userId, sessionId |

### 6.2 RabbitMQ Exchange Configuration

```
Exchange: utop.events (type: topic, durable: true)

Routing Keys:
  booking.created
  booking.confirmed
  booking.cancelled
  booking.amended
  resource.allocated
  resource.conflict.detected
  allocation.overridden
  accommodation.booked
  pilgrimage.confirmed
  pilgrimage.compliance.checked
  group.created
  group.member.joined
  group.member.left
  cost.share.calculated
  cost.share.recalculated
  payment.confirmed
  notification.sent
  notification.failed
  recommendation.generated
  recommendation.accepted
  user.logged.in
  user.logged.out

Queues:
  utop.resource-allocation.queue     (binds: booking.confirmed, booking.cancelled)
  utop.notifications.queue           (binds: booking.*, resource.*, pilgrimage.*, group.*, cost.*, payment.*)
  utop.analytics.queue               (binds: # — all events)
  utop.cost-splitting.queue          (binds: booking.confirmed, group.member.joined, group.member.left)
  utop.audit.queue                   (binds: # — all events → immutable audit log)
```

---

## 7. Deployment Architecture

### 7.1 Docker Compose (Development)

```yaml
# docker-compose.yml
services:
  utop-api:
    build: ./src/UTOP.API
    ports: ["5000:80"]
    depends_on: [postgres, redis, rabbitmq]
    environment:
      - ConnectionStrings__Postgres=Host=postgres;Database=utop;Username=utop;Password=utop_dev
      - ConnectionStrings__Redis=redis:6379
      - RabbitMQ__Host=rabbitmq
      - JWT__SecretKey=${JWT_SECRET}

  utop-frontend:
    build: ./src/frontend
    ports: ["3000:80"]
    depends_on: [utop-api]

  postgres:
    image: postgres:16
    volumes: [postgres_data:/var/lib/postgresql/data]
    environment:
      POSTGRES_DB: utop
      POSTGRES_USER: utop
      POSTGRES_PASSWORD: utop_dev

  redis:
    image: redis:7-alpine
    volumes: [redis_data:/data]

  rabbitmq:
    image: rabbitmq:3-management
    ports: ["5672:5672", "15672:15672"]
    volumes: [rabbitmq_data:/var/lib/rabbitmq]

  elasticsearch:
    image: elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    volumes: [elastic_data:/usr/share/elasticsearch/data]

  kibana:
    image: kibana:8.11.0
    ports: ["5601:5601"]
    depends_on: [elasticsearch]

  logstash:
    image: logstash:8.11.0
    depends_on: [elasticsearch]

volumes:
  postgres_data:
  redis_data:
  rabbitmq_data:
  elastic_data:
```

### 7.2 Kubernetes Production (Future)

```
Cluster Topology:
  ├── Namespace: utop-prod
  │   ├── Deployment: utop-api (2 replicas min, auto-scale to 10)
  │   ├── Deployment: utop-frontend (2 replicas)
  │   ├── Service: utop-api-svc (ClusterIP)
  │   ├── Service: utop-frontend-svc (LoadBalancer)
  │   └── Ingress: utop-ingress (TLS termination)
  │
  ├── Namespace: utop-data
  │   ├── StatefulSet: postgres (primary + read replica)
  │   ├── StatefulSet: redis (sentinel mode)
  │   └── StatefulSet: rabbitmq (3-node cluster)
  │
  └── Namespace: utop-observability
      ├── StatefulSet: elasticsearch (3 nodes)
      ├── Deployment: kibana
      └── Deployment: logstash
```

---

## 8. Non-Functional Architecture

### 8.1 Performance Architecture

| Concern | Strategy | Target |
|---------|----------|--------|
| Search latency | Stub returns in-memory data; Redis caches results | < 3 seconds |
| Booking confirmation | Async allocation (fire-and-forget after confirm) | < 5 seconds |
| Report generation | Pre-aggregated read models; materialized views | < 5 seconds |
| Login | Redis session cache; JWT validation is stateless | < 2 seconds |
| Log writes | Async to ELK; never block transactions | < 100ms |

### 8.2 Reliability Architecture

| Concern | Strategy |
|---------|----------|
| External service failure | Circuit breaker; fall back to stub; log warning |
| Database connection failure | Retry (3x, exponential backoff); health check on startup |
| RabbitMQ failure | Outbox pattern: events persisted to DB first, then published |
| Partial saga failure | Compensating transactions; saga state persisted at each step |
| Application crash | Stateless API; restart resumes from saga state in DB |

### 8.3 Scalability Architecture

| Concern | Strategy |
|---------|----------|
| Horizontal API scaling | Stateless API (JWT auth, Redis sessions); add instances |
| Database read scaling | Read replicas for analytics queries |
| Event processing scaling | Multiple consumers per RabbitMQ queue |
| Cache scaling | Redis cluster (horizontal sharding) |
| Multi-region | Kubernetes multi-region deployment (future) |

---

## 9. GitHub Repository Structure (Phase 3+)

```
unified-travel-operations-platform/
├── README.md
├── docs/
│   ├── 01_system_overview.md
│   ├── 02_System_Requirements_Specification.md
│   └── architecture/
│       ├── 01_architecture_decisions.md     (This session)
│       ├── 02_high_level_design.md          (This document)
│       ├── 03_domain_models.md              (Next)
│       ├── 04_low_level_design.md           (Next)
│       ├── 05_business_rules.md             (Next)
│       └── 06_integration_strategy.md       (Next)
├── src/
│   ├── backend/
│   │   └── (C# .NET 10 solution — Phase 5)
│   └── frontend/
│       └── (React/TypeScript — Phase 5)
├── tests/
│   └── (Phase 6)
├── deployment/
│   ├── docker/
│   │   ├── docker-compose.yml
│   │   └── docker-compose.override.yml
│   └── kubernetes/
│       └── (Phase 8)
├── .github/
│   └── workflows/
│       └── ci.yml                           (Phase 7)
├── .gitignore
└── .gitattributes
```

---

**End of High-Level Design**

**Status:** LOCKED — Ready for Domain Models and LLD
