# Phase 3 – Detailed Design

## Domain Entities
- **TravelRequest**: input contract for trip intent and operational constraints.
- **TravelWorkflow**: aggregate root representing end-to-end lifecycle state.
- **Recommendation**: AI/ML placeholder output with confidence and rationale.
- **WorkflowEvent**: audit event emitted at each lifecycle step.

## Lifecycle State Machine
`draft -> submitted -> priced -> allocated -> confirmed`

Alternate branch:
`any_active_state -> cancelled`

## API Design
- RESTful JSON endpoints.
- Idempotency strategy deferred to future persistence-backed release.
- Errors use 404 with descriptive detail when workflow IDs are missing.

## Validation Rules
- Passenger count range: 1..50
- Priority range: 1..5
- Enumerated mode/category values enforced by type-safe enums.

## Extension Points
- Replace in-memory logging with relational/event store adapter.
- Replace deterministic decision service with model-serving gateway.
- Add role-based authN/authZ middleware without changing domain contracts.
