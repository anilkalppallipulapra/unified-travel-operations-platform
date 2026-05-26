from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Literal
from uuid import uuid4


class TravelMode(str, Enum):
    air = "air"
    rail = "rail"
    road = "road"
    maritime = "maritime"


class TravelCategory(str, Enum):
    personal = "personal"
    leisure = "leisure"
    group = "group"
    religious = "religious"
    business = "business"


class WorkflowStatus(str, Enum):
    draft = "draft"
    submitted = "submitted"
    priced = "priced"
    allocated = "allocated"
    confirmed = "confirmed"
    cancelled = "cancelled"


@dataclass(slots=True)
class TravelRequest:
    requester_id: str
    origin: str
    destination: str
    departure_date: str
    return_date: str | None
    travel_mode: TravelMode
    category: TravelCategory
    passengers: int
    priority: int = 3


@dataclass(slots=True)
class Recommendation:
    strategy: Literal["cost", "time", "balanced"]
    confidence: float
    rationale: str


@dataclass(slots=True)
class WorkflowEvent:
    workflow_id: str
    stage: str
    message: str
    metadata: dict[str, str | int | float] = field(default_factory=dict)
    event_id: str = field(default_factory=lambda: str(uuid4()))
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))


@dataclass(slots=True)
class TravelWorkflow:
    request: TravelRequest
    workflow_id: str = field(default_factory=lambda: str(uuid4()))
    status: WorkflowStatus = WorkflowStatus.draft
    recommendation: Recommendation | None = None
    estimated_cost: float | None = None
    resource_allocation: dict[str, str] = field(default_factory=dict)
