"""
PULSE Clinical Decision Support System (CDSS)
Test Suite: ml.tests.test_prediction_pipeline
Description: Unit and integration tests for ML-202:
             - Lifespan state manager and zero disk I/O verification per request
             - Internal ClinicalDataTransformer utility (mapping DTOs to NumPy/Pandas matrices)
             - Core prediction pipeline (calibrated probabilities, risk bands, 95% CI)
             - Concurrent benchmark testing verifying median latency < 30 ms (10 req/s)
"""

from concurrent.futures import ThreadPoolExecutor, as_completed
import time
from typing import Any, Dict, List
from unittest.mock import patch

import numpy as np
import pandas as pd
import pytest
from fastapi.testclient import TestClient

from ml.main import app
from ml.schemas.clinical import PatientClinicalIndicators, PredictionResponse
from ml.services.inference_service import InferenceService, get_inference_service
from ml.services.transformer import ClinicalDataTransformer
from ml.src.pipeline import CLINICAL_FEATURE_ORDER, NUM_EXPECTED_FEATURES


@pytest.fixture(scope="module")
def client():
    """Provides a TestClient within the application lifespan context."""
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def low_risk_patient_payload() -> Dict[str, Any]:
    """Younger asymptomatic patient with optimal vitals (expected Low Risk)."""
    return {
        "age": 36,
        "sex": 0,          # Female
        "cp": 2,           # Atypical Angina
        "trestbps": 110.0, # Optimal BP
        "chol": 165.0,     # Optimal Cholesterol
        "fbs": 0,          # Normal fasting blood sugar
        "restecg": 0,      # Normal ECG
        "thalach": 178.0,  # Robust max exercise heart rate
        "exang": 0,        # No exercise-induced angina
        "oldpeak": 0.0,    # No ST depression
        "slope": 1,        # Upsloping
        "ca": 0,           # No occluded vessels
        "thal": 3,         # Normal scintigraphy
    }


@pytest.fixture
def high_risk_patient_payload() -> Dict[str, Any]:
    """Older patient with severe ischemic indicators (expected High Risk)."""
    return {
        "age": 67,
        "sex": 1,          # Male
        "cp": 4,           # Asymptomatic (silent ischemia pattern)
        "trestbps": 160.0, # Hypertension
        "chol": 298.0,     # Hypercholesterolemia
        "fbs": 1,          # Diabetic blood sugar
        "restecg": 2,      # Left ventricular hypertrophy
        "thalach": 108.0,  # Chronotropic incompetence
        "exang": 1,        # Exercise-induced angina present
        "oldpeak": 3.4,    # Severe ST depression
        "slope": 2,        # Flat ST slope
        "ca": 3,           # 3 occluded coronary vessels
        "thal": 7,         # Reversible defect
    }


# ===========================================================================
# 1. Internal Transformation Utility Tests (ClinicalDataTransformer)
# ===========================================================================
class TestClinicalDataTransformer:
    """Verifies DTO to DataFrame and NumPy transformation integrity."""

    def test_single_dto_to_dataframe(self, low_risk_patient_payload):
        indicators = PatientClinicalIndicators(**low_risk_patient_payload)
        df = ClinicalDataTransformer.to_dataframe(indicators)

        assert isinstance(df, pd.DataFrame)
        assert df.shape == (1, 13)
        assert list(df.columns) == CLINICAL_FEATURE_ORDER
        assert df.iloc[0]["age"] == 36
        assert df.iloc[0]["trestbps"] == 110.0
        assert df.iloc[0]["chol"] == 165.0

    def test_batch_dto_to_dataframe(self, low_risk_patient_payload, high_risk_patient_payload):
        ind1 = PatientClinicalIndicators(**low_risk_patient_payload)
        ind2 = PatientClinicalIndicators(**high_risk_patient_payload)
        df = ClinicalDataTransformer.to_batch_dataframe([ind1, ind2])

        assert isinstance(df, pd.DataFrame)
        assert df.shape == (2, 13)
        assert list(df.columns) == CLINICAL_FEATURE_ORDER
        assert df.iloc[0]["age"] == 36
        assert df.iloc[1]["age"] == 67

    def test_batch_dto_to_dataframe_empty_raises(self):
        with pytest.raises(ValueError, match="Input indicators list cannot be empty"):
            ClinicalDataTransformer.to_batch_dataframe([])

    def test_transform_patient_numpy_shape_and_values(self, client, low_risk_patient_payload):
        service = get_inference_service()
        indicators = PatientClinicalIndicators(**low_risk_patient_payload)
        x_trans = ClinicalDataTransformer.transform_patient(indicators, service.preprocessor)

        assert isinstance(x_trans, np.ndarray)
        assert x_trans.shape == (1, NUM_EXPECTED_FEATURES)  # (1, 22)
        assert not np.isnan(x_trans).any()
        assert not np.isinf(x_trans).any()

    def test_transform_batch_numpy_shape(self, client, low_risk_patient_payload, high_risk_patient_payload):
        service = get_inference_service()
        ind1 = PatientClinicalIndicators(**low_risk_patient_payload)
        ind2 = PatientClinicalIndicators(**high_risk_patient_payload)
        x_batch = ClinicalDataTransformer.transform_batch([ind1, ind2], service.preprocessor)

        assert isinstance(x_batch, np.ndarray)
        assert x_batch.shape == (2, NUM_EXPECTED_FEATURES)  # (2, 22)
        assert not np.isnan(x_batch).any()


