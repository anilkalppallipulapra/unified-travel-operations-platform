from __future__ import annotations

from utop.domain.models import TravelRequest, TravelWorkflow, WorkflowEvent, WorkflowStatus
from utop.services.decision_service import DecisionService
from utop.services.logging_service import LoggingService


class WorkflowService:
    """Core orchestration for UTOP travel lifecycle."""

    def __init__(self, logger: LoggingService, decision_service: DecisionService) -> None:
        self._logger = logger
        self._decision_service = decision_service
        self._workflows: dict[str, TravelWorkflow] = {}

    def create_workflow(self, request: TravelRequest) -> TravelWorkflow:
        workflow = TravelWorkflow(request=request)
        self._workflows[workflow.workflow_id] = workflow
        self._log(workflow.workflow_id, "draft", "Workflow created.")
        return workflow

    def submit(self, workflow_id: str) -> TravelWorkflow:
        workflow = self._get(workflow_id)
        workflow.status = WorkflowStatus.submitted
        self._log(workflow_id, "submitted", "Workflow submitted for pricing and allocation.")

        workflow.recommendation = self._decision_service.recommend(workflow.request)
        workflow.status = WorkflowStatus.priced
        workflow.estimated_cost = self._estimate_cost(workflow.request)
        self._log(workflow_id, "priced", "Pricing completed using deterministic estimator.")

        workflow.resource_allocation = self._allocate(workflow.request)
        workflow.status = WorkflowStatus.allocated
        self._log(workflow_id, "allocated", "Resources allocated from simulated providers.")

        workflow.status = WorkflowStatus.confirmed
        self._log(workflow_id, "confirmed", "Workflow confirmed and ready for operations.")
        return workflow

    def cancel(self, workflow_id: str) -> TravelWorkflow:
        workflow = self._get(workflow_id)
        workflow.status = WorkflowStatus.cancelled
        self._log(workflow_id, "cancelled", "Workflow cancelled by operator.")
        return workflow

    def get_workflow(self, workflow_id: str) -> TravelWorkflow:
        return self._get(workflow_id)

    def _get(self, workflow_id: str) -> TravelWorkflow:
        if workflow_id not in self._workflows:
            msg = f"Workflow '{workflow_id}' was not found."
            raise KeyError(msg)
        return self._workflows[workflow_id]

    def _estimate_cost(self, request: TravelRequest) -> float:
        mode_multiplier = {
            "air": 1.9,
            "rail": 1.2,
            "road": 1.0,
            "maritime": 1.5,
        }[request.travel_mode.value]
        base = 130.0 * request.passengers
        urgency_fee = request.priority * 25.0
        return round((base + urgency_fee) * mode_multiplier, 2)

    def _allocate(self, request: TravelRequest) -> dict[str, str]:
        return {
            "transport_provider": f"simulated-{request.travel_mode.value}-provider",
            "accommodation_provider": "simulated-hotel-network",
            "support_team": "regional-ops-cell",
        }

    def _log(self, workflow_id: str, stage: str, message: str) -> None:
        self._logger.record(WorkflowEvent(workflow_id=workflow_id, stage=stage, message=message))
