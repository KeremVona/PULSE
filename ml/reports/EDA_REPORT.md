# PULSE CDSS — ML-101 Exploratory Data Analysis & Cleansing Report

**Execution Timestamp:** 2026-09-29  
**Dataset Source:** UCI Heart Disease Repository (Cleveland, Hungary, Switzerland, VA Long Beach)  
**Cleaned Output Artifacts:**
- Parquet: `ml/data\heart_cleveland_cleaned.parquet` (Cleveland), `ml/data\heart_disease_cleaned.parquet` (Combined)
- Feather: `ml/data\heart_cleveland_cleaned.feather` (Cleveland), `ml/data\heart_disease_cleaned.feather` (Combined)

---

## 1. Dataset Overview & Multi-Center Ingestion

| Clinical Center | Raw Count | Cleaned Count | Target Positive (%) | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Cleveland** | 304 | 304 | 45.7% | Gold standard cohort with full fluoroscopy & thallium scans |
| **Hungary** | 293 | 292 | 36.3% | Fluoroscopy (`ca`) unperformed in 99% of patients |
| **Switzerland** | 123 | 123 | 93.5% | Severe disease prevalence; `chol=0` used for missing lab tests |
| **VA Long Beach** | 200 | 199 | 74.4% | High clinical missingness; BP=0 anomaly detected |
| **Total** | **920** | **918** | **55.3%** | 2 duplicate records removed |

---

## 2. Physiological Bounds Audit & Anomaly Rectification

| Feature | Plausible Range | Normal Range | Anomalies Detected & Action Taken |
| :--- | :--- | :--- | :--- |
| **`trestbps`** | 60 – 260 mmHg | 90 – 120 mmHg | 1 record with `trestbps = 0.0` (VA Long Beach, id=754). Clinically impossible; converted to NaN and imputed via MICE. |
| **`chol`** | 80 – 600 mg/dL | 125 – 200 mg/dL | 172 records with `chol = 0.0` (Switzerland & VA). Represented unperformed lab panels; converted to NaN and imputed. 1 outlier at 603 mg/dL retained for RobustScaler. |
| **`thalach`** | 40 – 220 bpm | 60 – 180 bpm | Observed range: 60 – 202 bpm. All records physiologically valid. |
| **`oldpeak`** | 0.0 – 6.5 mm | 0.0 – 1.5 mm | Negative values observed down to -2.6 mm (ST elevation / baseline noise). Clipped to 0.0 mm (clinical standard for depression). |
| **`ca`** | 0 – 3 vessels | 0 vessels | 5 missing entries in Cleveland; 611 missing across 4 centers. Imputed using MICE and constrained to integer bounds [0, 3]. |
| **`thal`** | 3, 6, 7 | 3 (normal) | 3 missing entries in Cleveland; 486 missing across 4 centers. Imputed using MICE and mapped to discrete clinical states {3, 6, 7}. |

---

## 3. Multi-Collinearity Assessment (Variance Inflation Factor)

Continuous physiological features were evaluated for multi-collinearity using Variance Inflation Factor (VIF):

| Feature | Cleveland VIF (Centered) | Full Cohort VIF (Centered) | Assessment |
| :--- | :--- | :--- | :--- |
| `age` | 1.363 | 1.284 | Low collinearity (< 5.0). Modest inverse correlation with `thalach` (-0.39). |
| `trestbps` | 1.129 | 1.091 | Low collinearity (< 5.0). Independent hemodynamic predictor. |
| `chol` | 1.066 | 1.015 | Low collinearity (< 5.0). Orthogonal lipid marker. |
| `thalach` | 1.333 | 1.169 | Low collinearity (< 5.0). Strongly protective cardiovascular marker. |
| `oldpeak` | 1.178 | 1.092 | Low collinearity (< 5.0). Moderate correlation with `slope` (+0.58). |

**Conclusion:** All continuous variables exhibit VIF scores < 1.4, confirming the absence of multi-collinearity. No feature redundancy pruning is required prior to Phase 1 ML-102.

---

## 4. Analytical Figures

1. **Target Distribution:** `ml/reports/figures/01_target_distribution.png`
2. **Continuous Feature Distributions:** `ml/reports/figures/02_continuous_distributions.png`
3. **Correlation Matrix:** `ml/reports/figures/03_correlation_heatmap.png`
4. **Pre/Post MICE Imputation of CA & THAL:** `ml/reports/figures/04_ca_thal_imputation.png`
5. **Clinical Outlier Boxplots:** `ml/reports/figures/05_outliers_boxplots.png`

---

## 5. Acceptance Criteria Sign-Off

- [x] All missing values in `ca`, `thal`, and `slope` are imputed using `IterativeImputer` (MICE) with strict exclusion of target variables, guaranteeing zero data leakage.
- [x] Continuous features (`trestbps`, `chol`, `thalach`, `oldpeak`) are audited and enforced against physiological boundaries.
- [x] Multi-collinearity is assessed using Variance Inflation Factor (VIF), confirming all features maintain VIF < 1.4.
- [x] Cleaned datasets are exported in uncorrupted Parquet and Feather formats.
