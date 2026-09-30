"""
PULSE Clinical Decision Support System (CDSS)
Test Suite: ml.tests.test_api_schemas
Description: Unit and integration tests for FastAPI scaffolding, Pydantic schemas,
             physiological boundary enforcement, and OpenAPI documentation generation.
"""

from typing import Any, Dict
import pytest
from fastapi.testclient import TestClient

from ml.main import app
from ml.schemas.clinical import (
    HealthCheckResponse,
    HTTPValidationErrorResponse,
    PatientClinicalIndicators,
    PredictionRequest,
    PredictionResponse,
)


@pytest.fixture(scope="module")
def client():
    """Provides a TestClient within the application lifespan context."""
    with TestClient(app) as test_client:
        yield test_client


@pytest.fixture
def valid_patient_payload() -> Dict[str, Any]:
    """Returns a clinically plausible reference patient payload."""
    return {
        "age": 58,
        "sex": 0,
        "cp": 4,
        "trestbps": 145.0,
        "chol": 268.0,
        "fbs": 1,
        "restecg": 1,
        "thalach": 115.0,
        "exang": 1,
        "oldpeak": 2.8,
        "slope": 2,
        "ca": 2,
        "thal": 7,
    }


# ===========================================================================
# 1. Physiological Boundary Validation Tests (Pydantic Schema Level)
# ===========================================================================
class TestPhysiologicalBoundaryValidation:
    """Verifies that out-of-range clinical parameters fail validation with clear messages."""

    def test_valid_patient_schema_instantiation(self, valid_patient_payload):
        """Valid indicators should instantiate without error."""
        patient = PatientClinicalIndicators(**valid_patient_payload)
        assert patient.age == 58
        assert patient.trestbps == 145.0
        assert patient.chol == 268.0
        assert patient.thalach == 115.0
        assert patient.oldpeak == 2.8
        assert patient.ca == 2
        assert patient.thal == 7

    def test_trestbps_below_physiological_limit(self, valid_patient_payload):
        """Resting BP < 60 mmHg must raise ValueError."""
        data = dict(valid_patient_payload, trestbps=50.0)
        with pytest.raises(ValueError, match="Resting Blood Pressure .* violates physiological limits"):
            PatientClinicalIndicators(**data)

    def test_trestbps_above_physiological_limit(self, valid_patient_payload):
        """Resting BP > 260 mmHg must raise ValueError."""
        data = dict(valid_patient_payload, trestbps=275.0)
        with pytest.raises(ValueError, match="Resting Blood Pressure .* violates physiological limits"):
            PatientClinicalIndicators(**data)

    def test_thalach_below_physiological_limit(self, valid_patient_payload):
        """Max heart rate < 40 bpm must raise ValueError."""
        data = dict(valid_patient_payload, thalach=35.0)
        with pytest.raises(ValueError, match="Maximum heart rate achieved .* must be between 40 and 220 bpm"):
            PatientClinicalIndicators(**data)

    def test_thalach_above_physiological_limit(self, valid_patient_payload):
        """Max heart rate > 220 bpm (e.g. 245 bpm) must raise ValueError."""
        data = dict(valid_patient_payload, thalach=245.0)
        with pytest.raises(ValueError, match="Maximum heart rate achieved .* must be between 40 and 220 bpm"):
            PatientClinicalIndicators(**data)

    def test_cholesterol_below_physiological_limit(self, valid_patient_payload):
        """Serum cholesterol < 80 mg/dL must raise ValueError."""
        data = dict(valid_patient_payload, chol=65.0)
        with pytest.raises(ValueError, match="Serum Cholesterol .* must be within plausible limits"):
            PatientClinicalIndicators(**data)

    def test_cholesterol_above_physiological_limit(self, valid_patient_payload):
        """Serum cholesterol > 600 mg/dL must raise ValueError."""
        data = dict(valid_patient_payload, chol=650.0)
        with pytest.raises(ValueError, match="Serum Cholesterol .* must be within plausible limits"):
            PatientClinicalIndicators(**data)

    def test_oldpeak_negative_limit(self, valid_patient_payload):
        """ST depression < 0.0 mm must raise ValueError."""
        data = dict(valid_patient_payload, oldpeak=-1.5)
        with pytest.raises(ValueError, match="ST depression .* must range between 0.0 and 6.5 mm"):
            PatientClinicalIndicators(**data)

    def test_oldpeak_above_physiological_limit(self, valid_patient_payload):
        """ST depression > 6.5 mm must raise ValueError."""
        data = dict(valid_patient_payload, oldpeak=7.2)
        with pytest.raises(ValueError, match="ST depression .* must range between 0.0 and 6.5 mm"):
            PatientClinicalIndicators(**data)

    def test_ca_above_physiological_limit(self, valid_patient_payload):
        """Major vessels > 3 must raise ValueError."""
        data = dict(valid_patient_payload, ca=4)
        with pytest.raises(ValueError, match="Number of major vessels .* must be 0, 1, 2, or 3"):
            PatientClinicalIndicators(**data)

    def test_thalassemia_invalid_code(self, valid_patient_payload):
        """Invalid thal code must raise ValueError."""
        data = dict(valid_patient_payload, thal=5)
        with pytest.raises(ValueError, match="Thalassemia code .* is invalid"):
            PatientClinicalIndicators(**data)

    def test_thalassemia_code_normalization(self, valid_patient_payload):
        """1-based coding (1, 2) normalizes to standard UCI codes (3, 6) while 3, 6, 7 are retained."""
        p1 = PatientClinicalIndicators(**dict(valid_patient_payload, thal=1))
        assert p1.thal == 3
        p2 = PatientClinicalIndicators(**dict(valid_patient_payload, thal=2))
        assert p2.thal == 6
        p3 = PatientClinicalIndicators(**dict(valid_patient_payload, thal=7))
        assert p3.thal == 7


    def test_age_out_of_bounds(self, valid_patient_payload):
        """Age < 1 or > 110 must raise ValueError."""
        data_infant = dict(valid_patient_payload, age=0)
        with pytest.raises(ValueError, match="Patient age .* violates physiological plausibility bounds"):
            PatientClinicalIndicators(**data_infant)

        data_centenarian = dict(valid_patient_payload, age=125)
        with pytest.raises(ValueError, match="Patient age .* violates physiological plausibility bounds"):
            PatientClinicalIndicators(**data_centenarian)

    def test_clinical_warning_flags_generation(self, valid_patient_payload):
        """Critical non-blocking threshold warnings must be generated accurately."""
        patient = PatientClinicalIndicators(**valid_patient_payload)
        warnings = patient.get_clinical_warning_flags()
        assert any("Multi-Vessel Calcification" in w for w in warnings)
        assert any("Diabetic Risk Factor" in w for w in warnings)


