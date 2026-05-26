# Phase 7 – Operations & Support Runbook

## Service Start/Stop
- Start: `python -m utop.main`
- Health check: `GET /health`
- Stop: SIGTERM process or orchestrator stop action.

## Incident Triage
1. Validate health endpoint.
2. Inspect recent workflow events via `/workflows/{id}/events`.
3. Check API logs for request validation failures.
4. Reproduce against deterministic test payload.

## Monitoring Baseline (Planned)
- Availability: API heartbeat and uptime.
- Reliability: workflow completion rate.
- Error budget: 4xx/5xx trend.
- Business KPI: confirmed workflows by category/mode.

## Operational SLAs (Initial)
- Service uptime objective: 99.5% (target).
- Initial response for P1 incidents: within 15 minutes.
- Incident postmortem: within 48 hours.

## Knowledge Transfer
- Keep docs synchronized with every release.
- Link runbook updates to pull request evidence.
