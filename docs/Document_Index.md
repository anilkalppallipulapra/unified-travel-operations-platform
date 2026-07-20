# UTOP Document Index
**Project**: Unified Travel Operations Platform  
**Repository**: `anilkalppallipulapra/unified-travel-operations-platform`  
**Purpose**: Master reference mapping document IDs to filenames, locations, and descriptions. Use this if you are picking up this project and need to find any artifact referenced in the codebase or other documents.

---

## How to Read This Index

Documents are referenced throughout the codebase and LLD artifacts using short IDs such as `UTOP-ARCH-003` or `UTOP-LLD-BOOKING-001`. This index maps every ID to its actual filename and folder so you can find it without searching the repository.

---

## Architecture Documents

All architecture documents live in:
```
docs/architecture/
```

| Document ID | Filename | Description |
|---|---|---|
| UTOP-ARCH-001 | `01_architecture_decisions.md` | Architecture Decision Records (ADRs) — all major technology and design decisions with rationale |
| UTOP-ARCH-002 | `02_high_level_design.md` | High-Level Design — component overview, system context, deployment topology, non-functional requirements |
| UTOP-ARCH-003 | `03_domain_models.md` | Domain Models — initial aggregate definitions, value objects, entity relationships across all bounded contexts. Note: some definitions superseded by stabilization artifacts; see correction tables in LLD documents |
| UTOP-ARCH-004 | `04_aggregate_invariants.md` | Aggregate Invariants — named invariants (BK-INV-*, etc.) enforced by each aggregate; referenced by LLD test suites |
| UTOP-ARCH-005 | `05_state_machines.md` | State Machines — legal and forbidden state transitions for all aggregates with guard conditions |
| UTOP-ARCH-006 | `06_consistency_concurrency.md` | Distributed Consistency and Concurrency Semantics — outbox/inbox patterns, saga coordination, idempotency, optimistic concurrency rules |
| UTOP-ARCH-007 | `07_event_contract_governance.md` | Event Contract Governance — domain vs integration event distinction, canonical envelope schema, event ownership register, versioning, PII redaction, retention policy |
| UTOP-ARCH-008 | `08_context_ownership_matrix.md` | Context Ownership Matrix — 13 bounded contexts with owns/publishes/consumes/forbidden boundaries and source-of-truth declarations |
| UTOP-ARCH-009 | `09-temporal-semantics.md` | Temporal Semantics — UTC storage rules, IClock implementations, timezone ownership, business-day semantics, DST handling, prayer schedule precision |
| UTOP-ARCH-010 | `10-shared-kernel-governance.md` | Shared Kernel Governance — admission criteria, forbidden items, approved contents, versioning rules, anti-bloat policy, extraction criteria |
| UTOP-ARCH-011 | `11_solution_structure.md` | Solution Structure — full backend + frontend folder tree, locked at Phase 5 implementation kickoff |

> **Note on filename inconsistency**: ARCH-001 through ARCH-008 use underscore separators (`01_architecture_decisions.md`). ARCH-009 and ARCH-010 use hyphen separators (`09-temporal-semantics.md`). This is a known cosmetic inconsistency introduced when those documents were generated. It does not affect content or references.

---

## Low-Level Design Documents

All LLD documents live in:
```
docs/design/
```

| Document ID | Filename | Context | Description |
|---|---|---|---|
| UTOP-LLD-BOOKING-001 | `UTOP-LLD-ACCOMMODATION-001.md` | Booking | Aggregate design, value objects, domain events, command handlers, port interfaces, PostgreSQL schema, EF Core configuration, integration events, test strategy |
| UTOP-LLD-ACCOMMODATION-001 | `UTOP-LLD-ACCOMMODATION-001.md` | Accommodation | *(Planned — not yet produced)* |
| UTOP-LLD-RESOURCEALLOCATION-001 | `lld_resource_allocation.md` | ResourceAllocation | *(Planned — not yet produced)* |
| UTOP-LLD-TRAVELCATEGORY-001 | `lld_travel_category.md` | TravelCategory | *(Planned — not yet produced)* |
| UTOP-LLD-PILGRIMAGE-001 | `lld_pilgrimage.md` | Pilgrimage | *(Planned — not yet produced)* |
| UTOP-LLD-GROUP-001 | `lld_group.md` | GroupManagement | *(Planned — not yet produced)* |
| UTOP-LLD-COSTSPLITTING-001 | `lld_cost_splitting.md` | CostSplitting | *(Planned — not yet produced)* |
| UTOP-LLD-NOTIFICATIONS-001 | `lld_notifications.md` | Notifications | *(Planned — not yet produced)* |
| UTOP-LLD-KNOWLEDGEBASE-001 | `lld_knowledge_base.md` | KnowledgeBase | *(Planned — not yet produced)* |
| UTOP-LLD-ANALYTICS-001 | `lld_analytics.md` | Analytics | *(Planned — not yet produced)* |
| UTOP-LLD-AI-001 | `lld_ai_recommendation.md` | AIRecommendation | *(Planned — not yet produced)* |
| UTOP-LLD-IDENTITY-001 | `lld_identity.md` | Identity | *(Planned — not yet produced)* |
| UTOP-LLD-LOCALIZATION-001 | `lld_localization.md` | Localization | *(Planned — not yet produced)* |
| UTOP-LLD-MASTER-001 | `lld_master.md` | All contexts | Master LLD — cross-context dependency map, full integration event register, Shared Kernel final state, data ownership map, infrastructure topology, implementation sequencing. Produced last, after all context LLDs are complete. |

