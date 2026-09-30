# ML-201: FastAPI Service Scaffolding & Pydantic Validation Schemas

## 1. Executive Summary

**Task Identifier:** ML-201 (Phase 2: High-Performance Inference & Explainability Service)  
**Objective:** Scaffold a Python FastAPI microservice that enforces strict physiological input constraints via Pydantic models before clinical data reaches the machine learning model, exposes structured endpoints (`/health`, `/api/v1/predict`), and automatically generates comprehensive OpenAPI/Swagger documentation at `/docs`.  
**Status:** Completed & Validated against Acceptance Criteria.  
**Test Suite:** 25/25 unit & integration tests passing (`ml/tests/test_api_schemas.py`), 49/49 across the entire PULSE repository.

---

## 2. Architecture & File Structure

The inference microservice follows standard clean architecture separating schemas, routers, service layers, and application lifecycles:

```
ml/
├── main.py                          # FastAPI app factory, lifespan manager, CORS, middleware, custom error handlers
├── schemas/
│   ├── __init__.py                  # Public schema contract exports
│   └── clinical.py                  # Pydantic v2 models, physiological validators, warning flags, response DTOs
├── routers/
│   ├── __init__.py                  # Router package aggregation
│   ├── health.py                    # GET /health probe returning readiness, threshold, uptime
│   └── predict.py                   # POST /api/v1/predict endpoint with validation and inference execution
├── services/
│   ├── __init__.py                  # Service package exports
│   └── inference_service.py         # Singleton managing artifacts, model state, SHAP explainer, and latency metrics
└── tests/
    └── test_api_schemas.py          # 25 unit and integration tests verifying boundaries and API contracts
```

---

## 3. Physiological Boundary Specifications & Validation

Physiological boundaries mirror clinical plausibility standards defined in `PROJECT_DETAILS.md` and match ASP.NET Core FluentValidation specs in the main backend gateway:

| Clinical Attribute      | Variable   | Plausible Limits      | Normal Reference    | Custom Validator Rule & Violation Message                                                               |
| :---------------------- | :--------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------------------------ |
| **Age**                 | `age`      | $1 - 110$ yrs         | $18 - 85$ yrs       | `Patient age ({v} yrs) violates physiological plausibility bounds (1 - 110 yrs).`                       |
| **Biological Sex**      | `sex`      | $0$ or $1$            | $0$ or $1$          | `Biological sex code ({v}) is invalid. Must be 0 (Female) or 1 (Male).`                                 |
| **Chest Pain Type**     | `cp`       | $1, 2, 3, 4$          | $1 - 4$             | `Chest pain type ({v}) must be 1 (Typical Angina), 2 (Atypical), 3 (Non-Anginal), or 4 (Asymptomatic).` |
| **Resting BP**          | `trestbps` | $60 - 260$ mmHg       | $90 - 120$ mmHg     | `Resting Blood Pressure ({v} mmHg) violates physiological limits (60 - 260 mmHg).`                      |
| **Serum Cholesterol**   | `chol`     | $80 - 600$ mg/dL      | $125 - 200$ mg/dL   | `Serum Cholesterol ({v} mg/dL) must be within plausible limits (80 - 600 mg/dL).`                       |
| **Fasting Blood Sugar** | `fbs`      | $0$ or $1$            | $0$ ($< 120$ mg/dL) | `Fasting blood sugar flag ({v}) must be 0 (< 120 mg/dL) or 1 (>= 120 mg/dL).`                           |
| **Resting ECG**         | `restecg`  | $0, 1, 2$             | $0$ (Normal)        | `Resting ECG code ({v}) must be 0 (Normal), 1 (ST-T abnormality), or 2 (LV hypertrophy).`               |
| **Max Heart Rate**      | `thalach`  | $40 - 220$ bpm        | $60 - 180$ bpm      | `Maximum heart rate achieved ({v} bpm) must be between 40 and 220 bpm.`                                 |
| **Exercise Angina**     | `exang`    | $0$ or $1$            | $0$ (Absent)        | `Exercise-induced angina flag ({v}) must be 0 (Absent) or 1 (Present).`                                 |
| **ST Depression**       | `oldpeak`  | $0.0 - 6.5$ mm        | $0.0 - 1.5$ mm      | `ST depression (oldpeak: {v} mm) must range between 0.0 and 6.5 mm.`                                    |
| **ST Slope**            | `slope`    | $1, 2, 3$             | $1 - 3$             | `Peak exercise ST slope ({v}) must be 1 (Upsloping), 2 (Flat), or 3 (Downsloping).`                     |
| **Fluoroscopy Vessels** | `ca`       | $0 - 3$               | $0$                 | `Number of major vessels ({v}) colored by fluoroscopy must be 0, 1, 2, or 3.`                           |
| **Thallium Defect**     | `thal`     | $3, 6, 7$ (or $1, 2$) | $3$ (Normal)        | `Thalassemia code ({v}) is invalid. Must be 3 (Normal), 6 (Fixed Defect), or 7 (Reversible Defect).`    |

