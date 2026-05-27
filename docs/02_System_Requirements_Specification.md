# Phase 1 – System Requirements Specification (SRS)
## Unified Travel Operations Platform (UTOP)

**Version:** 1.0  
**Status:** LOCKED - Ready for Phase 3 Architecture  
**Last Updated:** Session 2 - Comprehensive Completion  
**Classification:** Project Internal - Binding Technical Specification  

---

## 1. Introduction

### 1.1 Purpose
This document specifies the complete functional and non-functional requirements for the Unified Travel Operations Platform (UTOP). It provides a detailed blueprint for architects, developers, testers, and stakeholders to design, implement, and validate the system defined in Phase 0 – System Definition.

This SRS is **binding**. All requirements listed herein are in-scope for the complete system delivery. No requirement is deferred or optional.

### 1.2 Scope
UTOP is an enterprise-grade, framework-first travel operations platform that supports:
- Multi-modal travel booking (bus, train, plane, cruise)
- Multiple travel categories (personal, leisure, religious/pilgrimage, group)
- Accommodation and ancillary service coordination
- Resource allocation and prioritization
- Knowledge sharing and micro-learning modules
- Analytics and reporting pipelines
- AI/ML-based decision support
- Comprehensive logging and observability
- Notifications across multiple channels
- Multilingual support (English, Arabic, Hindi, French)
- Pilgrimage-specific workflows with schedule compliance
- Group cost splitting and shared expense management

This SRS focuses on **what the system must do**, without prescribing proprietary logic or vendor-specific integrations. All external integrations are initially stubbed/mocked and designed for adapter-based real integration without code changes.

### 1.3 Intended Stakeholders
- Travel agency operators (daily workflow execution)
- Operations and planning managers (resource oversight, decision-making)
- System administrators (configuration, maintenance, security)
- Business and operational analysts (reporting, trend analysis, insights)
- Integration engineers (extending platform for real-world deployments)

### 1.4 Definitions, Acronyms, and Abbreviations
- **UTOP** – Unified Travel Operations Platform
- **SRS** – System Requirements Specification
- **FR** – Functional Requirement
- **NFR** – Non-Functional Requirement
- **AI/ML** – Artificial Intelligence / Machine Learning
- **Stub/Mock** – Simulated external system interfaces
- **Adapter** – Replacement module for stub/mock that integrates real services
- **RBAC** – Role-Based Access Control
- **i18n** – Internationalization (localization support)
- **Pilgrimage** – Religious journey with specific schedule and site constraints
- **Group Travel** – Multi-person coordinated journey with shared costs and logistics
- **Resource Allocation** – Assignment of vehicles, accommodations, staff to bookings based on priority

---

## 2. Overall Description

### 2.1 Product Perspective
- Builds upon Phase 0 System Definition as the binding baseline
- Framework-first, modular, and vendor-neutral architecture
- Provides simulated external interactions (stubs/mocks) to validate workflows
- Designed for adapter-based real integration without core changes
- All data flows, decisions, and workflows are observable through comprehensive logging

### 2.2 Product Functions
High-level capabilities include:
1. Multi-modal travel booking orchestration
2. Accommodation and ancillary service management
3. Travel category adaptation (personal, leisure, religious, group)
4. Resource allocation with priority-based optimization
5. Knowledge sharing and micro-learning modules
6. Analytics and reporting with data visualization
7. AI/ML-driven recommendations and decision support
8. Comprehensive audit logging and observability
9. Multi-channel notifications (Email, SMS, Push)
10. Internationalization and localization (4+ languages)
11. Pilgrimage-specific workflows with sacred site and schedule constraints
12. Group cost splitting and shared expense tracking

### 2.3 User Classes and Characteristics

| User Role | Characteristics | Primary Activities |
|-----------|-----------------|-------------------|
| **Operator** | Daily user, workflow-focused | Execute bookings, manage reservations, process amendments |
| **Manager** | Decision-maker, overview-focused | Review allocations, prioritize resources, approve exceptions |
| **Analyst** | Data-focused, insight-seeking | Generate reports, analyze trends, identify patterns |
| **Administrator** | System-focused, maintenance-oriented | Configure roles, manage permissions, maintain knowledge base |
| **Integration Engineer** | Extension-focused, deployment-oriented | Integrate real APIs, deploy to production, extend modules |

### 2.4 Operating Environment
- Local desktop environment (Windows, macOS, Linux) with standard hardware
- Cloud-ready for multi-region deployment (Kubernetes)
- Standard SQL database (PostgreSQL) with Redis caching
- No reliance on real-world travel APIs for initial deployment (all stubs/mocks)
- Containerized for consistency and portability
- Supports simultaneous access by multiple user roles

### 2.5 Design/Implementation Constraints
- Framework-first architecture with clear module boundaries
- All external systems represented through abstraction layers
- Simulated external systems to avoid proprietary dependencies
- Must support later integration with live APIs without structural changes
- SOLID principles and modular design patterns required
- Adapter pattern for replacing stubs with real implementations
- No hard-coded business logic; all rules stored as configurable data

### 2.6 Assumptions and Dependencies
- Users interact via role-based interfaces (no direct API access for operators)
- External APIs (booking, payment, accommodation) will be simulated initially
- AI/ML modules provide decision support (not mandatory execution)
- Database is available and accessible
- Network connectivity for multi-user concurrent access
- Logging infrastructure is in place and writable
- i18n/localization frameworks are available in chosen tech stack

---

## 3. Specific Requirements

### 3.1 Functional Requirements

---

#### **FR1: Multi-Modal Travel Booking Orchestration**

**Actors:** Operator, Manager

**Description:**
The system enables booking of travel across multiple modes (bus, train, plane, cruise). Each mode has specific constraints, pricing models, and availability rules. The workflow supports search, comparison, validation, pricing, and confirmation with full observability through logging.

**Functional Requirements:**

- **FR1.1:** The system allows operators to search for available travel options by:
  - Origin location (city, airport, terminal code)
  - Destination location (city, airport, terminal code)
  - Travel date and time (with flexible date range support)
  - Travel mode (bus, train, plane, cruise)
  - Passenger count and passenger classes (economy, business, first, etc.)

- **FR1.2:** The system returns search results with:
  - Available journey options ranked by relevance/price
  - Detailed itinerary (stops, transfer points, duration)
  - Pricing per passenger and total cost
  - Availability status (seats, capacity)
  - Journey-specific constraints (luggage limits, dietary restrictions for cruise, etc.)

- **FR1.3:** The system validates resource availability through stubbed logic that:
  - Checks seat/capacity availability
  - Validates route feasibility
  - Confirms driver/crew availability (for buses/trains)
  - Checks fleet constraints
  - Returns validation status with explanation

- **FR1.4:** The system calculates pricing based on:
  - Base fare per mode
  - Distance/duration multiplier
  - Passenger class multiplier
  - Surge pricing rules (stubbed)
  - Dynamic pricing engine call (stubbed, replaceable)
  - Tax and surcharge calculation
  - Discount application if applicable

- **FR1.5:** The system confirms bookings by:
  - Reserving seats/capacity
  - Generating unique booking ID (format: UTOP-[mode]-[timestamp]-[hash])
  - Recording booking timestamp and operator details
  - Storing complete itinerary and passenger manifest
  - Logging confirmation event

- **FR1.6:** The system logs all workflow steps including:
  - Search parameters and result count
  - Validation results and any failures
  - Pricing breakdown
  - Booking confirmation with full details
  - User actions and timeline

- **FR1.7:** The system supports multiple simultaneous bookings per operator:
  - No maximum limit enforced
  - Each booking tracked independently
  - Cross-booking visibility in operator dashboard
  - Amendment/cancellation capability per booking

- **FR1.8:** The system handles exceptions gracefully:
  - Unavailable routes → return suggestion alternatives
  - Capacity exhausted → return waitlist option (stubbed)
  - Price calculation failure → log error and use fallback pricing
  - Booking confirmation failure → rollback and notify operator

**Acceptance Criteria:**
- ✅ Operator can search and receive results within 3 seconds
- ✅ Booking confirmation generates unique trackable ID
- ✅ All workflow steps are logged and queryable
- ✅ System handles 100+ concurrent bookings without performance degradation
- ✅ Pricing calculation is accurate to 2 decimal places
- ✅ Exceptions are handled without system crash

**Notes:**
- All external API calls (real booking systems) are stubbed; replaceable via adapter pattern
- Framework-first: module interfaces define contracts; implementation can change
- Logging is mandatory; every decision point must be logged

---

#### **FR2: Accommodation and Ancillary Service Coordination**

**Actors:** Operator, Manager

**Description:**
The system manages accommodation bookings (hotels, resorts, hostels) and coordinates related optional services (meals, guides, transfers, travel insurance). Workflows include search, comparison, booking, and modification with full observability.

**Functional Requirements:**

- **FR2.1:** The system allows operators to search for accommodations by:
  - Destination city/region
  - Check-in and check-out dates
  - Guest count and room requirements
  - Room type preference (single, double, suite, etc.)
  - Star rating or price range filter
  - Special requirements (accessibility, pet-friendly, etc.)

