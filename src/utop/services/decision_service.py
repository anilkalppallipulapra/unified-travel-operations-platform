from __future__ import annotations

from utop.domain.models import Recommendation, TravelRequest


class DecisionService:
    """AI/ML placeholder service with deterministic recommendation logic."""

    def recommend(self, request: TravelRequest) -> Recommendation:
        if request.priority >= 4:
            return Recommendation(
                strategy="time",
                confidence=0.81,
                rationale="High-priority trip; optimize for shortest feasible itinerary.",
            )

        if request.passengers >= 8:
            return Recommendation(
                strategy="cost",
                confidence=0.76,
                rationale="Large group size indicates strong economies-of-scale sensitivity.",
            )

        return Recommendation(
            strategy="balanced",
            confidence=0.72,
            rationale="Default blended optimization for standard operational requests.",
        )