---

## 4. RFC 7807 Structured Problem Details Error Handling

When out-of-range clinical parameters are submitted, the custom exception handler formats the rejection into an RFC 7807-compatible problem details JSON payload:

```json
{
  "title": "Validation Error",
  "status": 422,
  "detail": "Patient clinical indicators failed physiological boundary validation.",
  "errors": [
    {
      "loc": ["body", "clinical_indicators", "trestbps"],
      "msg": "Resting Blood Pressure (50.0 mmHg) violates physiological limits (60 - 260 mmHg).",
      "type": "value_error",
      "input": 50.0
    }
  ]
}
```

The request intake schema (`PredictionRequest`) supports both:

1. **Nested DTO format:** Sent by ASP.NET Core gateway with `encounter_id`, `correlation_id`, `patient_mrn`, and `clinical_indicators`.
2. **Flat root format:** Sent during rapid developer testing or direct API invocation with clinical indicators at the root JSON level, automatically normalized by `@model_validator(mode="before")`.

---

## 5. Lifespan Management & Cold-Start Performance

Artifacts are loaded strictly during microservice startup via FastAPI's `@asynccontextmanager lifespan`:

- **Load Time:** Sub-$50$ ms cold start (measured at $39.2$ ms against $< 500$ ms target).
- **In-Memory Residency:** Preprocessor, Calibrated LightGBM model, and SHAP TreeExplainer reside persistently in `app.state.inference_service`.
- **Zero Request-Time I/O:** Requests perform feature transformations and matrix evaluations purely in memory.

---

## 6. Acceptance Criteria Verification

| Requirement               | Acceptance Criteria                                                                                                                                                       | Verified Result                                                                                                                                                                                   | Status   |
| :------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | :------- |
| **Input Rejection**       | Requests containing out-of-range inputs (e.g., `trestbps < 60` or `thalach > 240`) are rejected immediately with HTTP 422 and actionable field-level validation messages. | Tested via `test_predict_rejects_trestbps_below_60` and `test_predict_rejects_thalach_above_240`. Both return HTTP 422 with structured RFC 7807 payloads and exact physiological boundary alerts. | **PASS** |
| **OpenAPI Documentation** | API generates complete OpenAPI/Swagger documentation at `/docs` with detailed schema descriptions.                                                                        | Verified via `test_openapi_schema_generation` and `test_docs_swagger_ui_accessible`. OpenAPI 3.1 JSON contains all 8 schemas with property descriptions and constraints.                          | **PASS** |
| **Health Probe**          | Health endpoint `/health` returns readiness state and metadata.                                                                                                           | Verified via `test_health_check_endpoint`. Returns status `healthy`, `model_loaded: true`, model version, threshold, and uptime.                                                                  | **PASS** |
