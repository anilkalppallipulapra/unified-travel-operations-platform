from __future__ import annotations

from dataclasses import asdict

from utop.domain.models import TravelRequest
from utop.services.decision_service import DecisionService
from utop.services.logging_service import LoggingService
from utop.services.workflow_service import WorkflowService

_logger = LoggingService()
_workflow_service = WorkflowService(logger=_logger, decision_service=DecisionService())


def health() -> dict[str, str]:
    return {"status": "ok", "service": "utop"}


def create_workflow(request: TravelRequest) -> dict[str, object]:
    return {"workflow": asdict(_workflow_service.create_workflow(request))}


def submit_workflow(workflow_id: str) -> dict[str, object]:
    return {"workflow": asdict(_workflow_service.submit(workflow_id))}


def cancel_workflow(workflow_id: str) -> dict[str, object]:
    return {"workflow": asdict(_workflow_service.cancel(workflow_id))}


def get_workflow(workflow_id: str) -> dict[str, object]:
    return {"workflow": asdict(_workflow_service.get_workflow(workflow_id))}


def get_events(workflow_id: str) -> dict[str, object]:
    return {"events": [asdict(e) for e in _logger.list_events(workflow_id)]}
