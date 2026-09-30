"""
PULSE Clinical Decision Support System (CDSS)
Test Suite: ml.tests.test_shap_service
Description: Comprehensive unit and integration tests for ML-203:
             - Real-Time SHAP Attribution Engine (ShapService)
             - Feature label mapper converting technical keys to clinical terminology
             - Directional attribution classification (RISK_INCREASING vs PROTECTIVE)
             - Mathematical additivity verification (f(x) = E[f(x)] + sum(phi_i))
             - Acceptance Criteria: explanations array on /predict and < 25 ms calculation overhead
"""

import time
from typing import Any, Dict
import numpy as np
import pytest
from fastapi.testclient import TestClient

from ml.main import app
from ml.schemas.clinical import FeatureAttribution, PatientClinicalIndicators, PredictionResponse
from ml.services.inference_service import get_inference_service
from ml.services.shap_service import CLINICAL_FEATURE_LABELS, ShapService
from ml.services.transformer import ClinicalDataTransformer


@pytest.fixture(scope="module")
def client():
    """Provides a TestClient within the application lifespan context."""
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def sample_patient_indicators() -> PatientClinicalIndicators:
    """Returns a validated clinical patient instance."""
    return PatientClinicalIndicators(
        age=58,
        sex=0,
        cp=4,
        trestbps=145.0,
        chol=268.0,
        fbs=1,
        restecg=1,
        thalach=115.0,
        exang=1,
        oldpeak=2.8,
        slope=2,
        ca=2,
        thal=7,
    )


@pytest.fixture
def low_risk_patient_indicators() -> PatientClinicalIndicators:
    """Returns a young asymptomatic patient with protective markers."""
    return PatientClinicalIndicators(
        age=36,
        sex=0,
        cp=2,
        trestbps=110.0,
        chol=165.0,
        fbs=0,
        restecg=0,
        thalach=178.0,
        exang=0,
        oldpeak=0.0,
        slope=1,
        ca=0,
        thal=3,
    )


# ===========================================================================
# 1. Feature Label Mapper & Clinical Formatting Tests
# ===========================================================================
class TestFeatureLabelMapperAndFormatting:
    """Verifies conversion of technical model variables into clinician-readable presentation."""

    def test_clinical_label_mapping_coverage(self, client):
        """Verifies that all 22 transformed features have clear clinical definitions."""
        service = get_inference_service()
        feature_names = service.preprocessor.feature_names_out_
        assert len(feature_names) == 22

        for key in feature_names:
            assert key in CLINICAL_FEATURE_LABELS, f"Feature key '{key}' missing from CLINICAL_FEATURE_LABELS"
            label = CLINICAL_FEATURE_LABELS[key]
            assert isinstance(label, str)
            assert len(label) > 3

    def test_specific_clinical_translations(self):
        """Validates specific domain translations requested in ML-203 deliverables."""
        assert CLINICAL_FEATURE_LABELS["oldpeak"] == "ST Depression"
        assert CLINICAL_FEATURE_LABELS["ca"] == "Major Vessels Calcified"
        assert CLINICAL_FEATURE_LABELS["cp_4"] == "Asymptomatic Chest Pain"
        assert CLINICAL_FEATURE_LABELS["cp_1"] == "Typical Anginal Chest Pain"
        assert CLINICAL_FEATURE_LABELS["thal_7"] == "Reversible Thallium Defect"
        assert CLINICAL_FEATURE_LABELS["thal_3"] == "Normal Thallium Perfusion"
        assert CLINICAL_FEATURE_LABELS["thalach"] == "Max Exercise Heart Rate"

    def test_patient_value_clinical_formatting(self, client, sample_patient_indicators):
        """Verifies that patient measurements display with proper medical units and descriptors."""
        service = get_inference_service()

        x_trans = ClinicalDataTransformer.transform_patient(
            sample_patient_indicators, service.preprocessor
        )

        # Continuous formatting
        assert ShapService.format_patient_value("oldpeak", sample_patient_indicators, x_trans, 4) == "2.8 mm"
        assert ShapService.format_patient_value("thalach", sample_patient_indicators, x_trans, 3) == "115 bpm"
        assert ShapService.format_patient_value("trestbps", sample_patient_indicators, x_trans, 1) == "145 mmHg"
        assert ShapService.format_patient_value("chol", sample_patient_indicators, x_trans, 2) == "268 mg/dL"
        assert ShapService.format_patient_value("ca", sample_patient_indicators, x_trans, 21) == "2 vessel(s)"
        assert ShapService.format_patient_value("age", sample_patient_indicators, x_trans, 0) == "58 yrs"

        # Categorical binary formatting
        assert ShapService.format_patient_value("sex", sample_patient_indicators, x_trans, 18) == "Female"
        assert ShapService.format_patient_value("exang", sample_patient_indicators, x_trans, 20) == "Yes (Present)"
        assert ShapService.format_patient_value("fbs", sample_patient_indicators, x_trans, 19) == "> 120 mg/dL"

        # One-hot encoded category formatting
        cp_4_idx = service.preprocessor.feature_names_out_.index("cp_4")
        assert ShapService.format_patient_value("cp_4", sample_patient_indicators, x_trans, cp_4_idx) == "Present (Observed)"
        cp_1_idx = service.preprocessor.feature_names_out_.index("cp_1")
        assert ShapService.format_patient_value("cp_1", sample_patient_indicators, x_trans, cp_1_idx) == "Absent"