---

## Tracked Open Items

Open items are tracked by ID in the LLD documents. This table shows where each tracked item lives.

| Item ID | Originating Document | Description |
|---|---|---|
| UTOP-LLD-LOCALTIME-01 | UTOP-ARCH-009, UTOP-LLD-BOOKING-001 | `LocalizedTime` type-system enforcement — private members or interface abstraction; deferred to Localization LLD |
| UTOP-LLD-BK-01 | UTOP-LLD-BOOKING-001 | PII encryption at rest for passenger `first_name`, `last_name`, `document_number` |
| UTOP-LLD-BK-02 | UTOP-LLD-BOOKING-001 | Refund amount derivation — deferred to CostSplitting LLD |
| UTOP-LLD-BK-03 | UTOP-LLD-BOOKING-001 | Manager escalation resolution — deferred to Identity/manager workflow LLD |
| UTOP-LLD-BK-04 | UTOP-LLD-BOOKING-001 | Outbox processor — shared infrastructure; applies platform-wide |
| UTOP-LLD-BK-05 | UTOP-LLD-BOOKING-001 | `row_version` concurrency — EF Core integer token vs PostgreSQL `xmin`; implementation decision |

---

## Repository Tags

Tags mark the completion of each major phase. If you are resuming the project, check out the latest tag to understand the last stable state.

| Tag | Marks |
|---|---|
| `v1.3-stabilization-invariants` | Phase 3 stabilization — aggregate invariants complete |
| `v1.4-stabilization-consistency` | Phase 3 stabilization — consistency and concurrency complete |
| `v2.0-phase3-stabilization-complete` | Phase 3 stabilization — all 10 architecture artifacts locked. **LLD entry gate.** |
| `v2.1-lld-booking-complete` | *(Planned)* LLD — Booking context complete |

---

## Branching Convention

| Branch pattern | Purpose |
|---|---|
| `main` | Stable, reviewed, tagged artifacts only |
| `feature/lld` | All LLD context documents — single branch, merged to main on phase completion |

---

## Key Cross-References for Anyone Resuming

If you are reading the code and see a reference you do not recognise, use this guide:

| You see | Go to |
|---|---|
| `// BK-INV-010` or similar invariant codes | UTOP-ARCH-004 `04_aggregate_invariants.md` |
| `// BK-TINV-001` or temporal invariant codes | UTOP-ARCH-004 `04_aggregate_invariants.md` and UTOP-ARCH-009 `09-temporal-semantics.md` |
| State transition comments (`CONFIRMED → ALLOCATED`) | UTOP-ARCH-005 `05_state_machines.md` |
| `ARCH-008 FORBIDDEN` comments | UTOP-ARCH-008 `08_context_ownership_matrix.md` |
| `IClock`, `FakeClock`, `SystemClock` | UTOP-ARCH-009 `09-temporal-semantics.md` §3 |
| `Money`, `CorrelationId`, `PassengerCount`, `Location` | UTOP-ARCH-010 `10-shared-kernel-governance.md` §5 |
| Outbox pattern, saga coordination | UTOP-ARCH-006 `06_consistency_concurrency.md` |
| Integration event routing keys | UTOP-ARCH-007 `07_event_contract_governance.md` |
| `UTOP-LLD-BK-*` open item references | UTOP-LLD-BOOKING-001 `lld/lld_booking.md` §14 |

---

*Maintained by: UTOP Architecture Board*  
*Last updated: Solution Structure document updated at Architecture folder*  
*Update this file whenever a new document is produced or an open item is resolved.*
