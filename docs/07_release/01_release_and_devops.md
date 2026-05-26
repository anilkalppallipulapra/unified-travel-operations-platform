# Phase 6 – Release & DevOps Plan

## Versioning
- Semantic versioning (`MAJOR.MINOR.PATCH`).
- Initial release: `0.1.0` (baseline framework).

## CI/CD Flow
1. Lint and test in GitHub Actions.
2. Build package artifact.
3. Deploy to target environment (future staging/prod pipelines).

## Deployment Targets (Planned)
- Containerized API service.
- Managed relational database for workflow persistence.
- Centralized logging/metrics stack.

## Release Checklist
- [ ] Changelog updated.
- [ ] Test evidence captured.
- [ ] Security review completed.
- [ ] Rollback instructions verified.

## Rollback Approach
- Tag immutable artifacts per release.
- Re-deploy previous known-good image and schema revision.
