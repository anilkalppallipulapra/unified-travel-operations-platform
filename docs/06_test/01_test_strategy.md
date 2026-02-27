# Phase 5 – Test Strategy & Verification

## Test Levels
- **Unit tests:** domain/service deterministic behavior.
- **Integration tests:** API endpoints and workflow execution.
- **Contract tests (future):** provider adapter request/response schemas.

## Current Automated Tests
- Happy-path workflow creation and submission.
- Event timeline integrity.
- Unknown workflow handling with 404 behavior.

## Quality Gates
- `pytest` must pass.
- `ruff check .` must pass.
- CI pipeline validates on pull requests and main branch pushes.

## Future Test Expansion
- Property-based tests for cost estimator invariants.
- Performance and load tests for concurrent workflow operations.
- Security tests for authentication, authorization, and input abuse.
