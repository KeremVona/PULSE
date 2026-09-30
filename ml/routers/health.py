"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.routers.health
Description: Health check, readiness probe, and diagnostic endpoints.
"""

from fastapi import APIRouter, Depends, Response, status
from ml.schemas.clinical import HealthCheckResponse
from ml.services.inference_service import InferenceService, get_inference_service

router = APIRouter(tags=["Diagnostics & Health"])


@router.get(
    "/health",
    response_model=HealthCheckResponse,
    status_code=status.HTTP_200_OK,
    summary="Microservice Readiness & Health Check",
    description=(
        "Returns current health status, model artifact load state, active calibration method, "
        "operational classification threshold, process RSS memory usage in MB, and service uptime."
    ),
    responses={
        200: {
            "description": "Service is healthy, fully initialized, and machine learning artifacts are loaded in memory.",
            "model": HealthCheckResponse,
        },
        503: {
            "description": "Service is degraded or unready; ML artifacts are not yet loaded.",
            "model": HealthCheckResponse,
        },
    },
)
def get_health(
    response: Response,
    service: InferenceService = Depends(get_inference_service),
) -> HealthCheckResponse:
    """Executes runtime readiness check, returning 503 if unready or degraded."""
    health = service.get_health_status()
    if not service.is_ready or health.status != "healthy":
        response.status_code = status.HTTP_503_SERVICE_UNAVAILABLE
    return health