- **FR2.2:** The system validates room availability through stubbed logic that:
  - Checks total rooms available for dates
  - Confirms guest capacity
  - Validates special requirements feasibility
  - Returns availability confirmation or alternatives

- **FR2.3:** The system displays accommodation details including:
  - Room description and amenities
  - Guest reviews and ratings (demo data)
  - Photo gallery
  - Cancellation policy
  - Check-in/check-out times
  - Available ancillary services (meals, laundry, transfers, etc.)

- **FR2.4:** The system allows operators to select accommodations and services:
  - Primary accommodation selection
  - Ancillary service selection (breakfast included, daily laundry, guided tours, etc.)
  - Special requests and notes
  - Length of stay confirmation

- **FR2.5:** The system calculates total cost including:
  - Base accommodation rate per night
  - Taxes and resort fees
  - Ancillary service charges
  - Discounts or promotional rates (if applicable)
  - Total for entire stay

- **FR2.6:** The system confirms bookings by:
  - Reserving rooms and services
  - Generating unique confirmation ID (format: UTOP-ACC-[timestamp]-[hash])
  - Recording booking details and operator information
  - Storing guest manifest and special requirements
  - Sending confirmation event to logging

- **FR2.7:** The system supports post-booking modifications:
  - Amendment requests (change dates, add services, modify guests)
  - Cancellation with refund calculation per policy
  - Service additions or removals
  - All changes logged with timestamp and operator identity

- **FR2.8:** The system handles service coordination:
  - Meal planning (breakfast, lunch, dinner selection)
  - Transfer scheduling (airport pickup, local tours)
  - Activity booking (tour operators, guides)
  - Insurance coordination (travel insurance quotes and enrollment)

**Acceptance Criteria:**
- ✅ Accommodation search returns results within 2 seconds
- ✅ Price calculation includes all fees and taxes accurately
- ✅ Booking confirmation generates unique trackable ID
- ✅ Modifications are processed and logged
- ✅ Cancellation refunds calculated per policy (stubbed rules)
- ✅ Operator can view all accommodation details clearly

**Notes:**
- Accommodation provider APIs (real hotels) are stubbed; replaceable via adapter
- Ancillary services tied to accommodations for coordinated booking
- Modifications trigger amendment workflows with manager approval (if significant)

---

#### **FR3: Travel Category Adaptation and Workflow Support**

**Actors:** Operator, Manager

**Description:**
The system supports four distinct travel categories, each with specific workflows, constraints, and optimization rules. The system adapts booking processes, resource allocation, and reporting based on selected category.

**Functional Requirements:**

- **FR3.1:** The system allows operators to classify travel by category:
  - Personal travel (individual, no special rules)
  - Leisure travel (group or individual, recreational focus)
  - Religious/Pilgrimage travel (specific constraints and schedule compliance)
  - Group travel (multiple travelers, coordinated logistics, shared costs)

- **FR3.2:** The system applies category-specific validation rules:
  - **Personal:** No special constraints; standard booking rules apply
  - **Leisure:** Booking flexibility; optional group coordinator role
  - **Religious/Pilgrimage:** Schedule constraints (prayer times, sacred site hours), group mobility requirements, specific accommodation needs
  - **Group:** Multiple traveler coordination, cost-sharing rules, group approval workflows

- **FR3.3:** The system adapts workflows based on category:
  - **Personal:** Solo booking to confirmation
  - **Leisure:** Optional group booking with individual flexibility
  - **Religious/Pilgrimage:** Guided itinerary with schedule compliance checks, multi-leg journey planning
  - **Group:** Initiate group, invite members, collect preferences, coordinated booking, shared cost management

- **FR3.4:** The system enforces category-specific constraints:
  - **Personal:** None beyond standard rules
  - **Leisure:** Optional guide/coordinator
  - **Religious/Pilgrimage:** 
    - Prayer schedule compliance (5 daily prayers for Islamic pilgrimage, etc.)
    - Sacred site access hours (opening/closing times)
    - Specific accommodation types (near mosques, temples, etc.)
    - Guided tour requirement (often mandatory)
    - Group mobility (no individuals separated)
  - **Group:** 
    - Minimum group size (configurable, default 2)
    - Group leader designation
    - Shared transportation preference
    - Cost-splitting rules

- **FR3.5:** The system logs category selection and category-specific decisions:
  - Category choice timestamp
  - Category-specific constraints checked
  - Adapted workflows executed
  - Any category-based exceptions or overrides

- **FR3.6:** The system allows category change before final confirmation:
  - Switch from Personal to Leisure/Group with re-calculation
  - Switch from Leisure to Group with invite workflow
  - Switch between categories triggers re-validation
  - All changes logged with operator approval

- **FR3.7:** The system generates category-specific reports:
  - Personal bookings analytics (frequency, destinations, spending)
  - Leisure group insights (group size trends, repeat groups)
  - Pilgrimage specific metrics (prayer schedule adherence, sacred site visits, group cohesion)
  - Group cost analysis (sharing fairness, refund tracking)

**Acceptance Criteria:**
- ✅ Category selection is enforced before booking confirmation
- ✅ Category-specific constraints are applied and logged
- ✅ Workflow adapts based on selected category
- ✅ Category switching recalculates pricing and constraints
- ✅ Category-specific reports are accurate and complete
- ✅ No bookings can proceed without category classification

**Notes:**
- Category is immutable after booking confirmation (no post-booking category change)
- Religious/Pilgrimage workflows are complex; detailed specification in FR11
- Group travel workflows are complex; detailed specification in FR12
- Framework supports adding new categories without code changes (configurable rules)

---

#### **FR4: Resource Allocation Engine with Prioritization**

**Actors:** Manager, System

**Description:**
The system allocates resources (vehicles, seats, accommodations, staff) to ongoing bookings based on priority rules, availability, and optimization criteria. Resource allocation considers multiple factors and logs all decisions for auditability.

**Functional Requirements:**

- **FR4.1:** The system tracks resource inventory including:
  - Vehicle fleet (buses, trains, aircraft, cruise ships) with capacity
  - Accommodation availability (rooms, types, capacity)
  - Staff availability (drivers, guides, coordinators, managers)
  - Ancillary services capacity (meal slots, tour slots, transfer capacity)

- **FR4.2:** The system supports multiple prioritization strategies:
  - **First-come-first-served:** Allocate to earliest booking
  - **High-value-first:** Prioritize high-revenue bookings
  - **VIP-first:** Prioritize pre-defined VIP categories
  - **Group-first:** Prioritize group travel over personal
  - **Religious-compliance-first:** Prioritize pilgrimage-specific allocations
  - **Custom-rules:** Manager-defined rules per situation

- **FR4.3:** The system assigns resources to bookings by:
  - Identifying resource requirements from booking (mode, capacity, special needs)
  - Applying priority strategy to candidate resources
  - Checking resource availability and conflicts
  - Performing conflict resolution if over-booked
  - Allocating resource and logging decision

- **FR4.4:** The system handles conflicts and exceptions:
  - Over-subscription detection (demand > supply)
  - Allocation failure → escalate to manager for manual decision
  - Resource unavailability → suggest alternatives
  - Special constraints (accessibility, dietary) → verify resource match

- **FR4.5:** The system optimizes allocations:
  - Minimize empty capacity (fill buses before deploying new ones)
  - Minimize transfers (prefer direct routes)
  - Prefer grouped resources (group travelers on same vehicle when possible)
  - Consider utilization rates and cost efficiency

- **FR4.6:** The system allows manual overrides:
  - Manager can manually allocate resources
  - Override rules recorded with justification
  - System flags high-cost or low-efficiency allocations
  - All overrides logged for audit

- **FR4.7:** The system tracks allocation lifecycle:
  - Pre-allocation (tentative reserve)
  - Confirmed allocation (locked resource)
  - In-transit/in-use (resource active)
  - Post-use (resource released, utilization metrics recorded)

- **FR4.8:** The system generates allocation reports:
  - Resource utilization rate (occupied vs. total capacity)
  - Allocation time (how quickly resources were assigned)
  - Conflict resolution rate (manual overrides percentage)
  - Cost efficiency metrics (revenue per resource per unit time)

**Acceptance Criteria:**
- ✅ Resources are allocated within 30 seconds of booking confirmation
- ✅ Allocation respects all constraints (capacity, special needs, availability)
- ✅ Conflicts are detected and escalated appropriately
- ✅ All allocation decisions are logged with decision rationale
- ✅ Utilization metrics are accurate
- ✅ Managers can override auto-allocation with justification

**Notes:**
- Resource allocation is asynchronous (happens post-booking)
- Allocation can be re-optimized if new bookings arrive
- Allocation engine is framework-agnostic; can swap optimization algorithms
- Framework provides decision logging; algorithm can be replaced via adapter

---

#### **FR5: Knowledge Sharing and Micro-Learning Modules**

**Actors:** Administrator, Operator, Manager, All users