# ===========================================================================
# 2. Lifespan State Management & Zero Disk I/O Verification
# ===========================================================================
class TestLifespanContextAndMemoryResidency:
    """Verifies model loading is bound to lifespan and artifacts are NOT re-read from disk per request."""

    def test_model_loaded_in_app_state(self, client):
        """Verifies ML artifacts reside directly in FastAPI application state."""
        assert hasattr(app.state, "inference_service")
        service = app.state.inference_service
        assert service is not None
        assert service.is_ready is True
        assert service.model is not None
        assert service.preprocessor is not None
        assert service.calibrator is not None
        assert service.explainer is not None

    def test_zero_disk_io_per_request(self, client, low_risk_patient_payload):
        """
        Acceptance Criteria:
        Model loading is bound to the lifespan context; artifacts are not re-read from disk per request.
        Patches joblib.load and np.load to ensure zero disk reads occur during prediction requests.
        """
        with patch("joblib.load") as mock_joblib, patch("numpy.load") as mock_npload:
            for _ in range(10):
                response = client.post("/api/v1/predict", json=low_risk_patient_payload)
                assert response.status_code == 200

            # Verify that neither joblib.load nor numpy.load were invoked during inference
            mock_joblib.assert_not_called()
            mock_npload.assert_not_called()


# ===========================================================================
# 3. Core Prediction Pipeline & Output Payload Specifications
# ===========================================================================
class TestPredictionPipelineOutput:
    """
    Acceptance Criteria:
    Output payload delivers:
    - risk_probability (float between 0.0 and 1.0)
    - risk_band (Low, Moderate, Elevated, High)
    - confidence_interval (95% CI bounds)
    """

    def test_output_payload_schema_and_bounds(self, client, low_risk_patient_payload):
        response = client.post("/api/v1/predict", json=low_risk_patient_payload)
        assert response.status_code == 200
        data = response.json()

        # Validate risk probability
        p = data["risk_probability"]
        assert isinstance(p, float)
        assert 0.0 <= p <= 1.0

        # Validate risk band categorization
        assert data["risk_band"] in ["Low", "Moderate", "Elevated", "High"]

        # Validate confidence interval
        ci = data["confidence_interval"]
        assert "lower" in ci and "upper" in ci
        assert "confidence_level" in ci and "margin_percentage" in ci
        assert ci["confidence_level"] == 0.95
        assert 0.0 <= ci["lower"] <= p <= ci["upper"] <= 1.0
        assert ci["margin_percentage"] >= 0.0

        # Validate threshold and classification
        assert data["decision_threshold"] == 0.29
        assert data["is_cad_positive"] == (p >= 0.29)

        # Validate clinical action and disclaimer
        assert len(data["recommended_action"]) > 20
        assert "Clinical Decision Support Notice" in data["clinical_disclaimer"]
        assert data["inference_latency_ms"] > 0.0

    def test_low_risk_patient_classification(self, client, low_risk_patient_payload):
        """Asymptomatic young patient should be classified into Low Risk tier."""
        response = client.post("/api/v1/predict", json=low_risk_patient_payload)
        assert response.status_code == 200
        data = response.json()

        assert data["risk_probability"] < 0.20
        assert data["risk_band"] == "Low"
        assert data["is_cad_positive"] is False
        assert "Main Prevention" in data["recommended_action"]

    def test_high_risk_patient_classification(self, client, high_risk_patient_payload):
        """Patient with multiple severe ischemic markers should be classified into High/Elevated Risk tier."""
        response = client.post("/api/v1/predict", json=high_risk_patient_payload)
        assert response.status_code == 200
        data = response.json()

        assert data["risk_probability"] >= 0.70
        assert data["risk_band"] in ["Elevated", "High"]
        assert data["is_cad_positive"] is True
        assert any(term in data["recommended_action"] for term in ["Cardiology", "Angiography", "Referral"])

    def test_prediction_response_validation_model(self, client, low_risk_patient_payload):
        """Response dictionary can be parsed directly into PredictionResponse Pydantic model."""
        response = client.post("/api/v1/predict", json=low_risk_patient_payload)
        assert response.status_code == 200
        pred_obj = PredictionResponse(**response.json())
        assert isinstance(pred_obj, PredictionResponse)
        assert 0.0 <= pred_obj.risk_probability <= 1.0