# ===========================================================================
# 2. ShapService Core Engine & Directional Attribution Tests
# ===========================================================================
class TestShapServiceCoreEngine:
    """Verifies ShapService mathematical additivity and directional classification."""

    def test_shap_service_initialization(self, client):
        service = get_inference_service()
        assert service.shap_service is not None
        assert isinstance(service.shap_service, ShapService)
        assert len(service.shap_service.feature_names) == 22
        assert isinstance(service.shap_service.base_value, float)

    def test_compute_explanations_structure(self, client, sample_patient_indicators):
        service = get_inference_service()
        x_trans = ClinicalDataTransformer.transform_patient(
            sample_patient_indicators, service.preprocessor
        )

        res = service.shap_service.compute_explanations(
            patient_vector=x_trans[0],
            indicators=sample_patient_indicators,
            top_n_risk=5,
            top_n_protective=3,
        )

        assert "top_risk_drivers" in res
        assert "top_protective_factors" in res
        assert "explanations" in res
        assert "all_attributions" in res
        assert "additivity_verified" in res
        assert res["additivity_verified"] is True
        assert res["additivity_error"] <= 1e-5

        # Check top risk drivers are strictly RISK_INCREASING
        assert len(res["top_risk_drivers"]) <= 5
        for item in res["top_risk_drivers"]:
            assert item.impact_direction == "RISK_INCREASING"
            assert item.shap_value > 0.0

        # Check top protective factors are strictly PROTECTIVE
        assert len(res["top_protective_factors"]) <= 3
        for item in res["top_protective_factors"]:
            assert item.impact_direction == "PROTECTIVE"
            assert item.shap_value < 0.0

        # Check all 22 explanations have sequential ranks 1 to 22
        assert len(res["explanations"]) == 22
        ranks = [item.rank for item in res["explanations"]]
        assert ranks == list(range(1, 23))

    def test_mathematical_additivity_across_diverse_patients(
        self, client, sample_patient_indicators, low_risk_patient_indicators
    ):
        """Verifies sum(phi_i) + base_value matches raw LightGBM margin within 10^-5."""
        service = get_inference_service()

        for patient in [sample_patient_indicators, low_risk_patient_indicators]:
            x_trans = ClinicalDataTransformer.transform_patient(patient, service.preprocessor)
            res = service.shap_service.compute_explanations(x_trans[0], patient)

            assert res["additivity_verified"] is True
            assert res["additivity_error"] <= 1e-5
            reconstructed = res["base_expected_value"] + sum(
                attr.shap_value for attr in res["all_attributions"]
            )
            assert abs(reconstructed - res["raw_margin_prediction"]) <= 1e-4


# ===========================================================================
# 3. Acceptance Criteria Verification on POST /api/v1/predict
# ===========================================================================
class TestShapAcceptanceCriteria:
    """
    Acceptance Criteria:
    1. Every /predict response includes an explanations array containing feature name,
       raw value, SHAP contribution value, and directional impact flag (RISK_INCREASING or PROTECTIVE).
    2. Calculation execution adds no more than 25 ms overhead to the baseline prediction.
    """

    def test_predict_response_contains_explanations_array(self, client, sample_patient_indicators):
        """
        Acceptance Criteria 1:
        Verifies that /predict response contains the full explanations array with all required fields.
        """
        response = client.post("/api/v1/predict", json=sample_patient_indicators.model_dump())
        assert response.status_code == 200
        data = response.json()

        assert "explanations" in data
        assert isinstance(data["explanations"], list)
        assert len(data["explanations"]) == 22

        for expl in data["explanations"]:
            assert "feature_key" in expl and isinstance(expl["feature_key"], str)
            assert "feature_label" in expl and len(expl["feature_label"]) > 0
            assert "patient_value" in expl and len(expl["patient_value"]) > 0
            assert "shap_value" in expl and isinstance(expl["shap_value"], float)
            assert "impact_direction" in expl
            assert expl["impact_direction"] in ["RISK_INCREASING", "PROTECTIVE", "NEUTRAL"]
            assert "rank" in expl and 1 <= expl["rank"] <= 22

        # Verify top risk and protective breakdowns
        assert len(data["top_risk_drivers"]) <= 5
        assert len(data["top_protective_factors"]) <= 3

    def test_shap_calculation_adds_under_25ms_overhead(self, client, sample_patient_indicators):
        """
        Acceptance Criteria 2:
        Calculation execution adds no more than 25 ms overhead to the baseline prediction.
        """
        service = get_inference_service()
        x_trans = ClinicalDataTransformer.transform_patient(
            sample_patient_indicators, service.preprocessor
        )

        # Measure baseline prediction (without SHAP computation) over 20 iterations
        baseline_times = []
        for _ in range(20):
            t0 = time.perf_counter()
            _ = service.calibrator.predict_with_uncertainty(x_trans)
            baseline_times.append((time.perf_counter() - t0) * 1000.0)

        # Measure SHAP computation overhead over 20 iterations
        shap_overhead_times = []
        for _ in range(20):
            t0 = time.perf_counter()
            _ = service.shap_service.compute_explanations(
                patient_vector=x_trans[0],
                indicators=sample_patient_indicators,
            )
            shap_overhead_times.append((time.perf_counter() - t0) * 1000.0)

        median_baseline = float(np.median(baseline_times))
        median_shap_overhead = float(np.median(shap_overhead_times))

        print(
            f"\n[SHAP Overhead Benchmark] Over 20 iterations:\n"
            f"Median Baseline Prediction Latency: {median_baseline:.2f} ms\n"
            f"Median SHAP Calculation Overhead: {median_shap_overhead:.2f} ms (Target < 25.0 ms)\n"
            f"Headroom below 25 ms budget: {25.0 - median_shap_overhead:.2f} ms"
        )

        # Acceptance Criteria Assertion
        assert (
            median_shap_overhead < 25.0
        ), f"SHAP overhead {median_shap_overhead:.2f} ms exceeded 25 ms budget"