# ===========================================================================
# 2. FastAPI Endpoint & HTTP 422 Validation Rejection Tests
# ===========================================================================
class TestFastAPIEndpointValidation:
    """Verifies HTTP status codes and structured ProblemDetails responses from API."""

    def test_predict_rejects_trestbps_below_60(self, client, valid_patient_payload):
        """Requests with trestbps < 60 must immediately return HTTP 422 with actionable message."""
        payload = dict(valid_patient_payload, trestbps=48.0)
        response = client.post("/api/v1/predict", json=payload)
        assert response.status_code == 422

        body = response.json()
        assert body["status"] == 422
        assert body["title"] == "Validation Error"
        assert len(body["errors"]) >= 1

        error = body["errors"][0]
        assert "trestbps" in error["loc"]
        assert "violates physiological limits (60 - 260 mmHg)" in error["msg"]
        assert error["input"] == 48.0

    def test_predict_rejects_thalach_above_240(self, client, valid_patient_payload):
        """Requests with thalach > 240 must immediately return HTTP 422 with actionable message."""
        payload = dict(valid_patient_payload, thalach=248.0)
        response = client.post("/api/v1/predict", json=payload)
        assert response.status_code == 422

        body = response.json()
        assert body["status"] == 422
        error = body["errors"][0]
        assert "thalach" in error["loc"]
        assert "must be between 40 and 220 bpm" in error["msg"]

    def test_predict_rejects_cholesterol_below_80(self, client, valid_patient_payload):
        """Requests with chol < 80 must return HTTP 422."""
        payload = dict(valid_patient_payload, chol=55.0)
        response = client.post("/api/v1/predict", json=payload)
        assert response.status_code == 422

        body = response.json()
        error = body["errors"][0]
        assert "chol" in error["loc"]
        assert "must be within plausible limits (80 - 600 mg/dL)" in error["msg"]

    def test_predict_rejects_oldpeak_above_6_5(self, client, valid_patient_payload):
        """Requests with oldpeak > 6.5 must return HTTP 422."""
        payload = dict(valid_patient_payload, oldpeak=7.5)
        response = client.post("/api/v1/predict", json=payload)
        assert response.status_code == 422

        body = response.json()
        error = body["errors"][0]
        assert "oldpeak" in error["loc"]
        assert "must range between 0.0 and 6.5 mm" in error["msg"]

    def test_predict_rejects_ca_above_3(self, client, valid_patient_payload):
        """Requests with ca > 3 must return HTTP 422."""
        payload = dict(valid_patient_payload, ca=5)
        response = client.post("/api/v1/predict", json=payload)
        assert response.status_code == 422

        body = response.json()
        error = body["errors"][0]
        assert "ca" in error["loc"]
        assert "must be 0, 1, 2, or 3" in error["msg"]

    def test_predict_wrapped_payload_rejection(self, client, valid_patient_payload):
        """Wrapped PredictionRequest format correctly pinpoints invalid fields."""
        wrapped = {
            "encounter_id": "8d39c094-0a37-4f6c-8a03-7cb529bfa5e1",
            "clinical_indicators": dict(valid_patient_payload, trestbps=290.0),
        }
        response = client.post("/api/v1/predict", json=wrapped)
        assert response.status_code == 422

        body = response.json()
        error = body["errors"][0]
        assert "trestbps" in error["loc"]
        assert "violates physiological limits" in error["msg"]

    def test_predict_accepts_valid_payload(self, client, valid_patient_payload):
        """Clinically valid indicators pass validation and return HTTP 200 with full CDSS response."""
        response = client.post("/api/v1/predict", json=valid_patient_payload)
        assert response.status_code == 200

        data = response.json()
        assert "risk_probability" in data
        assert 0.0 <= data["risk_probability"] <= 1.0
        assert data["risk_band"] in ["Low", "Moderate", "Elevated", "High"]
        assert isinstance(data["is_cad_positive"], bool)
        assert "confidence_interval" in data
        assert "lower" in data["confidence_interval"]
        assert "upper" in data["confidence_interval"]
        assert len(data["top_risk_drivers"]) <= 5
        assert len(data["top_protective_factors"]) <= 3
        assert len(data["all_attributions"]) == 22
        assert "recommended_action" in data
        assert "clinical_disclaimer" in data
        assert data["inference_latency_ms"] > 0.0