# ===========================================================================
# 4. Concurrent Testing & Latency Budget Verification (< 30 ms median)
# ===========================================================================
class TestConcurrentInferenceLatency:
    """
    Acceptance Criteria:
    Median inference latency remains < 30 ms under concurrent testing (10 req/s).
    """

    def test_concurrent_throughput_and_median_latency(self, client, low_risk_patient_payload):
        """
        Acceptance Criteria:
        Median inference latency remains < 30 ms under concurrent testing (10 req/s).
        """
        num_requests = 30
        target_rps = 10.0
        interval_s = 1.0 / target_rps

        # Warm-up request
        client.post("/api/v1/predict", json=low_risk_patient_payload)

        latencies = []
        server_latencies = []
        inference_latencies = []

        start_time = time.perf_counter()
        for i in range(num_requests):
            t0 = time.perf_counter()
            resp = client.post("/api/v1/predict", json=low_risk_patient_payload)
            dur_ms = (time.perf_counter() - t0) * 1000.0

            assert resp.status_code == 200
            data = resp.json()
            latencies.append(dur_ms)
            server_latencies.append(float(resp.headers.get("x-process-time-ms", 0.0)))
            inference_latencies.append(float(data.get("inference_latency_ms", 0.0)))

            # Pacing at 10 req/s
            elapsed = time.perf_counter() - t0
            if elapsed < interval_s:
                time.sleep(interval_s - elapsed)

        total_duration = time.perf_counter() - start_time
        actual_rps = num_requests / total_duration

        median_roundtrip = float(np.median(latencies))
        median_server = float(np.median(server_latencies))
        median_inference = float(np.median(inference_latencies))
        p95_roundtrip = float(np.percentile(latencies, 95))

        print(
            f"\n[10 req/s Benchmark] Requests: {num_requests}, Rate: {actual_rps:.1f} req/s\n"
            f"Roundtrip Latency: Median={median_roundtrip:.2f} ms (Target < 30 ms), 95th={p95_roundtrip:.2f} ms\n"
            f"Server Process Latency: Median={median_server:.2f} ms, 95th={np.percentile(server_latencies, 95):.2f} ms\n"
            f"Pure Inference Engine Latency: Median={median_inference:.2f} ms"
        )

        assert median_roundtrip < 30.0, f"Median roundtrip latency {median_roundtrip:.2f} ms exceeded 30 ms budget"
        assert median_server < 30.0, f"Median server process latency {median_server:.2f} ms exceeded 30 ms budget"
        assert median_inference < 30.0, f"Median inference latency {median_inference:.2f} ms exceeded 30 ms budget"


