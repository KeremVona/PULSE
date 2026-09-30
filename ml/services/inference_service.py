"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.services.inference_service
Description: Service layer managing lifecycle, artifact loading, model inference,
             uncertainty estimation, and SHAP explainability.
"""

from typing import Any, Dict, List, Optional
from datetime import datetime, timezone
import os
import time
import json
import logging
import joblib
import numpy as np
import pandas as pd
import psutil

from ml.schemas.clinical import (
    ConfidenceInterval,
    FeatureAttribution,
    HealthCheckResponse,
    PatientClinicalIndicators,
    PredictionResponse,
)
from ml.src.calibration import CalibratedRiskModel, classify_risk_band
from ml.src.explainer import ClinicalShapExplainer
from ml.src.pipeline import PULSEPreprocessor
from ml.services.shap_service import ShapService
from ml.services.transformer import ClinicalDataTransformer


logger = logging.getLogger("pulse.inference_service")



class InferenceService:
    """
    Singleton service managing the machine learning runtime:
    - Preprocessor (RobustScaler + OneHotEncoder)
    - Calibrated LightGBM model
    - SHAP TreeExplainer
    - Health & readiness metrics
    """

    def __init__(self, artifacts_dir: str = "ml/artifacts"):
        self.artifacts_dir = artifacts_dir
        self.start_time = time.time()
        self.is_ready = False

        self.model: Optional[Any] = None
        self.preprocessor: Optional[PULSEPreprocessor] = None
        self.calibrator: Optional[CalibratedRiskModel] = None
        self.background_sample: Optional[np.ndarray] = None
        self.shap_service: Optional[ShapService] = None
        self.explainer: Optional[Any] = None
        self.manifest: Dict[str, Any] = {}
        self.model_config: Dict[str, Any] = {}

        self.decision_threshold: float = 0.2900
        self.model_version: str = "v1.0.0"
        self.calibration_method: str = "IsotonicRegression"

    def load_artifacts(self) -> None:
        """
        Loads all serialized artifacts from disk during service startup.
        Ensures sub-500ms cold start load time.
        """
        t0 = time.perf_counter()
        logger.info(f"Loading PULSE CDSS production artifacts from {self.artifacts_dir}...")

        model_path = os.path.join(self.artifacts_dir, "model.joblib")
        prep_path = os.path.join(self.artifacts_dir, "preprocessor.joblib")
        calib_path = os.path.join(self.artifacts_dir, "calibrator.joblib")
        bg_path = os.path.join(self.artifacts_dir, "background_sample.npy")
        manifest_path = os.path.join(self.artifacts_dir, "production_manifest.json")
        config_path = os.path.join(self.artifacts_dir, "calibrated_model_config.json")

        if not os.path.exists(model_path):
            raise FileNotFoundError(f"Model artifact not found at {model_path}")
        if not os.path.exists(prep_path):
            raise FileNotFoundError(f"Preprocessor artifact not found at {prep_path}")
        if not os.path.exists(calib_path):
            raise FileNotFoundError(f"Calibrator artifact not found at {calib_path}")

        # 1. Load Preprocessor and Model
        self.preprocessor = joblib.load(prep_path)
        self.model = joblib.load(model_path)
        self.calibrator = joblib.load(calib_path)

        # 2. Load Configuration and Manifest if available
        if os.path.exists(config_path):
            with open(config_path, "r", encoding="utf-8") as f:
                self.model_config = json.load(f)
                self.decision_threshold = float(self.model_config.get("optimal_threshold", 0.2900))
                self.calibration_method = self.model_config.get("calibration_method", "IsotonicRegression")

        if os.path.exists(manifest_path):
            with open(manifest_path, "r", encoding="utf-8") as f:
                self.manifest = json.load(f)
                self.model_version = self.manifest.get("model_version", "v1.0.0")

        # 3. Load Background Sample and Initialize ShapService
        if os.path.exists(bg_path):
            self.background_sample = np.load(bg_path)
        else:
            self.background_sample = None

        feature_names = self.preprocessor.feature_names_out_
        self.shap_service = ShapService(
            model=self.model,
            feature_names=feature_names,
            background_sample=self.background_sample,
        )
        self.explainer = self.shap_service


        self.is_ready = True
        elapsed_ms = (time.perf_counter() - t0) * 1000.0
        logger.info(
            f"PULSE CDSS artifacts loaded successfully in {elapsed_ms:.1f} ms. "
            f"Threshold={self.decision_threshold:.4f}, Method={self.calibration_method}"
        )

    def get_health_status(self) -> HealthCheckResponse:
        """Returns microservice readiness, memory usage, and health metrics."""
        uptime = time.time() - self.start_time
        try:
            process = psutil.Process()
            mem_mb = round(process.memory_info().rss / (1024.0 * 1024.0), 2)
        except Exception:
            mem_mb = 0.0

        timestamp = datetime.now(timezone.utc).isoformat()

        return HealthCheckResponse(
            status="healthy" if self.is_ready else "degraded",
            model_loaded=self.is_ready,
            model_version=self.model_version,
            calibrator_method=self.calibration_method,
            decision_threshold=self.decision_threshold,
            uptime_seconds=round(uptime, 1),
            memory_usage_mb=mem_mb,
            timestamp=timestamp,
        )

    @staticmethod
    def _determine_clinical_action(risk_band: str, is_cad_positive: bool) -> str:
        """Guideline-directed clinical intervention recommendation."""
        if risk_band == "High":
            return (
                "Urgent Cardiology Referral: High estimated probability of obstructive CAD. "
                "Recommend prompt diagnostic catheterization / invasive coronary angiography (ICA), "
                "continuous hemodynamic telemetry, and urgent specialist consultation."
            )
        elif risk_band == "Elevated":
            return (
                "Cardiology Consultation & Diagnostic Imaging: Elevated cardiovascular risk profile. "
                "Recommend anatomical evaluation via Coronary Computed Tomography Angiography (CCTA) or "
                "stress echocardiography, alongside intensive guideline-directed statin and antiplatelet therapy."
            )
        elif risk_band == "Moderate":
            return (
                "Secondary Prevention & Functional Evaluation: Moderate risk stratification. "
                "Recommend non-invasive functional exercise treadmill testing (ETT), aggressive LDL-C lowering, "
                "blood pressure management, and scheduled 6-month clinical follow-up."
            )
        else:
            return (
                "Main Prevention & Lifestyle Counseling: Low short-term cardiovascular risk. "
                "Recommend maintenance of cardiovascular fitness, DASH/Mediterranean dietary patterns, "
                "annual preventive screening, and re-stratification upon symptom evolution."
            )


    def predict(
        self,
        indicators: PatientClinicalIndicators,
        encounter_id: Optional[str] = None,
        correlation_id: Optional[str] = None,
    ) -> PredictionResponse:
        """
        Executes end-to-end clinical inference:
        1. Validates and prepares tabular patient feature frame
        2. Applies preprocessor transforms
        3. Predicts calibrated posterior probability and 95% CI
        4. Calculates SHAP feature attributions
        5. Formulates clinical recommendations and alert warnings
        """
        if not self.is_ready or self.calibrator is None or self.explainer is None:
            raise RuntimeError("InferenceService is not ready or models are not loaded.")

        t0 = time.perf_counter()

        # 1. Convert validated Pydantic model to DataFrame matching training format
        df_patient = ClinicalDataTransformer.to_dataframe(indicators)

        # 2. Transform via robust preprocessor
        x_trans = ClinicalDataTransformer.transform_patient(indicators, self.preprocessor)

        # 3. Calibrated Risk & Uncertainty Estimation
        pred_results = self.calibrator.predict_with_uncertainty(x_trans, confidence_level=0.95)
        res = pred_results[0]

        # 4. Local Explainability (Real-Time SHAP Attribution Engine)
        explanation = self.shap_service.compute_explanations(
            patient_vector=x_trans[0],
            indicators=indicators,
            top_n_risk=5,
            top_n_protective=3,
        )

        top_risk = explanation["top_risk_drivers"]
        top_protective = explanation["top_protective_factors"]
        explanations = explanation["explanations"]
        all_attrs = explanation["all_attributions"]

        # 5. Clinical Warnings & Actionable Recommendation
        warnings = indicators.get_clinical_warning_flags()
        recommended_action = self._determine_clinical_action(res["risk_band"], res["is_cad_positive"])

        total_latency_ms = (time.perf_counter() - t0) * 1000.0

        return PredictionResponse(
            encounter_id=encounter_id,
            correlation_id=correlation_id,
            risk_probability=res["risk_probability"],
            risk_band=res["risk_band"],
            is_cad_positive=res["is_cad_positive"],
            decision_threshold=res["decision_threshold"],
            confidence_interval=ConfidenceInterval(**res["confidence_interval"]),
            top_risk_drivers=top_risk,
            top_protective_factors=top_protective,
            all_attributions=all_attrs,
            explanations=explanations,
            clinical_warnings=warnings,
            recommended_action=recommended_action,
            inference_latency_ms=round(total_latency_ms, 2),
        )



# Global singleton instance (fallback when not running inside FastAPI lifespan)
_inference_service_instance: Optional[InferenceService] = None


def get_inference_service(request: Optional[Any] = None) -> InferenceService:
    """
    Provides dependency injection accessor for the InferenceService.
    Prefers the instance stored in request.app.state during the FastAPI lifespan,
    falling back to a global singleton for standalone scripts and tests.
    """
    global _inference_service_instance
    if request is not None and hasattr(request, "app") and hasattr(request.app, "state"):
        service = getattr(request.app.state, "inference_service", None)
        if service is not None:
            return service

    if _inference_service_instance is None:
        _inference_service_instance = InferenceService()
        _inference_service_instance.load_artifacts()
    elif not _inference_service_instance.is_ready:
        _inference_service_instance.load_artifacts()

    return _inference_service_instance