# ===========================================================================
# 3. Health Endpoint & OpenAPI Documentation Tests
# ===========================================================================
class TestHealthAndDocumentation:
    """Verifies /health probe and OpenAPI schema generation."""

    def test_health_check_endpoint(self, client):
        """GET /health must return 200 OK with runtime diagnostics."""
        response = client.get("/health")
        assert response.status_code == 200

        data = response.json()
        assert data["status"] == "healthy"
        assert data["model_loaded"] is True
        assert data["model_version"] == "v1.0.0"
        assert data["calibrator_method"] == "Isotonic Regression"
        assert data["decision_threshold"] == 0.29
        assert data["uptime_seconds"] >= 0.0
        assert "memory_usage_mb" in data
        assert data["memory_usage_mb"] > 0.0
        assert "timestamp" in data


    def test_root_endpoint(self, client):
        """GET / returns service info and navigation endpoints."""
        response = client.get("/")
        assert response.status_code == 200
        data = response.json()
        assert "service" in data
        assert data["documentation"] == "/docs"

    def test_openapi_schema_generation(self, client):
        """GET /openapi.json must be accessible and include full schema definitions."""
        response = client.get("/openapi.json")
        assert response.status_code == 200

        schema = response.json()
        assert schema["info"]["title"] == "PULSE - Clinical Decision Support System (CDSS) API"
        assert schema["info"]["version"] == "1.0.0"

        # Check endpoints
        paths = schema["paths"]
        assert "/health" in paths
        assert "/api/v1/predict" in paths

        # Check components
        components = schema["components"]["schemas"]
        required_schemas = [
            "PatientClinicalIndicators",
            "PredictionRequest",
            "PredictionResponse",
            "ConfidenceInterval",
            "FeatureAttribution",
            "HealthCheckResponse",
            "HTTPValidationErrorResponse",
            "ValidationErrorDetail",
        ]
        for s in required_schemas:
            assert s in components, f"Missing component schema: {s}"

        # Verify PatientClinicalIndicators properties have descriptions
        props = components["PatientClinicalIndicators"]["properties"]
        assert len(props) == 13
        for field_name in ["age", "trestbps", "chol", "thalach", "oldpeak", "ca", "thal"]:
            assert "description" in props[field_name], f"Field {field_name} missing description"

    def test_docs_swagger_ui_accessible(self, client):
        """GET /docs returns 200 OK containing Swagger UI HTML."""
        response = client.get("/docs")
        assert response.status_code == 200
        assert "text/html" in response.headers["content-type"]
        assert "swagger-ui" in response.text.lower()
