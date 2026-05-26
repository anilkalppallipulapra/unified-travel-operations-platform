# Phase 2 – Solution Architecture

## Architectural Style
UTOP follows a layered modular architecture with clear separation between:
- API Interface Layer (`utop.api`)
- Domain Model Layer (`utop.domain`)
- Application Service/Workflow Layer (`utop.services`)
- External Adapter Layer (currently deterministic stubs)

## Key Decisions
1. **Framework-first** modules ensure extensibility for future vendor integrations.
2. **Deterministic orchestration** provides predictable outputs for validation and training.
3. **Observability by default** through centralized workflow event logging.
4. **Stub-compatible interfaces** avoid binding core logic to external providers.

## Context Diagram (Text)
- Actors: Operator, Manager, Analyst, Administrator.
- System: UTOP API + orchestration engine.
- External systems (future): booking providers, payment gateways, analytics engine, ML models.

## Component Responsibilities
- `WorkflowService`: Owns lifecycle transitions and business flow.
- `DecisionService`: Placeholder intelligence recommendations.
- `LoggingService`: Captures immutable stage events.
- FastAPI app: Exposes stable HTTP contracts.

## Quality Attribute Tactics
- Reliability: state transitions guarded by service boundaries.
- Maintainability: package modularization + typed models.
- Testability: deterministic estimators and in-memory adapters.
- Portability: Python 3.11+ and minimal runtime dependencies.
