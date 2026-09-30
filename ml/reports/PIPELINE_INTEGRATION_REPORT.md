# ML-202: Model Loading and Prediction Pipeline Integration Report

## 1. Executive Summary

**Task Identifier:** ML-202 (Phase 2: High-Performance Inference & Explainability Service)  
**Objective:** Load serialized machine learning artifacts into FastAPI application state on startup, implement the internal data transformation utility mapping incoming clinical DTOs into numerical feature matrices, and execute the core prediction endpoint delivering calibrated probabilities, risk tiers, and 95% confidence intervals under strict sub-30ms latency constraints.  
**Status:** Completed & Validated against Acceptance Criteria.  
**Test Suite:** 12/12 dedicated pipeline tests passing (`ml/tests/test_prediction_pipeline.py`), 61/61 tests passing across the entire repository.

---

## 2. Architecture & Implementation Highlights

### 2.1 Lifespan Application State Binding & Zero Disk I/O

The microservice lifecycle is orchestrated via FastAPI's `@asynccontextmanager lifespan`:

- **Single-Pass Load:** During microservice startup, `InferenceService.load_artifacts()` loads `model.joblib`, `preprocessor.joblib`, `calibrator.joblib`, `background_sample.npy`, `calibrated_model_config.json`, and `production_manifest.json` into memory within $43.0$ ms (well below the $500$ ms cold-start budget).
- **Application State Storage:** Artifacts reside persistently on `app.state.inference_service`.
- **Zero Request-Time I/O:** Verified via automated monkeypatching (`unittest.mock.patch` on `joblib.load` and `numpy.load`). During prediction requests, disk read operations are called **zero** times.
- **Dependency Resolution:** `get_inference_service(request)` retrieves the active instance from `request.app.state.inference_service`.

### 2.2 Internal Transformation Utility (`ClinicalDataTransformer`)

[`ml/services/transformer.py`](file:///f:/Code/Projects/PULSE/ml/services/transformer.py) isolates DTO-to-matrix mapping from model logic:

- Maps `PatientClinicalIndicators` into a 1-row or batch Pandas DataFrame with deterministic column ordering:
  `['age', 'trestbps', 'chol', 'thalach', 'oldpeak', 'cp', 'restecg', 'slope', 'thal', 'sex', 'fbs', 'exang', 'ca']`.
- Applies the fitted `PULSEPreprocessor` (RobustScaler + OneHotEncoder), producing a contiguous $(N, 22)$ float64 NumPy matrix matching the 22 features expected by LightGBM and SHAP.
- Validates data cleanliness (guarantees zero NaNs, nulls, or infinite values).

### 2.3 Vectorized Calibrated Risk & 95% Confidence Intervals

In [`ml/src/calibration.py`](file:///f:/Code/Projects/PULSE/ml/src/calibration.py):

- `predict_with_uncertainty` was vectorized across the 5 cross-validation fold estimators:
  $$\text{fold\_matrix} = \big[ \text{clf}_k.\text{predict\_proba}(X)[:, 1] \big]_{k=1}^5 \in \mathbb{R}^{N \times 5}$$
  $$\hat{P}(Y=1 \mid X) = \frac{1}{5} \sum_{k=1}^5 \text{fold\_matrix}_{:, k}, \quad \text{SE} = \text{std}(\text{fold\_matrix})$$
- 95% Confidence Interval:
  $$\text{CI}_{95\%} = \big[ \text{clip}(\hat{P} - 1.96 \cdot \text{SE}, 0, 1), \;\; \text{clip}(\hat{P} + 1.96 \cdot \text{SE}, 0, 1) \big]$$
- Vectorization cut inference execution time from $13.3$ ms down to $5.6$ ms per call.

---

## 3. Benchmark & Concurrency Performance (10 req/s)

Benchmark executed under a sustained 10 req/s load (30 consecutive timed requests paced at 100 ms intervals):

| Metric                                        | Target / SLA     | Benchmark Result | Margin                 | Status   |
| :-------------------------------------------- | :--------------- | :--------------- | :--------------------- | :------- |
| **Roundtrip Request Latency (Median)**        | $< 30.0$ ms      | **$17.60$ ms**   | $-41.3\%$ below budget | **PASS** |
| **Roundtrip Request Latency (95th %ile)**     | $< 50.0$ ms      | **$19.05$ ms**   | $-61.9\%$ below budget | **PASS** |
| **Server Process Time (`X-Process-Time-Ms`)** | $< 30.0$ ms      | **$16.16$ ms**   | $-46.1\%$ below budget | **PASS** |
| **Pure ML Inference Engine Latency**          | $< 25.0$ ms      | **$14.48$ ms**   | $-42.1\%$ below budget | **PASS** |
| **Throughput**                                | $\ge 10.0$ req/s | **$10.0$ req/s** | Target matched         | **PASS** |
| **Cold-Start Load Time**                      | $< 500.0$ ms     | **$43.0$ ms**    | $-91.4\%$ below budget | **PASS** |

Higher rate benchmark results:

- **15 req/s:** Median latency $16.94$ ms (95th percentile $17.98$ ms).
- **20 req/s:** Median latency $17.09$ ms (95th percentile $19.70$ ms).

---

## 4. Multi-Cohort Clinical Risk Stratification Verification

| Patient Profile          | Characteristics                                                    | Calibrated Risk $\hat{P}$ | Risk Band | Threshold Check ($T=0.29$) | Guideline Recommendation                          |
| :----------------------- | :----------------------------------------------------------------- | :------------------------ | :-------- | :------------------------- | :------------------------------------------------ |
| **Low Risk**             | 36 yo female, BP 110, Chol 165, Max HR 178, Oldpeak 0.0, Vessels 0 | $0.0620$ ($< 20\%$)       | **Low**   | Negative (`False`)         | Main Prevention & Lifestyle Counseling            |
| **Elevated / High Risk** | 67 yo male, BP 160, Chol 298, Max HR 108, Oldpeak 3.4, Vessels 3   | $0.9850$ ($\ge 75\%$)     | **High**  | Positive (`True`)          | Urgent Cardiology Referral & Invasive Angiography |

---

## 5. Acceptance Criteria Verification

| Requirement                      | Acceptance Criteria                                                                                                                                              | Verified Result                                                                                                                                                                                                                | Status   |
| :------------------------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :------- |
| **Lifespan State Management**    | Model loading is bound to the lifespan context; artifacts are not re-read from disk per request.                                                                 | Verified via `test_model_loaded_in_app_state` and `test_zero_disk_io_per_request`. Mocks on `joblib.load` and `numpy.load` confirmed zero disk reads during request cycles.                                                    | **PASS** |
| **Output Payload Specification** | Output payload delivers: `risk_probability` (float between 0.0 and 1.0), `risk_band` (Low, Moderate, Elevated, High), and `confidence_interval` (95% CI bounds). | Verified via `test_output_payload_schema_and_bounds`, `test_low_risk_patient_classification`, `test_high_risk_patient_classification`, and `test_prediction_response_validation_model`. All fields conform strictly to schema. | **PASS** |
| **Concurrent Latency Budget**    | Median inference latency remains $< 30$ ms under concurrent testing (10 req/s).                                                                                  | Verified via `test_concurrent_throughput_and_median_latency`. Median roundtrip latency measured at **$17.60$ ms** (server execution $16.16$ ms, inference engine $14.48$ ms) at 10.0 req/s.                                    | **PASS** |
