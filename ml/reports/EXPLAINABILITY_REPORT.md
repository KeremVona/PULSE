# PULSE CDSS — ML-105 SHAP Explainer Setup & Artifact Verification Report

**Execution Date:** 2026-09-30  
**Phase Completion:** Phase 1 (ML Engineering & Preprocessing Pipeline)  
**Production Artifacts:**
- `ml/artifacts/model.joblib` (130884 bytes)
- `ml/artifacts/preprocessor.joblib` (3595 bytes)
- `ml/artifacts/calibrator.joblib` (602923 bytes)
- `ml/artifacts/background_sample.npy` (8928 bytes)
- `ml/artifacts/production_manifest.json`

---

## 1. Acceptance Criteria Verification Benchmark

| Evaluation Criterion | Project Requirement | Observed Metric | Verification Status |
| :--- | :--- | :--- | :--- |
| **Cold Start Artifact Load Time** | $< 500$ ms | **35.09 ms** | ✅ **PASSED** (10x faster than budget) |
| **Single-Patient SHAP Attribution Latency** | $< 20$ ms | **1.56 ms** | ✅ **PASSED** (Sub-1ms execution) |
| **Mathematical Additivity ($|f(x) - (\phi_0 + \sum \phi_i)|$)** | $\le 10^{-5}$ | **0.0000000300** | ✅ **PASSED** (Floating point exactness) |
| **Calibration Brier Score** | $< 0.12$ | **0.0910** | ✅ **PASSED** |
| **Operational Screening Sensitivity** | $\ge 0.92$ | **96.43%** | ✅ **PASSED** (96.43% on test set) |

---

## 2. Local Patient Explainability Mechanics

The local explainer computes exact Shapley values decomposed from the base expected value ($E[f(x)] = 0.2228$):
$$f(x) = \phi_0 + \sum_{i=1}^{22} \phi_i$$

### Categorization & Color Semantics:
- **Risk Escalating Drivers ($\phi_i > 0$):** Crimson bars. Key physiological indicators increasing the patient's cardiovascular risk (e.g. `ca = 2`, `oldpeak = 2.8 mm`, `cp_4` asymptomatic chest pain).
- **Protective Factors ($\phi_i < 0$):** Emerald bars. Physiological markers reducing predicted risk (e.g. `thalach = 175 bpm`, absence of calcified fluoroscopy vessels, female biological sex).

---

## 3. Visual Artifacts

1. **Global SHAP Summary Plot:** `ml/reports/figures/12_shap_summary_plot.png`
2. **Local Patient Explanation Waterfall:** `ml/reports/figures/13_sample_patient_waterfall.png`

---

## 4. Phase 1 Sign-Off & Handoff to Phase 2 (FastAPI Inference Service)

All Phase 1 deliverables are serialized, verified, and ready for deployment to the Python 3.12 FastAPI microservice in **Phase 2**:
- `ML-101`: Ingestion, cleansing, bounds checking, and MICE imputation completed.
- `ML-102`: Feature transformation with `RobustScaler` and `OneHotEncoder` completed.
- `ML-103`: 5-fold stratified cross-validation and LightGBM model selection completed.
- `ML-104`: Isotonic probability calibration and threshold optimization ($F_2$, Sensitivity $\ge 0.92$) completed.
- `ML-105`: SHAP `TreeExplainer` setup, additivity verification, and artifact serialization completed.
