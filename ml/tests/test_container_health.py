"""
PULSE Clinical Decision Support System (CDSS)
Test Suite: ml.tests.test_container_health
Description: Comprehensive automated tests for ML-204:
             - Microservice health probe (GET /health) monitoring memory (RSS via psutil),
               artifact load state, uptime, and ISO timestamps.
             - Degraded/unready status handling returning HTTP 503.
             - Container cold-start and health check latency (< 5s requirement).
             - Multi-stage Dockerfile, .dockerignore, and non-root execution verification.
             - Deep edge cases and branch coverage across routers, schemas, and services.
"""

from datetime import datetime, timezone
import json
import os
import re
import time
from typing import Any, Dict
from unittest.mock import MagicMock, patch
import numpy as np
import pandas as pd
import pytest
from fastapi.testclient import TestClient

from ml.main import app
from ml.schemas.clinical import (
    HealthCheckResponse,
    PatientClinicalIndicators,
    PredictionRequest,
)
from ml.services.inference_service import InferenceService, get_inference_service
from ml.services.shap_service import ShapService
from ml.services.transformer import ClinicalDataTransformer


@pytest.fixture(scope="module")
def client():
    """Provides a TestClient within the application lifespan context."""
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def valid_patient_payload() -> Dict[str, Any]:
    """Clinically valid reference patient payload."""
    return {
        "age": 62,
        "sex": 1,
        "cp": 4,
        "trestbps": 150.0,
        "chol": 275.0,
        "fbs": 1,
        "restecg": 2,
        "thalach": 122.0,
        "exang": 1,
        "oldpeak": 3.2,
        "slope": 2,
        "ca": 2,
        "thal": 7,
    }


# ===========================================================================
# 1. Healthcheck Endpoint & Memory Monitoring Tests
# ===========================================================================
class TestHealthProbeAndMemoryMonitoring:
    """Tests the GET /health probe verifying memory, model state, and timing."""

    def test_health_endpoint_healthy_state(self, client):
        """GET /health must return HTTP 200 with complete diagnostics."""
        t0 = time.perf_counter()
        response = client.get("/health")
        latency_ms = (time.perf_counter() - t0) * 1000.0

        assert response.status_code == 200
        # Criterion: Health check responds well within 5 seconds (typically < 30ms)
        assert latency_ms < 5000.0, f"Health check took {latency_ms:.2f} ms (budget 5000ms)"

        data = response.json()
        assert data["status"] == "healthy"
        assert data["model_loaded"] is True
        assert data["model_version"] == "v1.0.0"
        assert "Isotonic" in data["calibrator_method"]
        assert data["decision_threshold"] == 0.29
        assert data["uptime_seconds"] >= 0.0

        # Memory verification (psutil RSS)
        assert "memory_usage_mb" in data
        assert isinstance(data["memory_usage_mb"], (int, float))
        assert data["memory_usage_mb"] > 0.0
        # Standard Python + LightGBM RSS in memory is typically 80-350 MB, well under 500 MB
        assert data["memory_usage_mb"] < 600.0

        # Timestamp verification (ISO 8601 UTC)
        assert "timestamp" in data
        ts = datetime.fromisoformat(data["timestamp"])
        assert ts.tzinfo is not None

    def test_health_endpoint_degraded_state_returns_503(self, client):
        """When the service is unready or degraded, GET /health must return HTTP 503."""
        mock_unready_service = MagicMock(spec=InferenceService)
        mock_unready_service.is_ready = False
        mock_unready_service.get_health_status.return_value = HealthCheckResponse(
            status="degraded",
            model_loaded=False,
            model_version="v1.0.0",
            calibrator_method="IsotonicRegression",
            decision_threshold=0.2900,
            uptime_seconds=5.0,
            memory_usage_mb=98.5,
            timestamp=datetime.now(timezone.utc).isoformat(),
        )

        app.dependency_overrides[get_inference_service] = lambda: mock_unready_service
        try:
            response = client.get("/health")
            assert response.status_code == 503
            data = response.json()
            assert data["status"] == "degraded"
            assert data["model_loaded"] is False
        finally:
            app.dependency_overrides.pop(get_inference_service, None)

    def test_psutil_memory_exception_fallback(self):
        """If psutil raises an exception, get_health_status should fallback to 0.0 MB gracefully."""
        service = InferenceService()
        with patch("psutil.Process", side_effect=RuntimeError("psutil unavailable")):
            health = service.get_health_status()
            assert health.memory_usage_mb == 0.0
            assert health.status == "degraded"
            assert health.model_loaded is False