**Description:**
The system provides contextual, just-in-time learning content linked to workflows. Knowledge modules help operators understand processes, troubleshoot issues, and build competency. Learning is tracked and contributes to operational efficiency.

**Functional Requirements:**

- **FR5.1:** The system maintains a knowledge base organized by:
  - Topic (booking, accommodation, resource allocation, etc.)
  - Difficulty level (beginner, intermediate, advanced)
  - Audience role (operator, manager, admin)
  - Workflow context (what workflow does this apply to)
  - Search tags and keywords

- **FR5.2:** The system provides content types including:
  - **Micro-lessons:** 2-5 minute focused explanations with examples
  - **Troubleshooting guides:** Step-by-step problem resolution
  - **Video tutorials:** Screen recordings or instructional videos (optional, stubs allowed)
  - **Use case examples:** Real workflow scenarios and outcomes
  - **Best practices:** Optimization tips and efficiency insights
  - **FAQs:** Common questions and answers

- **FR5.3:** The system provides contextual access:
  - Help button on every workflow screen
  - Contextual suggestions based on current action
  - Search interface for on-demand knowledge lookup
  - Suggested learning path for new operators

- **FR5.4:** The system tracks learning engagement:
  - Content view count and duration
  - User completion of modules
  - Learning effectiveness (measured by reduced errors post-training)
  - Certification status for critical workflows

- **FR5.5:** The system supports knowledge contribution:
  - Administrators create and update content
  - Experienced operators can suggest new topics
  - Content versioning and archival
  - Localization of knowledge base (multi-language)

- **FR5.6:** The system provides learning analytics:
  - User competency assessment (which topics mastered)
  - Team knowledge gaps (common problem areas)
  - Training ROI (correlation between training and performance)
  - Content effectiveness (which modules most useful)

- **FR5.7:** The system integrates learning into workflows:
  - Suggestion on first error: "Do you want to learn about this?"
  - Required certification before workflow access (for critical tasks)
  - Refresher prompts based on last-used date
  - Competency verification before complex allocations

- **FR5.8:** The system supports knowledge reuse:
  - Content templates for common scenarios
  - Workflow-specific checklists
  - Decision trees for troubleshooting
  - Integration with logging for case-based learning

**Acceptance Criteria:**
- ✅ Operators can access help within 2 clicks from any workflow
- ✅ Knowledge base covers all critical workflows (minimum 15 modules)
- ✅ Learning engagement is tracked and reported
- ✅ Content is searchable and discoverable
- ✅ Localization supports at least English, Arabic, Hindi, French
- ✅ New operators can complete onboarding knowledge path in under 4 hours

**Notes:**
- Knowledge is critical for operator efficiency; not optional
- Content should be written by subject matter experts
- Video content can be stubbed initially (link placeholders)
- Learning effectiveness should be measured and content improved iteratively

---

#### **FR6: Analytics and Reporting Pipelines**

**Actors:** Analyst, Manager

**Description:**
The system captures operational data and generates reports/dashboards for decision support. Analytics enable trend analysis, performance evaluation, and operational optimization. All analytics run on stubbed or demo data initially; replaceable via adapters.

**Functional Requirements:**

- **FR6.1:** The system logs all workflow execution events:
  - Booking creation, amendment, cancellation (timestamp, operator, details)
  - Resource allocation decisions (priority strategy, outcome, conflict resolution)
  - Accommodation and service events (inquiry, booking, modification, cancellation)
  - Travel category selection and constraints applied
  - Cost calculations and pricing changes
  - User actions and system events

- **FR6.2:** The system aggregates data for reporting:
  - Booking volume by mode, category, destination (daily/weekly/monthly)
  - Revenue analysis (per mode, per category, per operator, trend analysis)
  - Accommodation metrics (occupancy rate, average length of stay, popular destinations)
  - Resource utilization (capacity used vs. available, cost per unit)
  - Staffing metrics (assignments, utilization, efficiency)

- **FR6.3:** The system provides filtered analytics views:
  - **By time period:** Day, week, month, quarter, year
  - **By dimension:** Geography, mode, category, operator, resource type, travel group
  - **By metric:** Volume, revenue, cost, efficiency, quality
  - **Drill-down capability:** From summary to detail transaction level

- **FR6.4:** The system generates reportable outputs:
  - **Pre-built reports:** Daily summary, weekly performance, monthly financial review
  - **Ad-hoc reports:** Analyst-defined queries returning tabular data
  - **Dashboards:** Real-time visualization of key metrics with drill-down capability
  - **Export capability:** CSV, PDF, Excel formats for external analysis
  - **Scheduled reports:** Automated delivery via email on defined schedules

- **FR6.5:** The system supports decision-focused analytics:
  - **Anomaly detection:** Unusual booking patterns, pricing deviations (stubbed logic)
  - **Trend analysis:** Booking growth, revenue trends, seasonal patterns
  - **Forecasting:** Predictive models for demand and resource needs (stubbed)
  - **Benchmarking:** Performance comparison vs. targets or historical data
  - **What-if analysis:** Impact of pricing or resource allocation changes (stubbed)

- **FR6.6:** The system logs all analytics executions:
  - Report generation timestamp, analyst, parameters, result count
  - Data sources used (booking table, resource table, etc.)
  - Query execution time and data volume processed
  - Any data quality issues or warnings

- **FR6.7:** The system provides role-specific views:
  - **Operator:** Personal performance (bookings processed, revenue, efficiency)
  - **Manager:** Team performance (resource utilization, cost per booking, KPIs)
  - **Analyst:** Full operational view (all metrics, all segments)
  - **Administrator:** System health (performance, errors, audit trails)

- **FR6.8:** The system supports data governance:
  - Sensitive data masking (personal info in exports)
  - Access control (analyst views > operator views)
  - Data retention policies (log archival after 12 months)
  - Audit of all report access

**Acceptance Criteria:**
- ✅ Reports generate within 5 seconds for standard queries
- ✅ All major operational events are logged
- ✅ Dashboard displays real-time KPIs accurately
- ✅ Exported reports contain accurate calculations
- ✅ Access controls prevent unauthorized data access
- ✅ At least 8 pre-built reports available

**Notes:**
- Analytics engine is stubbed initially; real BI tools (Tableau, Power BI) can replace via adapter
- Logging is the foundation; analytics quality depends on logging completeness
- Framework separates data capture from analysis; engines can change
- Demo data should be realistic for testing; not inflated/artificial

---

#### **FR7: AI/ML-Based Decision Support Layer**

**Actors:** Manager, System

**Description:**
The system provides AI/ML-driven recommendations for resource allocation, pricing optimization, and demand forecasting. AI/ML is integrated into decision workflows but not mandatory for execution. Stubbed initially; replaceable with real models via adapters.

**Functional Requirements:**

- **FR7.1:** The system collects training data:
  - Historical booking patterns (mode, timing, destination, revenue)
  - Resource allocation outcomes (efficiency, cost, utilization)
  - Pricing decisions and resulting demand
  - Seasonal and trend patterns
  - Group behavior and preferences

- **FR7.2:** The system provides recommendation engines (stubbed):
  - **Resource allocation recommendation:** Suggest optimal vehicle/staff allocation based on booking characteristics (returns suggestion score 0-100)
  - **Dynamic pricing recommendation:** Suggest price adjustments based on demand and inventory (returns price adjustment factor 0.8-1.2)
  - **Demand forecast:** Predict booking volume for upcoming periods (returns forecast confidence 0-100)
  - **Group travel recommendation:** Suggest optimal group size and composition for efficiency (returns efficiency score)

- **FR7.3:** The system integrates recommendations into workflows:
  - Resource allocation: Show AI-recommended allocation with confidence score; manager can accept/override
  - Pricing: Display AI-suggested price adjustment; operator can approve/modify
  - Forecasting: Display demand forecast for capacity planning
  - Optimization: Suggest cost-reduction actions to manager

- **FR7.4:** The system logs all AI/ML decisions:
  - Input parameters (booking data, context, constraints)
  - AI model name and version
  - Recommendation (output value, confidence score)
  - Manager action (accepted, overridden, ignored)
  - Outcome (if measurable)
  - Timestamp and decision rationale

