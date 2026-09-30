# PULSE CDSS — ML-102 Feature Transformation & Preprocessing Report

**Execution Date:** 2026-09-30  
**Artifact Path:** `ml/artifacts/preprocessor.joblib` (3595 bytes)  
**Input Data:** `ml/data/heart_cleveland_cleaned.parquet` (Cleaned Cleveland Reference Cohort, N=304)  

---

## 1. Train / Test Split & Leakage Prevention Strategy

The dataset is partitioned into an **80% Training Set (N=243)** and a **20% Test Set (N=61)**, stratified by the binary disease target (`target`).

| Split | Sample Count | Disease Positive Count | Disease Prevalence (%) |
| :--- | :--- | :--- | :--- |
| **Training Set (`X_train`)** | 243 | 111 | 45.7% |
| **Test Set (`X_test`)** | 61 | 28 | 45.9% |
| **Total Cohort** | 304 | 139 | 45.7% |

> **Data Leakage Guarantee:** The `PULSEPreprocessor` is fitted **strictly on `X_train`**. Medians, IQRs, and category distributions are learned solely from the training distribution. The test split is evaluated using frozen training parameters without any parameter adaptation.

---

## 2. RobustScaler vs. StandardScaler for Clinical Outliers

In cardiovascular clinical data, laboratory indicators (such as cholesterol) frequently present severe positive skewness due to familial hypercholesterolemia, acute post-prandial state, or metabolic dysregulation.

`RobustScaler` scales features using the median and Interquartile Range (IQR = Q3 - Q1):
`x_scaled = (x - median(X_train)) / (Q3(X_train) - Q1(X_train))`

### Learned Parameters on Training Cohort:
| Feature | Learned Median (Center) | Learned IQR (Scale) | Plausible Bounds |
| :--- | :--- | :--- | :--- |
| `age` | 55.0 yrs | 13.0 yrs | 1 – 110 yrs |
| `trestbps` | 130.0 mmHg | 20.0 mmHg | 60 – 260 mmHg |
| `chol` | 245.0 mg/dL | 68.0 mg/dL | 80 – 600 mg/dL |
| `thalach` | 153.0 bpm | 29.5 bpm | 40 – 220 bpm |
| `oldpeak` | 0.8 mm | 1.6 mm | 0.0 – 6.5 mm |

---

## 3. Transformed Feature Space & SHAP Explainability Map

The input 13 predictor columns are expanded into **22 transformed features** with preserved technical identifiers and human-readable clinical labels for downstream `TreeExplainer` local attribution:

| Transformed Feature Key | Transformation Type | Human Clinical Label (UI Display) |
| :--- | :--- | :--- |
| `age` | Continuous (RobustScaled) | Age (years) |
| `trestbps` | Continuous (RobustScaled) | Resting Blood Pressure (mmHg) |
| `chol` | Continuous (RobustScaled) | Serum Cholesterol (mg/dL) |
| `thalach` | Continuous (RobustScaled) | Max Heart Rate Achieved (bpm) |
| `oldpeak` | Continuous (RobustScaled) | ST Depression (mm) |
| `cp_1` | Nominal (One-Hot Encoded) | Chest Pain: Typical Angina |
| `cp_2` | Nominal (One-Hot Encoded) | Chest Pain: Atypical Angina |
| `cp_3` | Nominal (One-Hot Encoded) | Chest Pain: Non-Anginal |
| `cp_4` | Nominal (One-Hot Encoded) | Chest Pain: Asymptomatic |
| `restecg_0` | Nominal (One-Hot Encoded) | Resting ECG: Normal |
| `restecg_1` | Nominal (One-Hot Encoded) | Resting ECG: ST-T Wave Abnormality |
| `restecg_2` | Nominal (One-Hot Encoded) | Resting ECG: Left Ventricular Hypertrophy |
| `slope_1` | Nominal (One-Hot Encoded) | ST Slope: Upsloping |
| `slope_2` | Nominal (One-Hot Encoded) | ST Slope: Flat |
| `slope_3` | Nominal (One-Hot Encoded) | ST Slope: Downsloping |
| `thal_3` | Nominal (One-Hot Encoded) | Thalassemia: Normal |
| `thal_6` | Nominal (One-Hot Encoded) | Thalassemia: Fixed Defect |
| `thal_7` | Nominal (One-Hot Encoded) | Thalassemia: Reversible Defect |
| `sex` | Binary / Count (Passthrough) | Biological Sex (Male=1, Female=0) |
| `fbs` | Binary / Count (Passthrough) | Fasting Blood Sugar > 120 mg/dL |
| `exang` | Binary / Count (Passthrough) | Exercise-Induced Angina (Yes=1) |
| `ca` | Binary / Count (Passthrough) | Major Fluoroscopy Vessels Colored (0-3) |

---

## 4. Acceptance Criteria Verification

- [x] **Strict fit/transform separation:** `PULSEPreprocessor` fits on `X_train` only; `X_test` transformed without refitting.
- [x] **Robust scaling:** `RobustScaler` selected and fitted on continuous metrics to prevent distortion from clinical lab outliers.
- [x] **SHAP compatibility:** Technical feature keys (`cp_4`, `oldpeak`, `ca`, `thalach`) preserved with direct mapping to clinical labels.
- [x] **Serialization:** Serialized to `ml/artifacts/preprocessor.joblib` and verified with bit-identical reloading.
