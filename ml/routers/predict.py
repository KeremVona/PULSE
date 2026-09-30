"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.routers.predict
Description: Real-time cardiovascular risk prediction and explainability endpoints.
"""

from typing import Union
from fastapi import APIRouter, Depends, HTTPException, status
from ml.schemas.clinical import (
    HTTPValidationErrorResponse,
    PatientClinicalIndicators,
    PredictionRequest,
    PredictionResponse,
)
from ml.services.inference_service import InferenceService, get_inference_service

router = APIRouter(prefix="/api/v1", tags=["Clinical Inference & Explainability"])


@router.post(
    "/predict",
    response_model=PredictionResponse,
    status_code=status.HTTP_200_OK,
    summary="Predict Cardiovascular Risk & Calculate SHAP Attributions",
    description=(
        "Ingests patient clinical indicators, enforces strict physiological boundary constraints, "
        "applies robust transformations, calculates calibrated posterior risk probability with "
        "a 95% confidence interval, and generates local SHAP feature attributions."
    ),
    responses={
        200: {
            "description": "Risk prediction and explainability analysis executed successfully.",
            "model": PredictionResponse,
        },
        422: {
            "description": "Validation failure: physiological boundaries or data types violated.",
            "model": HTTPValidationErrorResponse,
        },
        500: {
            "description": "Internal inference execution failure.",
        },
    },
)
def predict_cardiovascular_risk(
    payload: PredictionRequest,
    service: InferenceService = Depends(get_inference_service),
) -> PredictionResponse:
    """
    Executes clinical risk inference and local explainability.
    """
    indicators = payload.clinical_indicators
    encounter_id = payload.encounter_id
    correlation_id = payload.correlation_id


    try:
        response = service.predict(
            indicators=indicators,
            encounter_id=encounter_id,
            correlation_id=correlation_id,
        )
        return response
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Inference execution failed: {str(e)}",
        )
