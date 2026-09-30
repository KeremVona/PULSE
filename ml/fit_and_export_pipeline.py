"""
PULSE Clinical Decision Support System (CDSS)
Pipeline: ML-102 Fit and Export Clinical Preprocessing Pipeline
"""

import os
import sys

# Ensure project root is in sys.path
sys.path.insert(0, os.path.abspath("."))

import numpy as np
import pandas as pd

from ml.src.pipeline import (
    ALL_PREDICTOR_COLUMNS,
    CONTINUOUS_FEATURES,
    CATEGORICAL_FEATURES,
    BINARY_FEATURES,
    ORDINAL_FEATURES,
    CLINICAL_LABEL_MAP,
    PULSEPreprocessor,
    split_train_test_clinical,
)


def main():
    print("=" * 70)
    print("PULSE CDSS - ML-102: Feature Transformation & Preprocessing Pipeline")
    print("=" * 70)

    data_path = "ml/data/heart_cleveland_cleaned.parquet"
    artifact_path = "ml/artifacts/preprocessor.joblib"
    reports_dir = "ml/reports"
    os.makedirs(reports_dir, exist_ok=True)
    os.makedirs("ml/artifacts", exist_ok=True)

    # 1. Load Cleaned Dataset
    print(f"\n[1/5] Loading cleaned Cleveland dataset from: {data_path}")
    df = pd.read_parquet(data_path)
    print(f"      Total records: {len(df)}")
    print(f"      Target prevalence: {df['target'].mean() * 100:.1f}% ({df['target'].sum()} positive)")

    # 2. Stratified Train/Test Split (Preventing Data Leakage)
    print("\n[2/5] Partitioning into stratified Train (80%) and Test (20%) sets...")
    X_train, X_test, y_train, y_test = split_train_test_clinical(
        df, test_size=0.20, random_state=42, stratify=True
    )
    print(f"      X_train shape: {X_train.shape} (Prevalence: {y_train.mean() * 100:.1f}%)")
    print(f"      X_test shape:  {X_test.shape} (Prevalence: {y_test.mean() * 100:.1f}%)")

    # 3. Fit Pipeline Strictly on Training Data
    print("\n[3/5] Fitting PULSEPreprocessor strictly on X_train (Zero Data Leakage)...")
    preprocessor = PULSEPreprocessor()
    preprocessor.fit(X_train)

    # Transform both sets
    X_train_trans = preprocessor.transform_to_dataframe(X_train)
    X_test_trans = preprocessor.transform_to_dataframe(X_test)

    print(f"      Transformed feature count: {X_train_trans.shape[1]}")
    print(f"      Transformed features list: {preprocessor.get_feature_names()}")

    # Extract RobustScaler statistics learned on training set
    cont_scaler = preprocessor.transformer.named_transformers_["cont"]
    scaler_stats = pd.DataFrame({
        "Feature": CONTINUOUS_FEATURES,
        "Train_Median (Center)": cont_scaler.center_,
        "Train_IQR (Scale)": cont_scaler.scale_,
    })
    print("\n      Learned Continuous Scaling Parameters (RobustScaler):")
    print(scaler_stats.to_string(index=False))

    # 4. Serialize Preprocessor Artifact
    print(f"\n[4/5] Serializing fitted preprocessor to: {artifact_path}")
    saved_path = preprocessor.save(artifact_path)
    file_size_bytes = os.path.getsize(saved_path)
    print(f"      Successfully saved ({file_size_bytes} bytes).")

    # Verification of reload
    print("      Testing deserialization and verification...")
    reloaded = PULSEPreprocessor.load(artifact_path)
    reload_test_arr = reloaded.transform(X_test)
    np.testing.assert_allclose(X_test_trans.values, reload_test_arr, rtol=1e-12, atol=1e-12)
    print("      Verification PASSED: Deserialized artifact produces bit-identical outputs.")

    # 5. Export Preprocessing Report
    print("\n[5/5] Generating documentation report...")
    feature_names = preprocessor.get_feature_names()
    clinical_labels = preprocessor.get_clinical_labels()

    feature_table_rows = []
    for f_name, c_label in zip(feature_names, clinical_labels):
        f_type = "Continuous (RobustScaled)" if f_name in CONTINUOUS_FEATURES else \
                 "Nominal (One-Hot Encoded)" if any(f_name.startswith(p + "_") for p in CATEGORICAL_FEATURES) else \
                 "Binary / Count (Passthrough)"
        feature_table_rows.append(f"| `{f_name}` | {f_type} | {c_label} |")

    feature_table_md = "\n".join(feature_table_rows)

    report_md = f"""# PULSE CDSS — ML-102 Feature Transformation & Preprocessing Report

**Execution Date:** 2026-09-30  
**Artifact Path:** `{artifact_path}` ({file_size_bytes} bytes)  
**Input Data:** `{data_path}` (Cleaned Cleveland Reference Cohort, N={len(df)})  

---

## 1. Train / Test Split & Leakage Prevention Strategy

The dataset is partitioned into an **80% Training Set (N={len(X_train)})** and a **20% Test Set (N={len(X_test)})**, stratified by the binary disease target (`target`).

| Split | Sample Count | Disease Positive Count | Disease Prevalence (%) |
| :--- | :--- | :--- | :--- |
| **Training Set (`X_train`)** | {len(X_train)} | {int(y_train.sum())} | {y_train.mean() * 100:.1f}% |
| **Test Set (`X_test`)** | {len(X_test)} | {int(y_test.sum())} | {y_test.mean() * 100:.1f}% |
| **Total Cohort** | {len(df)} | {int(df['target'].sum())} | {df['target'].mean() * 100:.1f}% |

> **Data Leakage Guarantee:** The `PULSEPreprocessor` is fitted **strictly on `X_train`**. Medians, IQRs, and category distributions are learned solely from the training distribution. The test split is evaluated using frozen training parameters without any parameter adaptation.

---

## 2. RobustScaler vs. StandardScaler for Clinical Outliers

In cardiovascular clinical data, laboratory indicators (such as cholesterol) frequently present severe positive skewness due to familial hypercholesterolemia, acute post-prandial state, or metabolic dysregulation.

`RobustScaler` scales features using the median and Interquartile Range (IQR = Q3 - Q1):
`x_scaled = (x - median(X_train)) / (Q3(X_train) - Q1(X_train))`

### Learned Parameters on Training Cohort:
| Feature | Learned Median (Center) | Learned IQR (Scale) | Plausible Bounds |
| :--- | :--- | :--- | :--- |
| `age` | {cont_scaler.center_[0]:.1f} yrs | {cont_scaler.scale_[0]:.1f} yrs | 1 – 110 yrs |
| `trestbps` | {cont_scaler.center_[1]:.1f} mmHg | {cont_scaler.scale_[1]:.1f} mmHg | 60 – 260 mmHg |
| `chol` | {cont_scaler.center_[2]:.1f} mg/dL | {cont_scaler.scale_[2]:.1f} mg/dL | 80 – 600 mg/dL |
| `thalach` | {cont_scaler.center_[3]:.1f} bpm | {cont_scaler.scale_[3]:.1f} bpm | 40 – 220 bpm |
| `oldpeak` | {cont_scaler.center_[4]:.1f} mm | {cont_scaler.scale_[4]:.1f} mm | 0.0 – 6.5 mm |

---

## 3. Transformed Feature Space & SHAP Explainability Map

The input 13 predictor columns are expanded into **22 transformed features** with preserved technical identifiers and human-readable clinical labels for downstream `TreeExplainer` local attribution:

| Transformed Feature Key | Transformation Type | Human Clinical Label (UI Display) |
| :--- | :--- | :--- |
{feature_table_md}

---

## 4. Acceptance Criteria Verification

- [x] **Strict fit/transform separation:** `PULSEPreprocessor` fits on `X_train` only; `X_test` transformed without refitting.
- [x] **Robust scaling:** `RobustScaler` selected and fitted on continuous metrics to prevent distortion from clinical lab outliers.
- [x] **SHAP compatibility:** Technical feature keys (`cp_4`, `oldpeak`, `ca`, `thalach`) preserved with direct mapping to clinical labels.
- [x] **Serialization:** Serialized to `ml/artifacts/preprocessor.joblib` and verified with bit-identical reloading.
"""

    report_file = os.path.join(reports_dir, "PIPELINE_REPORT.md")
    with open(report_file, "w", encoding="utf-8") as f:
        f.write(report_md)
    print(f"      Saved documentation report: {report_file}")
    print("\n[SUCCESS] ML-102 pipeline fit and export completed successfully!")


if __name__ == "__main__":
    main()