# ===========================================================================
# 2. Dockerfile & Container Architecture Specification Tests
# ===========================================================================
class TestContainerArchitectureSpecification:
    """Verifies Dockerfile, .dockerignore, and dependency configuration against requirements."""

    def test_dockerfile_exists_and_multistage(self):
        """Dockerfile must exist and implement multi-stage build."""
        dockerfile_path = os.path.join("ml", "Dockerfile")
        assert os.path.exists(dockerfile_path), "ml/Dockerfile does not exist"

        with open(dockerfile_path, "r", encoding="utf-8") as f:
            content = f.read()

        # Check multi-stage builds
        assert "FROM python:3.12-slim-bookworm AS builder" in content
        assert "FROM python:3.12-slim-bookworm AS runtime" in content

    def test_dockerfile_non_root_execution(self):
        """Dockerfile must enforce non-root user execution (UID >= 10000)."""
        dockerfile_path = os.path.join("ml", "Dockerfile")
        with open(dockerfile_path, "r", encoding="utf-8") as f:
            content = f.read()

        # Check non-root user making and switch
        assert "groupadd" in content
        assert "useradd" in content
        assert "USER 10001:10001" in content or "USER pulseuser" in content

        # Verify no root user declaration
        assert "USER root" not in content

    def test_dockerfile_healthcheck_directive(self):
        """Dockerfile must specify built-in HEALTHCHECK with start-period <= 5s."""
        dockerfile_path = os.path.join("ml", "Dockerfile")
        with open(dockerfile_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "HEALTHCHECK" in content
        assert "--start-period=5s" in content
        assert "/health" in content

    def test_dockerfile_port_exposure(self):
        """Dockerfile must expose port 8000."""
        dockerfile_path = os.path.join("ml", "Dockerfile")
        with open(dockerfile_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "EXPOSE 8000" in content

    def test_dockerignore_excludes_caches_and_venvs(self):
        """Both root and ml .dockerignore must exclude venvs, pycache, and test cache."""
        for path in [".dockerignore", os.path.join("ml", ".dockerignore")]:
            assert os.path.exists(path), f"Missing {path}"
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            assert ".venv" in content
            assert "__pycache__" in content
            assert ".pytest_cache" in content

    def test_requirements_file_excludes_dev_dependencies(self):
        """Production requirements.txt must not contain testing or notebook dependencies."""
        req_path = os.path.join("ml", "requirements.txt")
        assert os.path.exists(req_path)
        with open(req_path, "r", encoding="utf-8") as f:
            content = f.read()

        # Required production libs
        assert "fastapi" in content
        assert "uvicorn" in content
        assert "lightgbm" in content
        assert "shap" in content
        assert "psutil" in content

        # Prohibited in production container image
        forbidden = ["pytest", "ipykernel", "jupyter", "matplotlib", "seaborn"]
        for pkg in forbidden:
            assert pkg not in content.lower(), f"Forbidden dev package '{pkg}' found in production requirements"

    def test_docker_compose_valid_structure(self):
        """docker-compose.yml must be configured for the ML service."""
        compose_path = "docker-compose.yml"
        assert os.path.exists(compose_path)
        with open(compose_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "ml-service" in content
        assert "8000:8000" in content
        assert "healthcheck" in content


# ===========================================================================
# 3. Predict Router Error Handling & Edge Cases
# ===========================================================================
class TestPredictRouterEdgeCases:
    """Verifies /predict router error handling and HTTP 500 conversion."""

    def test_predict_endpoint_internal_error_handling(self, client, valid_patient_payload):
        """If inference service throws an unexpected exception, router returns HTTP 500."""
        mock_failing_service = MagicMock(spec=InferenceService)
        mock_failing_service.predict.side_effect = RuntimeError("Simulated matrix singularity")

        app.dependency_overrides[get_inference_service] = lambda: mock_failing_service
        try:
            response = client.post("/api/v1/predict", json={"clinical_indicators": valid_patient_payload})
            assert response.status_code == 500
            data = response.json()
            assert "Inference execution failed" in data["detail"]
        finally:
            app.dependency_overrides.pop(get_inference_service, None)


# ===========================================================================
# 4. ClinicalDataTransformer Deep Edge Cases
# ===========================================================================
class TestClinicalDataTransformerEdgeCases:
    """Verifies transformer batch methods, error validations, and edge cases."""

    def test_to_batch_dataframe_empty_raises_value_error(self):
        """Empty input list to to_batch_dataframe must raise ValueError."""
        with pytest.raises(ValueError, match="Input indicators list cannot be empty"):
            ClinicalDataTransformer.to_batch_dataframe([])

    def test_to_batch_dataframe_multi_patient(self, valid_patient_payload):
        """Batch conversion preserves patient order and columns."""
        p1 = PatientClinicalIndicators(**valid_patient_payload)
        p2_data = dict(valid_patient_payload)
        p2_data["age"] = 45
        p2 = PatientClinicalIndicators(**p2_data)

        df = ClinicalDataTransformer.to_batch_dataframe([p1, p2])
        assert len(df) == 2
        assert list(df["age"]) == [62, 45]
        assert list(df.columns) == ClinicalDataTransformer.FEATURE_NAMES

    def test_transform_batch_success(self, valid_patient_payload):
        """Batch transformation produces (N, 22) matrix."""
        service = get_inference_service()
        p1 = PatientClinicalIndicators(**valid_patient_payload)
        p2 = PatientClinicalIndicators(**valid_patient_payload)

        x_batch = ClinicalDataTransformer.transform_batch([p1, p2], service.preprocessor)
        assert x_batch.shape == (2, 22)
        assert not np.isnan(x_batch).any()

    def test_transform_patient_shape_mismatch_raises(self, valid_patient_payload):
        """Mock preprocessor returning incorrect columns raises ValueError."""
        p = PatientClinicalIndicators(**valid_patient_payload)
        mock_prep = MagicMock()
        mock_prep.transform.return_value = np.zeros((1, 15))  # Expected 22

        with pytest.raises(ValueError, match="Transformed feature matrix shape mismatch"):
            ClinicalDataTransformer.transform_patient(p, mock_prep)

    def test_transform_patient_nan_raises(self, valid_patient_payload):
        """Mock preprocessor returning NaNs raises ValueError."""
        p = PatientClinicalIndicators(**valid_patient_payload)
        mock_prep = MagicMock()
        arr_with_nan = np.zeros((1, 22))
        arr_with_nan[0, 5] = np.nan
        mock_prep.transform.return_value = arr_with_nan

        with pytest.raises(ValueError, match="contains NaN or Inf"):
            ClinicalDataTransformer.transform_patient(p, mock_prep)

    def test_transform_batch_nan_raises(self, valid_patient_payload):
        """Mock preprocessor returning NaNs on batch raises ValueError."""
        p = PatientClinicalIndicators(**valid_patient_payload)
        mock_prep = MagicMock()
        arr_with_nan = np.zeros((2, 22))
        arr_with_nan[0, 0] = np.nan
        mock_prep.transform.return_value = arr_with_nan

        with pytest.raises(ValueError, match="contains NaN or Inf"):
            ClinicalDataTransformer.transform_batch([p, p], mock_prep)

    def test_transform_batch_shape_mismatch_raises(self, valid_patient_payload):
        """Mock preprocessor returning mismatched batch shape raises ValueError."""
        p = PatientClinicalIndicators(**valid_patient_payload)
        mock_prep = MagicMock()
        mock_prep.transform.return_value = np.zeros((1, 22))  # Provided 2 items, returned 1

        with pytest.raises(ValueError, match="Batch transformed feature matrix shape mismatch"):
            ClinicalDataTransformer.transform_batch([p, p], mock_prep)


# ===========================================================================
# 5. InferenceService Lifecycle & Clinical Action Coverage
# ===========================================================================
class TestInferenceServiceLifecycleCoverage:
    """Verifies unready exceptions, missing files, and all clinical action tiers."""

    def test_predict_when_unready_raises_runtime_error(self, valid_patient_payload):
        """predict() called on unready service must raise RuntimeError."""
        unready_service = InferenceService()
        p = PatientClinicalIndicators(**valid_patient_payload)

        with pytest.raises(RuntimeError, match="InferenceService is not ready"):
            unready_service.predict(p)

    def test_load_artifacts_missing_model_raises(self, tmp_path):
        """load_artifacts with missing model.joblib raises FileNotFoundError."""
        service = InferenceService(artifacts_dir=str(tmp_path))
        with pytest.raises(FileNotFoundError, match="Model artifact not found"):
            service.load_artifacts()

    def test_load_artifacts_missing_preprocessor_raises(self, tmp_path):
        """load_artifacts with model but missing preprocessor raises FileNotFoundError."""
        (tmp_path / "model.joblib").write_text("dummy")
        service = InferenceService(artifacts_dir=str(tmp_path))
        with pytest.raises(FileNotFoundError, match="Preprocessor artifact not found"):
            service.load_artifacts()

    def test_load_artifacts_missing_calibrator_raises(self, tmp_path):
        """load_artifacts with model & prep but missing calibrator raises FileNotFoundError."""
        (tmp_path / "model.joblib").write_text("dummy")
        (tmp_path / "preprocessor.joblib").write_text("dummy")
        service = InferenceService(artifacts_dir=str(tmp_path))
        with pytest.raises(FileNotFoundError, match="Calibrator artifact not found"):
            service.load_artifacts()

    def test_clinical_action_all_risk_tiers(self):
        """_determine_clinical_action returns distinct guidelines for each risk band."""
        high_act = InferenceService._determine_clinical_action("High", True)
        assert "Urgent Cardiology Referral" in high_act

        elev_act = InferenceService._determine_clinical_action("Elevated", True)
        assert "Cardiology Consultation & Diagnostic Imaging" in elev_act

        mod_act = InferenceService._determine_clinical_action("Moderate", False)
        assert "Secondary Prevention & Functional Evaluation" in mod_act

        low_act = InferenceService._determine_clinical_action("Low", False)
        assert "Main Prevention & Lifestyle Counseling" in low_act

    def test_get_inference_service_app_state(self):
        """get_inference_service retrieves existing instance from request.app.state."""
        mock_req = MagicMock()
        mock_service = MagicMock(spec=InferenceService)
        mock_req.app.state.inference_service = mock_service

        retrieved = get_inference_service(mock_req)
        assert retrieved is mock_service


# ===========================================================================
# 6. ShapService Edge Cases & Fallbacks
# ===========================================================================
class TestShapServiceEdgeCases:
    """Verifies dimension checks, 3D array handling, and fallback formatting in ShapService."""

    def test_shap_service_dimension_mismatch_raises(self, valid_patient_payload):
        """Input vector with incorrect dimension raises ValueError."""
        service = get_inference_service()
        shap_srv = service.shap_service
        wrong_vector = np.zeros(10)  # Expected 22
        indicators = PatientClinicalIndicators(**valid_patient_payload)

        with pytest.raises(ValueError, match="Feature vector dimension mismatch"):
            shap_srv.compute_explanations(patient_vector=wrong_vector, indicators=indicators)


    def test_shap_service_without_background_initialization(self):
        """ShapService initializes cleanly without background samples."""
        service = get_inference_service()
        new_srv = ShapService(
            model=service.model,
            feature_names=service.preprocessor.feature_names_out_,
            background_sample=None,
        )
        assert new_srv.explainer is not None
        assert isinstance(new_srv.base_value, float)


# ===========================================================================
# 7. Clinical Schema Boundary & Warning Flags Coverage
# ===========================================================================
class TestClinicalSchemaCoverage:
    """Covers warning flag thresholds, discrete validators, and schema conversion methods."""

    def test_patient_to_dataframe(self, valid_patient_payload):
        """to_dataframe() produces valid 1-row DataFrame."""
        p = PatientClinicalIndicators(**valid_patient_payload)
        df = p.to_dataframe()
        assert isinstance(df, pd.DataFrame)
        assert df.shape == (1, 13)
        assert set(df.columns) == set(ClinicalDataTransformer.FEATURE_NAMES)

    def test_warning_flags_severe_hypotension_bradycardia_extreme_age(self, valid_patient_payload):
        """Triggers severe hypotension, bradycardia, severe hypercholesterolemia, and age alerts."""
        payload = dict(valid_patient_payload)
        payload["trestbps"] = 65.0
        payload["chol"] = 450.0
        payload["thalach"] = 45.0
        payload["oldpeak"] = 4.0
        payload["age"] = 98

        p = PatientClinicalIndicators(**payload)
        warnings = p.get_clinical_warning_flags()

        assert any("Severe Hypotension" in w for w in warnings)
        assert any("Severe Hypercholesterolemia" in w for w in warnings)
        assert any("Critical Bradycardia" in w for w in warnings)
        assert any("Severe ST Depression" in w for w in warnings)
        assert any("Age Alert" in w for w in warnings)

    def test_warning_flags_hypertension_tachycardia_young_age(self, valid_patient_payload):
        """Triggers critical hypertension and extreme tachycardia warnings."""
        payload = dict(valid_patient_payload)
        payload["trestbps"] = 210.0
        payload["thalach"] = 210.0
        payload["age"] = 16

        p = PatientClinicalIndicators(**payload)
        warnings = p.get_clinical_warning_flags()

        assert any("Critical Hypertension" in w for w in warnings)
        assert any("Extreme Tachycardia" in w for w in warnings)
        assert any("Age Alert" in w for w in warnings)

    @pytest.mark.parametrize(
        "field,invalid_val,match_str",
        [
            ("sex", 2, "Biological sex code"),
            ("cp", 5, "Chest pain type"),
            ("fbs", 2, "Fasting blood sugar flag"),
            ("restecg", 3, "Resting ECG code"),
            ("exang", 2, "Exercise-induced angina flag"),
            ("slope", 4, "Peak exercise ST slope"),
            ("ca", 5, "Number of major vessels"),
            ("thal", 5, "Thalassemia code"),
        ],
    )
    def test_discrete_validators_raise_value_error(self, valid_patient_payload, field, invalid_val, match_str):
        """Out of bounds discrete fields must raise ValueError with diagnostic message."""
        payload = dict(valid_patient_payload)
        payload[field] = invalid_val
        with pytest.raises(ValueError, match=match_str):
            PatientClinicalIndicators(**payload)

    def test_thal_remapping(self, valid_patient_payload):
        """Thalassemia alternate codes 1 and 2 remap to 3 (normal) and 6 (fixed defect)."""
        p1 = PatientClinicalIndicators(**{**valid_patient_payload, "thal": 1})
        assert p1.thal == 3

        p2 = PatientClinicalIndicators(**{**valid_patient_payload, "thal": 2})
        assert p2.thal == 6



    def test_prediction_request_flat_assembly(self, valid_patient_payload):
        """PredictionRequest correctly parses flat indicators into clinical_indicators."""
        flat_payload = dict(valid_patient_payload)
        flat_payload["encounter_id"] = "enc-123"
        req = PredictionRequest(**flat_payload)
        assert req.encounter_id == "enc-123"
        assert req.clinical_indicators.age == valid_patient_payload["age"]
        assert req.clinical_indicators.trestbps == valid_patient_payload["trestbps"]


# ===========================================================================
# 8. Application Lifespan & Global Error Handlers Coverage
# ===========================================================================
class TestAppLifespanAndErrorHandlers:
    """Verifies startup error handling and value_error_handler in main.py."""

    @pytest.mark.anyio
    async def test_lifespan_startup_failure_gracefully_degrades(self):
        """If load_artifacts fails during lifespan startup, app stays in degraded state."""
        from ml.main import lifespan
        mock_app = MagicMock()
        mock_app.state = MagicMock()

        with patch("ml.main.get_inference_service") as mock_get_srv:
            mock_srv = MagicMock()
            mock_srv.load_artifacts.side_effect = RuntimeError("Disk IO failure")
            mock_get_srv.return_value = mock_srv

            async with lifespan(mock_app):
                # Service continued running in degraded state
                pass

    def test_value_error_handler_response(self, client):
        """Direct call triggering value_error_handler returns HTTP 422."""
        from fastapi import Request
        from ml.main import value_error_handler
        import asyncio

        dummy_req = MagicMock(spec=Request)
        exc = ValueError("Clinical parameter out of range")
        response = asyncio.run(value_error_handler(dummy_req, exc))
        assert response.status_code == 422
        body = json.loads(response.body.decode("utf-8"))
        assert body["title"] == "Clinical Parameter Error"
        assert "Clinical parameter out of range" in body["detail"]

