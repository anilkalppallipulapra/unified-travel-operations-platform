# Unified Travel Operations Platform (UTOP)

Enterprise-grade travel operations platform delivered with a full SDLC baseline: planning, requirements, architecture, implementation, testing, release, and operations artifacts.

## Quick Start

```bash
PYTHONPATH=src pytest -q
python -m utop.main
```

## Repository Structure

- `src/utop/` - Application source code (domain, services, API contracts).
- `tests/` - Automated test suite.
- `docs/` - SDLC documentation across all phases.
- `.github/workflows/ci.yml` - CI quality pipeline.

## SDLC Documentation Index

1. Phase 0: System Overview - `docs/01_system_overview.md`
2. Phase 1: SRS - `docs/02_System_Requirements_Specification.md`
3. Phase 2: Architecture - `docs/03_architecture/01_solution_architecture.md`
4. Phase 3: Detailed Design - `docs/04_design/01_detailed_design.md`
5. Phase 4: Implementation Plan - `docs/05_implementation/01_implementation_plan.md`
6. Phase 5: Test Strategy - `docs/06_test/01_test_strategy.md`
7. Phase 6: Release Plan - `docs/07_release/01_release_and_devops.md`
8. Phase 7: Operations Runbook - `docs/08_operations/01_operations_runbook.md`

## Core Workflow Interface

- `health()`
- `create_workflow(request)`
- `submit_workflow(workflow_id)`
- `cancel_workflow(workflow_id)`
- `get_workflow(workflow_id)`
- `get_events(workflow_id)`
