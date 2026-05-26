from __future__ import annotations

from collections import defaultdict

from utop.domain.models import WorkflowEvent


class LoggingService:
    """In-memory logging adapter with deterministic behavior for demonstrations/tests."""

    def __init__(self) -> None:
        self._events: dict[str, list[WorkflowEvent]] = defaultdict(list)

    def record(self, event: WorkflowEvent) -> None:
        self._events[event.workflow_id].append(event)

    def list_events(self, workflow_id: str) -> list[WorkflowEvent]:
        return self._events.get(workflow_id, [])

    def list_all(self) -> list[WorkflowEvent]:
        return [event for events in self._events.values() for event in events]