- **FR7.5:** The system handles AI/ML failures gracefully:
  - Model unavailable → return no recommendation (don't block workflow)
  - Malformed input → log error and use fallback logic
  - Unusually low confidence → flag for manual review
  - Contradictory recommendations → escalate to manager

- **FR7.6:** The system supports model replacement:
  - Clear interface contracts for recommendation engine
  - Stub returns placeholder values; real models replace seamlessly
  - Multiple models can coexist (A/B testing capability)
  - Model versioning and rollback capability

- **FR7.7:** The system measures AI/ML effectiveness:
  - Recommendation acceptance rate (% of times manager accepts)
  - Outcome vs. alternative (did AI recommendation perform better than manager override?)
  - Model accuracy (forecast error, allocation efficiency improvement)
  - ROI (cost savings from AI recommendations vs. cost to run)

- **FR7.8:** The system supports explainability:
  - Show AI recommendation rationale (top factors influencing decision)
  - Transparency of inputs used (which booking characteristics mattered most)
  - Confidence intervals (why is this recommendation 82% vs. 95% confident)
  - Historical performance of recommendation type

**Acceptance Criteria:**
- ✅ Recommendations are provided within 2 seconds
- ✅ Framework supports stubbed and real AI models interchangeably
- ✅ All recommendations are logged with full context
- ✅ Manager can see recommendation rationale
- ✅ Recommendation rejection doesn't block workflow
- ✅ AI effectiveness is measurable and reported

**Notes:**
- AI/ML is enhancement, not critical path; system functions without recommendations
- Stub implementations return deterministic values for testing
- Real models (scikit-learn, TensorFlow, LLMs) replace stubs via adapter pattern
- Explainability is important for trust; must be included from start

---

#### **FR8: Comprehensive Logging and Observability**

**Actors:** All roles, System

**Description:**
The system provides exhaustive logging of all operations, decisions, and state changes. Logging is fundamental to auditability, troubleshooting, and validating framework correctness. All modules integrate through standardized logging interfaces.

**Functional Requirements:**

- **FR8.1:** The system logs all user actions:
  - Authentication (login, logout, session events)
  - Workflow initiation (search, booking, allocation request)
  - Workflow progression (step completion, decision, confirmation)
  - Workflow completion or failure
  - Administrative actions (user creation, permission changes, configuration)

- **FR8.2:** The system logs all module executions:
  - Module entry (which module called, parameters)
  - Module processing steps (major decision points)
  - Module output (result, status, any errors)
  - Module exit (completion status, timing)
  - Calls to external systems (stubs/mocks and real adapters)

- **FR8.3:** Logs capture structured information:
  - Timestamp (microsecond precision)
  - User identity (operator, manager, system)
  - Action/event description (what happened)
  - Context (workflow ID, booking ID, resource ID)
  - Input parameters (what was passed in)
  - Output/result (what was returned)
  - Decision rationale (why this decision was made)
  - Performance metrics (execution time, resource usage)
  - Status (success, failure, partial)
  - Error details (if failed)

- **FR8.4:** Logs are stored persistently:
  - Log storage in PostgreSQL (queryable via SQL)
  - Log retention: 24 months minimum
  - Archival: logs older than 12 months archived to cold storage
  - Performance: log writes don't block transactions (async writes)
  - Durability: logs persist even if workflow fails

- **FR8.5:** The system provides log access and filtering:
  - Operator can view their own logs
  - Manager can view team logs
  - Analyst can query all logs for reporting
  - Administrator can configure log retention and access
  - Structured log search (filter by date, user, workflow, status, etc.)

- **FR8.6:** The system generates log-based dashboards:
  - Real-time activity feed (latest events)
  - User activity summary (who did what)
  - Workflow completion rate (success vs. failure)
  - Performance metrics (average processing time per workflow)
  - Error rate and error types
  - Resource utilization trends (visible in logs)

- **FR8.7:** The system enforces logging standards:
  - All modules must write to logging interface (not stdout/file)
  - Sensitive data (passwords, payment info) never logged
  - Consistent log format (structured JSON)
  - Correlation IDs for tracing cross-module workflows
  - Log levels (DEBUG, INFO, WARN, ERROR) applied consistently

- **FR8.8:** The system supports audit trails:
  - Immutable audit log for compliance (cannot modify historical logs)
  - Clear provenance for all changes (who changed what, when, why)
  - Traceability for sensitive operations (booking cancellation refund calculation)
  - Regulatory compliance (if applicable jurisdictions require audit logs)

- **FR8.9:** The system detects and alerts on anomalies:
  - Unusual patterns (operator booking rate 10x normal)
  - Failed operations (systematic workflow failures)
  - Performance degradation (processing time increasing)
  - Data inconsistencies (logging reveals conflicts)

**Acceptance Criteria:**
- ✅ All user actions are logged within 100ms
- ✅ Logs include sufficient context for debugging (not just "operation succeeded")
- ✅ Log queries return results within 2 seconds
- ✅ Sensitive data is never logged
- ✅ Logs are immutable after writing
- ✅ Log storage doesn't impact workflow performance
- ✅ Correlation IDs trace workflows across modules

**Notes:**
- Logging is non-negotiable; every decision point must be logged
- Framework provides logging interface; implementation can change (file, database, cloud)
- Logs are primary source of truth for audits and compliance
- Log analysis reveals system behavior for optimization

---

#### **FR9: Multi-Channel Notifications**

**Actors:** All users (operators, managers, analysts)

**Description:**
The system notifies users of important events through multiple channels (Email, SMS, Push notifications). Notifications keep stakeholders informed of booking confirmations, amendments, escalations, and alerts. All notifications are audited.

**Functional Requirements:**

- **FR9.1:** The system supports notification channels:
  - **Email:** Rich formatted messages with booking details, links, actionable buttons
  - **SMS:** Concise text messages for urgent/time-sensitive alerts
  - **Push notifications:** In-app or mobile app notifications for real-time events
  - **In-system messages:** Message center within application

- **FR9.2:** The system defines notification events:
  - **Booking events:** Confirmation, amendment, cancellation, confirmation reminder
  - **Resource allocation events:** Allocation completed, conflict detected, escalation required
  - **Approvals:** Approval request (manager), approval decision (operator)
  - **Alerts:** Group member joined/left, schedule conflict detected, cost split issue
  - **System events:** New knowledge available, system maintenance, error recovery

- **FR9.3:** The system manages notification preferences:
  - User can select channels per event type (SMS for urgent, Email for summary)
  - User can opt-out of non-critical notifications
  - User can set quiet hours (no notifications during specified times)
  - Manager can enforce notification policies for team

- **FR9.4:** The system generates notification content:
  - Operator: "Booking confirmed - Bus from Delhi to Agra, Jan 15, 2 passengers, ₹2500. Ref: UTOP-BUS-123456"
  - Manager: "Resource conflict - 5 group bookings competing for 2 buses. Awaiting manual allocation."
  - Analyst: "Daily report ready - 156 bookings processed, ₹3.2M revenue, 94% utilization."
  - Localized for recipient's language

- **FR9.5:** The system schedules notifications:
  - Immediate: Critical alerts (system errors, conflicts)
  - Delayed: Non-urgent notifications batched hourly
  - Scheduled: Daily/weekly summaries at configured times
  - Reminder: Follow-up notifications (booking reminders 24hrs before travel)

- **FR9.6:** The system handles notification failures:
  - Email delivery retry (up to 3 attempts)
  - SMS delivery failure escalation (switch to email)
  - Push delivery to offline users queued (deliver on next login)
  - Log all delivery attempts and outcomes

- **FR9.7:** The system tracks notification engagement:
  - Email open rate and link click-through rate
  - SMS read status (delivery + user acknowledgement)
  - Push notification action taken
  - In-system message read status
  - Engagement metrics inform notification effectiveness

- **FR9.8:** The system supports template-based notifications:
  - Pre-defined templates for common events (booking confirmation, approval request)
  - Template variables (booking ID, passenger name, etc.)
  - Template localization (different versions per language)
  - Custom templates for special scenarios

- **FR9.9:** The system implements notification audit:
  - All sent notifications logged (recipient, channel, content, timestamp)
  - Delivery confirmation logged
  - Failed notifications tracked and retried
  - Audit trail shows notification history per booking/user

**Acceptance Criteria:**
- ✅ Notifications sent within 30 seconds of triggering event
- ✅ All channels support required notification types
- ✅ Notifications are accurate and include relevant booking details
- ✅ Delivery failures are retried and logged
- ✅ User preferences are respected
- ✅ Localization works for all supported languages

**Notes:**
- Notification service is initially stubbed (mock email/SMS)
- Real providers (AWS SES, Twilio, Firebase) replace via adapter pattern
- User preferences stored in database (not hard-coded)
- Notification templates configurable by administrators

---

#### **FR10: Internationalization (i18n) and Localization**

**Actors:** All users, Administrator

**Description:**
The system supports multiple languages and regional preferences. Localization includes language translation, regional date/time formats, currency display, and cultural adaptations. Supported languages: English (en-US), Arabic (ar-SA), Hindi (hi-IN), French (fr-FR).

**Functional Requirements:**

- **FR10.1:** The system supports language selection:
  - User selects language on login or in preferences
  - Selection persists across sessions
  - Admin can set default language per region
  - Real-time language switching without re-login

- **FR10.2:** The system provides translated content:
  - **UI labels and buttons:** All interface text translated
  - **Reports and dashboards:** Dynamic translation based on user language
  - **Error messages:** Localized explanations (not just error codes)
  - **Knowledge base:** Content available in all supported languages
  - **Notifications:** Messages sent in user's language

- **FR10.3:** The system formats data by region:
  - **Dates:** MM/DD/YYYY (US), DD/MM/YYYY (Europe/India), formatted according to locale
  - **Times:** 12-hour (US) vs. 24-hour (Europe/India)
  - **Currency:** USD ($), SAR (﷼), INR (₹), EUR (€) displayed with correct symbol
  - **Numbers:** Thousands separator (1,000 vs. 1.000 vs. 1000), decimal point (. vs. ,)
  - **Addresses:** Format adapted to regional postal standards

- **FR10.4:** The system handles regional business rules:
  - **Arabic regions:** Right-to-left (RTL) text direction for interface
  - **Indian regions:** Rupee currency, Hindi content, regional holidays
  - **France:** EU privacy rules, French language requirements
  - **Multiple religions:** Prayer time formats (Islamic 5 times, Hindu, Christian)

- **FR10.5:** The system manages translation data:
  - Translation strings stored in database (not hard-coded)
  - Admin can update translations without redeployment
  - Translation version control (track changes)
  - Fallback to English if translation missing (graceful degradation)

- **FR10.6:** The system supports dynamic content localization:
  - Booking confirmations translated based on user language
  - System notifications localized
  - Reports generated in user's language
  - Knowledge base links point to correct language version

- **FR10.7:** The system handles multi-language input:
  - Accepts passenger names in any supported language
  - Search works in user's language (transliteration if needed)
  - Special characters (Arabic numerals, Hindi script) properly stored and displayed
  - No character encoding issues

- **FR10.8:** The system supports regional adaptations:
  - Holiday calendars (Islamic, Hindu, Christian, secular regional holidays)
  - Regional payment methods (card, bank transfer, mobile wallet per country)
  - Regional compliance (GDPR for EU, DPDP for India)
  - Regional carrier preferences (carriers common in region highlighted)

- **FR10.9:** The system provides translation management:
  - Admin interface to view/edit translations
  - Progress tracking (percentage translated per language)
  - Crowdsourcing capability (community can suggest translations)
  - Translation quality checks (terminology consistency, professionalism)

**Acceptance Criteria:**
- ✅ All UI text available in 4 languages
- ✅ Language switching is seamless (no page reload needed)
- ✅ Dates, times, and currency display correctly per region
- ✅ RTL support works for Arabic
- ✅ No character encoding errors
- ✅ Missing translations gracefully fall back to English
- ✅ Reports generate in selected language

**Notes:**
- i18n framework chosen in tech stack (i18next, gettext, etc.) defines implementation
- Translation strings must be complete before GA (no "placeholder" translations)
- Professional translators recommended for all languages (not machine translation only)
- Regional compliance (GDPR, DPDP) integrated with localization

---

#### **FR11: Pilgrimage-Specific Workflows**

**Actors:** Operator, Manager

**Description:**
The system supports specialized workflows for religious/pilgrimage travel. Pilgrimage booking is distinct from leisure travel due to schedule constraints (prayer times, sacred site access), group mobility requirements, and guided tour mandates. Workflows ensure compliance with pilgrimage-specific rules.

**Functional Requirements:**

- **FR11.1:** The system captures pilgrimage parameters:
  - Religion/tradition (Islamic, Hindu, Christian, Buddhist, etc.)
  - Pilgrimage type (Hajj, Umrah, Kumbh Mela, etc.)
  - Sacred sites to visit (Mecca, Varanasi, Jerusalem, etc.)
  - Pilgrimage dates and duration
  - Group size and composition
  - Special requirements (wheelchair accessibility, dietary needs, language needs)

- **FR11.2:** The system enforces prayer schedule compliance:
  - Daily prayer times fetched based on location and religion
  - Bookings validated to ensure prayer time access
  - Alerts if transportation schedule conflicts with prayer times
  - Suggestions for prayer-schedule-friendly booking times
  - Hotel selection ensures proximity to prayer facilities (mosque, temple, etc.)

- **FR11.3:** The system manages sacred site constraints:
  - Sacred site operating hours recorded (e.g., Mecca entry only during Hajj dates)
  - Booking validated for site access eligibility (e.g., only Muslims enter sanctum)
  - Group mobility enforced (pilgrims not separated from group)
  - Queue management and crowd coordination (if available)
  - Special dress codes and conduct rules highlighted to pilgrims

- **FR11.4:** The system mandates guided tours:
  - Pilgrimage guide (knowledgeable in religion/tradition) required
  - Guide assignment at booking time
  - Guide availability checked and enforced
  - Guide language capability matched to pilgrims
  - Guide contact information and background provided to group

- **FR11.5:** The system provides spiritual/cultural content:
  - Sacred site information (history, significance, visitor guidelines)
  - Prayer ritual guides (what to expect, how to participate)
  - Pilgrimage etiquette and conduct expectations
  - Spiritual reading/reflection suggestions
  - Interfaith respect guidelines (if multi-faith group)

- **FR11.6:** The system supports multi-leg pilgrimage journeys:
  - First leg: Origin → Gateway city (e.g., Delhi → Mumbai)
  - Second leg: Gateway → Sacred site (e.g., Mumbai → Mecca)
  - Third leg: Sacred site → Home (e.g., Mecca → Delhi)
  - Each leg booked but coordinated as single pilgrimage
  - Accommodation coordinated across legs
  - Baggage continuity managed

- **FR11.7:** The system enforces group cohesion:
  - All pilgrims must stay as group (no splitting)
  - Group leader designation and authority
  - Group decision-making for itinerary changes
  - Cost-sharing for group expenses (transport, guide, accommodation)
  - Conflict resolution if pilgrims want to separate

- **FR11.8:** The system provides pilgrimage-specific reporting:
  - Pilgrimage completion confirmation (all sacred sites visited, prayer times met)
  - Group cohesion metrics (% of group together throughout)
  - Satisfaction survey (spiritual fulfillment, experience quality)
  - Incident log (medical emergencies, lost pilgrims, etc.)
  - Repeat pilgrimage metrics (are pilgrims returning?)

- **FR11.9:** The system handles pilgrimage exceptions:
  - Medical emergency → escalate and arrange care
  - Pilgrim wants to leave group → documented with approval
  - Sacred site closed unexpectedly → offer alternatives
  - Weather/natural disaster → contingency routing
  - Cultural conflict → mediation and support

**Acceptance Criteria:**
- ✅ Prayer schedule checked for all pilgrimage bookings
- ✅ Sacred site constraints enforced (can't book if site closed)
- ✅ Guide assigned and availability confirmed
- ✅ Group mobility maintained (tracking, communication)
- ✅ Multi-leg itinerary coordinated seamlessly
- ✅ Pilgrimage-specific content displayed to pilgrims
- ✅ Completion confirmation accurate and documented

**Notes:**
- Pilgrimage workflows require cultural sensitivity; involve domain experts in validation
- Prayer times API (Aladhan, Islamic Finder) stubbed; real API replaceable
- Sacred site information maintained in database (admin can update)
- Pilgrimage guides are specialized resource; tracked separately in resource allocation
- Religious holidays and festival dates considered in booking availability

---

#### **FR12: Group Cost Splitting and Shared Expense Management**

**Actors:** Operator, Manager (Group coordinator)

**Description:**
The system manages cost splitting for group travel. Multiple travelers share transportation, accommodation, and ancillary costs. The system calculates fair shares, tracks individual contributions, and manages refunds/adjustments. Cost splitting must handle complex scenarios (late joiners, early leavers, partial participation).

**Functional Requirements:**

- **FR12.1:** The system creates group bookings:
  - Group coordinator initiates group booking
  - Group name and description defined
  - Expected group size and arrival dates
  - Shared cost items identified (transport, accommodation, meals)
  - Cost-sharing formula selected (equal split, per-person, weighted, etc.)

- **FR12.2:** The system manages group membership:
  - Group coordinator invites members (by email or code)
  - Members accept/decline invitations
  - Late joiners can be added (with recalculated shares)
  - Early leavers can be removed (with refund calculation)
  - Guest status vs. full member (different cost obligations)

- **FR12.3:** The system collects member preferences:
  - Accommodation preference (single, shared room)
  - Meal plan (vegetarian, non-veg, halal, kosher, etc.)
  - Activity participation (yes/no for each optional activity)
  - Special requirements (accessibility, proximity to prayer facilities)
  - Cost sensitivity (budget-conscious or flexible)

- **FR12.4:** The system calculates individual costs:
  - Base per-person cost (transport + accommodation + mandatory meals)
  - Variable costs (optional activities, premium choices)
  - Shared costs allocation (guide, group coordinator fee, insurance)
  - Discounts applied fairly (group booking discount split equally)
  - Tax and surcharge allocation
  - Final per-person total cost

- **FR12.5:** The system handles cost adjustments:
  - Late joiner joins → recalculate all shares (increase or decrease)
  - Early leaver leaves → calculate refund based on cancellation policy
  - Member upgrades accommodation → charge individual, not group
  - Shared meal cost changes → recalculate only for remaining members
  - Group activity upsell → only those participating pay increment

- **FR12.6:** The system tracks payment status:
  - Expected amount per member
  - Paid amount (if partially paid)
  - Outstanding balance (if any)
  - Payment deadline (group pays before travel start)
  - Payment method per member (separate cards, one card for all, etc.)

- **FR12.7:** The system facilitates payment collection:
  - Generate payment link per member
  - Send payment reminder (1 week, 3 days, 1 day before travel)
  - Support split payment (partial payment now, remainder later)
  - Process group refund when members drop
  - Handle payment failures and retry

- **FR12.8:** The system provides cost transparency:
  - Per-member cost breakdown (transport, accommodation, meals, activities, taxes)
  - Comparison to group average (is member paying more/less and why)
  - Refund calculation transparency (if member leaves, show refund formula)
  - Group savings (total cost vs. individual bookings would cost)

- **FR12.9:** The system resolves cost disputes:
  - Member contests their share → show calculation and support docs
  - Member paid but marked unpaid → investigate and correct
  - Refund calculation disagreement → escalate to manager for review
  - Partial refund scenarios (member used services partially)

- **FR12.10:** The system generates cost reports:
  - Group cost summary (total, per-member, breakeven)
  - Payment status (who paid, who owes, overdue list)
  - Refund tracking (processed, pending, disputed)
  - Group profitability (actual cost vs. collected revenue)
  - Cost variance analysis (budgeted vs. actual spend)

**Acceptance Criteria:**
- ✅ Per-member cost calculated correctly for all scenarios
- ✅ Cost splits recalculated accurately when members join/leave
- ✅ Payment tracking is accurate and transparent
- ✅ Payment reminders sent on schedule
- ✅ Refund calculations are fair and documented
- ✅ Group coordinator can view cost status anytime
- ✅ Members can see their cost breakdown

**Notes:**
- Cost splitting is complex math; must be thoroughly tested
- Fairness is critical (unfair splits damage trust)
- Currency handling for multi-currency groups (group in India, members from UAE)
- Integration with payment gateway (Stripe, Razorpay stubbed)
- Late-join cost recalculation must be approved by group (fairness)
- Refund disputes may need manager escalation

---

### 3.2 Non-Functional Requirements (NFRs)

---

**NFR1 – Scalability**

The system must support operational growth without architectural changes:
- **Concurrent users:** Support 100+ simultaneous operators, managers, analysts without degradation
- **Booking throughput:** Process 1000+ bookings per hour
- **Horizontal scaling:** Backend services deployable across multiple servers/pods
- **Database scaling:** Support 10M+ historical bookings (with archival of old data)
- **Load balancing:** Distribute requests across multiple instances

---

**NFR2 – Security**

The system must protect data and enforce access control:
- **Authentication:** Role-based login (operator, manager, analyst, admin)
- **Authorization:** RBAC enforced for all operations (operator cannot change others' data)
- **Data protection:** Sensitive data (personal info, payment details) encrypted at rest (AES-256) and in transit (TLS 1.3)
- **Audit logging:** All access and modifications logged immutably
- **Password policy:** Minimum 12 characters, complexity requirements, rotation every 90 days
- **Session management:** 30-minute idle timeout, secure session tokens, CSRF protection

---

**NFR3 – Reliability**

The system must execute consistently and recover from failures:
- **Uptime:** 99.5% availability (4.4 hours downtime per month maximum)
- **Deterministic execution:** Workflows produce consistent results given same inputs
- **Error handling:** Graceful degradation (one module failure doesn't crash system)
- **Retry logic:** Failed external calls retry up to 3 times with exponential backoff
- **Backup & recovery:** Daily backups, recovery time objective (RTO) 1 hour, recovery point objective (RPO) 15 minutes
- **Circuit breakers:** If external service unavailable, fall back to stub/mock

---

**NFR4 – Performance**

The system must respond quickly to user actions:
- **Search response:** Travel search completes within 3 seconds
- **Booking confirmation:** Booking confirmed within 5 seconds
- **Report generation:** Standard reports generate within 5 seconds
- **Login:** User logged in within 2 seconds
- **UI responsiveness:** Page interactions (button click, form submission) respond within 500ms
- **Resource utilization:** System uses < 80% CPU, < 90% memory under normal load

---

**NFR5 – Usability**

The system must be intuitive and accessible to all user roles:
- **Operator interface:** Clear 5-step workflow (search → select → confirm → view → complete)
- **Manager dashboard:** Key metrics visible in < 3 clicks from login
- **Consistency:** Similar actions use similar UI patterns across modules
- **Accessibility:** WCAG 2.1 AA compliance (keyboard navigation, screen reader support, color contrast)
- **Responsive design:** Works on desktop (1920x1080), tablet (1024x768), mobile (375x667)
- **Error messages:** Clear explanations (not just error codes) with recovery suggestions

---

**NFR6 – Maintainability and Extensibility**

The system must support changes without major rework:
- **Modular architecture:** Clear module boundaries; change one module without touching others
- **SOLID principles:** Single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion
- **Code quality:** Passing static analysis (no security issues, style compliance)
- **Documentation:** Every module has architecture docs and API documentation
- **Tech-agnostic:** Interfaces defined abstractly; implementation can change (Python → Java, etc.)
- **Adapter pattern:** Real integrations plug in via adapters without core changes
- **Configuration over code:** Business rules stored in database, not hard-coded

---

**NFR7 – Observability**

The system must be fully transparent for debugging and monitoring:
- **Logging:** Every decision point logged with context (input, output, rationale)
- **Tracing:** Correlation IDs trace workflows across modules
- **Metrics:** System collects performance metrics (latency, throughput, error rate)
- **Dashboards:** Operational visibility (system health, user activity, error trends)
- **Alerting:** Critical errors trigger alerts to administrators
- **Query capability:** Logs queryable by timestamp, user, workflow, status

---

**NFR8 – Portability**

The system must run in different environments without modification:
- **OS support:** Windows, macOS, Linux (via Docker containers)
- **Database agnostic:** Code works with PostgreSQL, MySQL (via abstraction layer)
- **Deployment:** Works in Docker, Kubernetes, on-premises, cloud (AWS, GCP, Azure)
- **Minimal dependencies:** External libraries carefully selected; no monolithic frameworks
- **Configuration:** Environment-specific config via environment variables or config files
- **No hard-coded paths:** All paths and URLs configurable

---

**NFR9 – Compliance and Data Protection**

The system must comply with applicable regulations:
- **GDPR (EU):** User consent for data processing, right to be forgotten, data portability
- **DPDP Act (India):** Data processing compliance, grievance redressal mechanism
- **PCI DSS (Payment):** If integrating real payment, comply with card data protection
- **Audit trails:** Immutable logs for regulatory inspection
- **Data retention:** Clear policies (delete old logs after 12 months, keep audit logs indefinitely)
- **Privacy by design:** Minimize data collection; encrypt sensitive data

---

**NFR10 – Testing and Quality**

The system must be thoroughly tested before deployment:
- **Unit tests:** > 80% code coverage, all critical paths tested
- **Integration tests:** Module interactions tested; stubs/mocks validated
- **System tests:** End-to-end workflows tested (booking → confirmation → report)
- **Performance tests:** Load tests with 100+ concurrent users
- **Security tests:** Authentication/authorization verified; SQL injection, XSS prevented
- **UAT:** User acceptance testing with real operators before deployment

---

### 3.3 External Interface Requirements

---

**EIR1: Travel Booking APIs (Stubbed)**
- **Interface:** REST API returning available journeys
- **Stub returns:** Hardcoded list of 5-10 sample journeys per search
- **Adapter pattern:** Real booking API (e.g., Kiwi, Amadeus) replaces stub
- **Data contract:** Journey object with [id, departure, arrival, duration, price, capacity]

**EIR2: Accommodation APIs (Stubbed)**
- **Interface:** REST API returning available accommodations
- **Stub returns:** Hardcoded list of 5-10 sample hotels per location/date
- **Adapter pattern:** Real hotel API (e.g., Booking.com) replaces stub
- **Data contract:** Accommodation object with [id, name, rooms_available, price_per_night, amenities]

**EIR3: Payment Gateway (Stubbed)**
- **Interface:** REST API for payment processing
- **Stub returns:** Always returns success with mock transaction ID
- **Adapter pattern:** Real provider (Stripe, Razorpay) replaces stub
- **Data contract:** Payment request with [amount, currency, card_token], response with [transaction_id, status]

**EIR4: Notification Service (Stubbed)**
- **Interface:** REST API for sending emails, SMS, push notifications
- **Stub returns:** Always returns success with mock message ID
- **Adapter pattern:** Real provider (AWS SES, Twilio, Firebase) replaces stub
- **Data contract:** Notification request with [recipient, channel, template, variables], response with [message_id, status]

**EIR5: AI/ML Models (Stubbed)**
- **Interface:** Function calls returning recommendations
- **Stub returns:** Deterministic values (e.g., always returns 0.8 confidence)
- **Adapter pattern:** Real model (scikit-learn, TensorFlow) replaces stub
- **Data contract:** Model input with [booking_data, context], output with [recommendation, confidence, explanation]

**EIR6: Prayer Time API**
- **Interface:** REST API for prayer times by location and date
- **Stub returns:** Hardcoded prayer times for major cities
- **Adapter pattern:** Real API (Aladhan, Islamic Finder) replaces stub
- **Data contract:** Prayer request with [location, date, method], response with [fajr, dhuhr, asr, maghrib, isha times]

**EIR7: Database Interface**
- **Type:** PostgreSQL (or MySQL via abstraction)
- **Schema:** Booking, Accommodation, Resource, User, Log tables
- **Persistence:** All data persisted durably
- **Queries:** Standard CRUD operations plus complex joins for reporting

**EIR8: Logging Interface**
- **Type:** Structured JSON logs to PostgreSQL
- **Format:** { timestamp, level, user_id, module, action, context, details, status }
- **Performance:** Async writes; don't block transactions
- **Query:** SQL-based log search with filters

---

## 4. System Models and Use Cases

---

### **Use Case 1: Multi-Modal Travel Booking (Personal)**

**Actors:** Operator

**Preconditions:** Operator is logged in

**Trigger:** Operator clicks "New Booking"

**Main Flow:**
1. Operator selects travel mode (bus)
2. Operator enters origin (Delhi), destination (Agra), date (Jan 15), passengers (2)
3. System searches and returns 8 bus options
4. Operator selects option (Comfort Coach, 9:00 AM departure)
5. System validates availability (confirmed)
6. System calculates price (₹1250 × 2 = ₹2500)
7. Operator confirms booking
8. System generates booking ID (UTOP-BUS-20260115-A7K3X)
9. System logs all steps
10. Operator receives confirmation with booking ID and passenger details

**Postconditions:** Booking confirmed, resource allocated, notification sent

**Exceptions:**
- If availability check fails → return alternatives
- If payment fails → retry or offer alternative options

---

### **Use Case 2: Accommodation with Ancillary Services**

**Actors:** Operator

**Preconditions:** Travel booking exists; operator logged in

**Trigger:** Operator clicks "Add Accommodation"

**Main Flow:**
1. Operator enters destination (Agra), dates (Jan 15-17), guests (2)
2. System searches hotels (returns 6 options)
3. Operator selects Hotel Amar Palace, 3-star, ₹3000/night
4. Operator selects services: breakfast included, airport transfer
5. System calculates total (₹3000 × 2 nights = ₹6000 + breakfast ₹400 + transfer ₹500 = ₹6900)
6. Operator confirms
7. System generates accommodation ID (UTOP-ACC-20260115-K9P2M)
8. System logs booking details
9. Operator receives confirmation with check-in info

**Postconditions:** Accommodation booked, services coordinated, notification sent

---

### **Use Case 3: Religious Pilgrimage Booking**

**Actors:** Operator, Group Coordinator

**Preconditions:** Group coordinator initiated pilgrimage planning

**Trigger:** Operator clicks "Book Pilgrimage Group"

**Main Flow:**
1. Operator selects pilgrimage type (Umrah), travel dates (March 1-15)
2. System shows prayer schedule for Mecca (5 daily prayers)
3. System shows sacred site hours (Haram entry: 8 AM - 10 PM)
4. Operator confirms group can comply with schedule
5. System mandates guide assignment → assigns Guide Ahmad (Urdu/Arabic speaker)
6. System books multi-leg journey: Delhi → Jeddah → Mecca → Jeddah → Delhi
7. System books accommodation near Haram (5-star hotel)
8. System enforces group mobility (all 25 pilgrims must stay together)
9. Operator confirms
10. System generates pilgrimage ID (UTOP-PIL-UMRAH-20260301-F8X1K)
11. System sends spiritual content to pilgrims (prayer rituals, site significance)
12. System logs compliance with prayer schedules

**Postconditions:** Pilgrimage booked with guide, all constraints verified, pilgrims notified

---

### **Use Case 4: Group Travel with Cost Splitting**

**Actors:** Group Coordinator, Manager

**Preconditions:** Group created with 6 members

**Trigger:** Group coordinator clicks "Calculate Costs"

**Main Flow:**
1. System identifies shared items: transport (₹18000), accommodation (₹12000), guide (₹2000)
2. System applies cost-sharing formula (equal split)
3. System calculates per-member share:
   - Transport: ₹18000 ÷ 6 = ₹3000
   - Accommodation: ₹12000 ÷ 6 = ₹2000
   - Guide: ₹2000 ÷ 6 = ₹333
   - **Total per member: ₹5333**
4. Group member joins late (now 7 members) → system recalculates:
   - Transport: ₹18000 ÷ 7 = ₹2571
   - Accommodation: ₹12000 ÷ 7 = ₹1714
   - Guide: ₹2000 ÷ 7 = ₹286
   - **New total per member: ₹4571** (existing members get refund)
5. System sends cost breakdown to all members
6. System generates payment links
7. Members submit payments
8. System tracks payment status
9. Manager approves group departure (all paid)

**Postconditions:** Costs calculated fairly, payments collected, group ready to depart

---

### **Use Case 5: Resource Allocation Under Conflict**

**Actors:** Manager, System

**Preconditions:** 3 group bookings pending allocation; only 2 buses available

**Trigger:** System triggers resource allocation at booking completion time

**Main Flow:**
1. System detects 3 bookings, 2 buses → conflict
2. System applies manager-configured priority: [High-value-first, Group-first, VIP-first]
3. Booking A (luxury group, ₹15000): Priority 1 → Allocate Bus 1 (premium)
4. Booking B (group pilgrimage, ₹12000): Priority 2 → Allocate Bus 2 (standard)
5. Booking C (personal, ₹2000): Priority 3 → Cannot allocate (no bus available)
6. System escalates Booking C to manager with recommendation
7. Manager reviews: suggests waitlisting or alternative transport
8. Manager approves waitlist recommendation
9. System marks Booking C as waitlisted; notifies customer
10. System logs all allocation decisions and manager override

**Postconditions:** High-priority bookings allocated, low-priority escalated, manager approval recorded

---

### **Use Case 6: Analytics Report Generation**

**Actors:** Analyst

**Preconditions:** Analyst logged in; system has 1000+ bookings in database

**Trigger:** Analyst clicks "Generate Weekly Revenue Report"

**Main Flow:**
1. Analyst selects date range (past 7 days)
2. System queries booking table (timestamp, mode, revenue, category)
3. System aggregates by mode: Bus (₹85000), Train (₹120000), Plane (₹450000), Cruise (₹200000)
4. System calculates totals: ₹855000 revenue, 412 bookings, 94% utilization
5. System calculates trends: +12% vs. previous week
6. System identifies top destination: Mecca (pilgrimage, 45 bookings)
7. System generates bar chart (revenue by mode)
8. System generates table (daily breakdown)
9. System exports as PDF
10. System logs report generation (analyst, parameters, result count)

**Postconditions:** Report generated in < 3 seconds, exported successfully, logged

---

### **Use Case 7: Knowledge Module Access**

**Actors:** New Operator

**Preconditions:** Operator first day on job; logged in to system

**Trigger:** Operator makes first booking attempt; searches for help

**Main Flow:**
1. Operator clicks "Help" button on search page
2. System suggests contextual knowledge module: "How to Search for Buses"
3. Operator clicks suggestion → views 3-minute video tutorial
4. Video demonstrates: mode selection → destination entry → search execution → result interpretation
5. Operator returns to booking interface
6. Operator completes first booking
7. System logs module view + booking completion (correlation)
8. System marks "How to Search for Buses" as completed for this operator
9. System tracks learning effectiveness (operator success rate increases)

**Postconditions:** Operator trained; knowledge effectiveness measured

---

### **Use Case 8: Notification Delivery - Multi-Channel**

**Actors:** System, Operator

**Preconditions:** Booking just confirmed

**Trigger:** System completes booking confirmation

**Main Flow:**
1. System identifies notification events: booking confirmation required
2. System retrieves operator preferences: Email preferred, SMS backup
3. System generates email notification:
   - Subject: "Booking Confirmed - Delhi to Agra, Jan 15"
   - Body: Full booking details, PDF itinerary attached, "View Online" link
4. System sends email via AWS SES (adapter)
5. Email delivery confirmed in 2 seconds
6. System logs email delivery: timestamp, recipient, status
7. If email delivery fails (after 3 retries), system sends SMS:
   - "Booking confirmed: UTOP-BUS-123456. View details: https://utop.app/booking/123456"
8. System logs SMS delivery
9. System displays notification in operator's in-app message center

**Postconditions:** Operator notified via preferred channel; all notifications logged

---

### **Use Case 9: Pilgrimage Compliance Check**

**Actors:** Manager, System

**Preconditions:** Pilgrimage group departing in 1 day

**Trigger:** Manager runs "Pre-Departure Compliance Check"

**Main Flow:**
1. System pulls pilgrimage group details (25 pilgrims, Umrah, March 1-15)
2. System verifies prayer schedule compliance:
   - All hotel bookings near Haram? ✓ Yes, 5 hotels within 2 km
   - Prayer times blocked in itinerary? ✓ Yes, 5 daily prayers reserved
   - Alternative prayer locations identified? ✓ Yes, 3 backup mosques nearby
3. System verifies guide assignment:
   - Qualified Umrah guide assigned? ✓ Yes, Guide Ahmad (certified, 10+ pilgrimages)
   - Guide language capability matches group? ✓ Yes, Urdu, Arabic, English fluent
4. System verifies group cohesion:
   - All 25 pilgrims confirmed? ✓ 24 confirmed, 1 pending (health issue, dropping out)
   - Accommodate loss of 1 pilgrim? ✓ Yes, no refund needed (departure next day, no cancellation fee)
   - New group size: 24 pilgrims
5. System verifies accommodation:
   - Rooms booked near sacred sites? ✓ Yes, walking distance to Haram
   - Dietary restrictions accommodated? ✓ Yes, halal meals confirmed
   - Accessibility requirements met? ✓ Yes, wheelchair rooms reserved
6. System generates compliance report: **✓ PASS - All checks passed**
7. Manager reviews report and confirms green light for departure
8. System sends final reminders to all pilgrims

**Postconditions:** Pilgrimage compliance verified, manager approval recorded, pilgrims ready

---

### **Use Case 10: AI/ML Recommendation for Pricing Adjustment**

**Actors:** Manager, System

**Preconditions:** System analyzing demand patterns

**Trigger:** Manager checks "AI Pricing Recommendation"

**Main Flow:**
1. System collects booking data: Last 7 days shows 40% increase in Delhi-Agra buses
2. System collects inventory data: Only 2 buses left for next weekend
3. System collects competitor data: Competitor prices up 15% (from stub data)
4. System calls AI pricing recommendation model:
   - Input: [demand_increase: 0.40, inventory_tight: true, competitor_price_high: true]
   - Model output: [price_adjustment_factor: 1.15, confidence: 0.82]
5. System displays recommendation: **Suggest price increase 15% for next weekend**
   - Confidence: 82% (fairly confident)
   - Reasoning: "High demand + low inventory + competitor price increase"
   - Current price: ₹1250 → Recommended price: ₹1438
6. Manager reviews recommendation
7. Manager has options:
   - Accept (apply 15% increase immediately)
   - Modify (apply custom 10% instead)
   - Reject (keep current pricing)
8. Manager chooses "Accept"
9. System applies 15% price increase to Delhi-Agra routes for next weekend
10. System logs recommendation, manager decision, and outcome

**Postconditions:** Price adjusted; recommendation effectiveness tracked

---

### **Use Case 11: Logging and Audit Trail Inspection**

**Actors:** Compliance Officer, System

**Preconditions:** Audit required for booking UTOP-BUS-20260115-A7K3X

**Trigger:** Officer searches for booking audit trail

**Main Flow:**
1. Officer clicks "Audit Trail" for booking UTOP-BUS-20260115-A7K3X
2. System displays chronological log:
   - 09:15 AM - Operator "john.operator@utop.app" initiates search
     - Input: [mode: bus, origin: Delhi, destination: Agra, date: Jan 15, passengers: 2]
     - Output: [8 results returned]
   - 09:16 AM - Operator selects option 3 (Comfort Coach, 9:00 AM)
     - Action: Option selected; price ₹1250/pax displayed
   - 09:17 AM - System validates availability
     - Result: [seats available: 12, validation: PASS]
   - 09:18 AM - System calculates pricing
     - Calculation: [base ₹1250 × 2 passengers × 1.0 multiplier = ₹2500]
   - 09:19 AM - Operator confirms booking
     - Confirmation: [booking_id: UTOP-BUS-20260115-A7K3X, status: confirmed]
   - 09:20 AM - System generates confirmation email
     - Recipient: passenger1@email.com, passenger2@email.com
     - Status: Delivery confirmed
3. Officer verifies: Price calculation correct, all validation steps passed, no overrides
4. Officer exports audit trail as PDF for record-keeping
5. System logs audit trail access (officer, timestamp, booking audited)

**Postconditions:** Full audit trail inspected; no discrepancies; compliance verified

---

### **Use Case 12: Internationalization - Multi-Language Booking**

**Actors:** Operator (Arabic-speaking), System

**Preconditions:** Operator language preference set to Arabic (ar-SA)

**Trigger:** Operator initiates booking

**Main Flow:**
1. System loads interface in Arabic (RTL text direction)
2. All labels in Arabic: "البحث عن الرحلات" (Search Journeys), "الوجهة" (Destination)
3. Operator enters search: origin "الرياض" (Riyadh), destination "الدمام" (Dammam)
4. System returns results in Arabic with dates formatted as DD/MM/YYYY
5. System displays currency in SAR (﷼) format: "1250﷼ لكل راكب"
6. Operator selects bus; system displays prayer times in Arabic
7. System shows Islamic holiday notice (if applicable)
8. Operator confirms booking; system sends confirmation email in Arabic
9. Notification sent in Arabic with Arabic phone number format
10. System logs language preference and confirms Arabic localization successful

**Postconditions:** Entire booking in Arabic; no English text visible; cultural/religious dates respected

---

## 5. Traceability Matrix

All functional requirements traced to Phase 0 system definition:

| FR # | Requirement | Phase 0 Reference | Status |
|------|-------------|-------------------|--------|
| FR1 | Multi-modal travel booking | Core Idea - "End-to-end workflow orchestration for multi-modal travel" | ✓ Defined |
| FR2 | Accommodation coordination | Core Idea - "Coordination of accommodation and optional services" | ✓ Defined |
| FR3 | Travel category support | Core Idea - "Modular support for diverse travel categories: personal, leisure, religious, and group" | ✓ Defined |
| FR4 | Resource allocation engine | Key Objectives - "Resource allocation engine with prioritization" | ✓ Defined |
| FR5 | Knowledge sharing modules | Core Idea - "Knowledge-sharing and micro-learning modules for operational efficiency" | ✓ Defined |
| FR6 | Analytics & reporting | Core Idea - "Comprehensive logging and observability for traceability and auditing" | ✓ Defined |
| FR7 | AI/ML decision support | Core Idea - "AI/ML-powered decision support for recommendations, resource allocation, and prioritization" | ✓ Defined |
| FR8 | Comprehensive logging | Core Idea - "Comprehensive logging and observability" | ✓ Defined |
| FR9 | Multi-channel notifications | System Features - "Notifications: Email, SMS, and push notifications for updates" | ✓ Defined |
| FR10 | Internationalization | System Features - "Multilingual Support: Toggle system content across multiple languages" | ✓ Defined |
| FR11 | Pilgrimage workflows | Core Idea - "Modular support for...religious...travel categories" | ✓ Defined |
| FR12 | Group cost splitting | Core Idea - "Modular support for...group...travel categories" | ✓ Defined |

---

## 6. Acceptance Criteria Summary

System is accepted when:
- ✓ All 12 functional requirements fully implemented and testable
- ✓ All 10 non-functional requirements met (performance, security, scalability, etc.)
- ✓ All 12 use cases execute end-to-end without errors
- ✓ All external interfaces (stubs/mocks) operational and replaceable via adapters
- ✓ Comprehensive logging validates all workflow steps
- ✓ 90%+ test coverage; critical paths 100% covered
- ✓ Documentation complete (API docs, module docs, operational guides)
- ✓ Performance benchmarked and meets NFR targets
- ✓ Security audit passed; no critical vulnerabilities
- ✓ User acceptance testing passed with real operators

---

## 7. References and Related Documents

- **Phase 0:** System Definition (binding baseline)
- **Phase 2:** Feasibility & Domain Analysis (TBD)
- **Phase 3:** System Architecture & Design (ADR, HLD, LLD - forthcoming)
- **Phase 4:** Detailed Technical Specifications (forthcoming)
- **Glossary:** Section 1.4 (Definitions, Acronyms, Abbreviations)

---

## 8. Document Sign-Off and Version Control

| Version | Date | Status | Author | Changes |
|---------|------|--------|--------|---------|
| 0.1 | Jan 19, 2026 | Draft | Anil | Initial SRS draft |
| 0.5 | Jan 20, 2026 | In Review | Anil | FRs 1-8 completed |
| 1.0 | Session 2 | **LOCKED** | Claude + Anil | Complete SRS with FR9-12, all use cases, formal lock |

---

## 9. Formal Lock Statement

**This Software Requirements Specification (SRS) is now LOCKED as of this session.**

All functional requirements (FR1-FR12) are complete, validated, and binding for Phase 3 (System Architecture & Design).

**No changes permitted to this SRS without formal change request and re-review.**

Changes to requirements trigger Phase 0 re-evaluation and SRS revision cycle.

---

**END OF PHASE 1 – SYSTEM REQUIREMENTS SPECIFICATION**

**Status:** ✅ COMPLETE AND LOCKED

**Next Phase:** Phase 3 – System Architecture & Design (ADR, HLD, LLD documents)

**Approval for Proceeding to Phase 3:** Awaiting your confirmation

---
