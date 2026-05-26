from utop.domain.models import TravelCategory, TravelMode, TravelRequest
from utop.services.decision_service import DecisionService
from utop.services.logging_service import LoggingService
from utop.services.workflow_service import WorkflowService


def build_request() -> TravelRequest:
    return TravelRequest(
        requester_id="ops-001",
        origin="JFK",
        destination="LHR",
        departure_date="2026-02-01",
        return_date="2026-02-10",
        travel_mode=TravelMode.air,
        category=TravelCategory.business,
        passengers=2,
        priority=4,
    )


def test_workflow_submit_path() -> None:
    logger = LoggingService()
    service = WorkflowService(logger=logger, decision_service=DecisionService())

    workflow = service.create_workflow(build_request())
    submitted = service.submit(workflow.workflow_id)

    assert submitted.status.value == "confirmed"
    assert submitted.recommendation is not None
    assert submitted.estimated_cost is not None
    assert len(logger.list_events(workflow.workflow_id)) == 5


def test_cancel_workflow() -> None:
    logger = LoggingService()
    service = WorkflowService(logger=logger, decision_service=DecisionService())

    workflow = service.create_workflow(build_request())
    cancelled = service.cancel(workflow.workflow_id)

    assert cancelled.status.value == "cancelled"
