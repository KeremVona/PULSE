"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.src.cleansing
Description: Clinical data cleansing, physiological boundary verification,
             and MICE iterative imputation for the UCI Heart Disease dataset.
"""

from typing import Dict, List, Optional, Tuple
import os
import numpy as np
import pandas as pd
from sklearn.experimental import enable_iterative_imputer  # noqa: F401
from sklearn.impute import IterativeImputer
from sklearn.linear_model import BayesianRidge
from statsmodels.stats.outliers_influence import variance_inflation_factor

# ---------------------------------------------------------------------------
# Clinical Physiological Bounds (from PROJECT_DETAILS.md)
# ---------------------------------------------------------------------------
PHYSIOLOGICAL_BOUNDS = {
    "age": {"min": 1, "max": 110, "normal_min": 18, "normal_max": 85},
    "trestbps": {"min": 60, "max": 260, "normal_min": 90, "normal_max": 120},
    "chol": {"min": 80, "max": 600, "normal_min": 125, "normal_max": 200},
    "thalach": {"min": 40, "max": 220, "normal_min": 60, "normal_max": 180},
    "oldpeak": {"min": 0.0, "max": 6.5, "normal_min": 0.0, "normal_max": 1.5},
    "ca": {"min": 0, "max": 3, "normal_min": 0, "normal_max": 0},
}

# ---------------------------------------------------------------------------
# Categorical Mappings (Standardized with UCI and DATABASE_SCHEMA.md)
# ---------------------------------------------------------------------------
SEX_MAP = {"Female": 0, "Male": 1, "female": 0, "male": 1, 0: 0, 1: 1}

CP_MAP = {
    "typical angina": 1,
    "atypical angina": 2,
    "non-anginal": 3,
    "asymptomatic": 4,
    1: 1,
    2: 2,
    3: 3,
    4: 4,
}

FBS_MAP = {
    False: 0,
    True: 1,
    "False": 0,
    "True": 1,
    "false": 0,
    "true": 1,
    0: 0,
    1: 1,
}

RESTECG_MAP = {
    "normal": 0,
    "st-t abnormality": 1,
    "lv hypertrophy": 2,
    0: 0,
    1: 1,
    2: 2,
}

EXANG_MAP = {
    False: 0,
    True: 1,
    "False": 0,
    "True": 1,
    "false": 0,
    "true": 1,
    0: 0,
    1: 1,
}

SLOPE_MAP = {
    "upsloping": 1,
    "flat": 2,
    "downsloping": 3,
    1: 1,
    2: 2,
    3: 3,
}

# UCI coding: 3 = normal, 6 = fixed defect, 7 = reversible defect
THAL_MAP = {
    "normal": 3,
    "fixed defect": 6,
    "reversable defect": 7,
    "reversible defect": 7,
    3: 3,
    6: 6,
    7: 7,
}

FEATURE_COLUMNS_14 = [
    "age",
    "sex",
    "cp",
    "trestbps",
    "chol",
    "fbs",
    "restecg",
    "thalach",
    "exang",
    "oldpeak",
    "slope",
    "ca",
    "thal",
]


def load_raw_dataset(csv_path: str) -> pd.DataFrame:
    """Loads raw UCI CSV dataset and applies initial standardization."""
    if not os.path.exists(csv_path):
        raise FileNotFoundError(f"Dataset not found at: {csv_path}")

    df = pd.read_csv(csv_path)

    # Standardize column naming
    if "thalch" in df.columns:
        df = df.rename(columns={"thalch": "thalach"})

    # Strip whitespace from string columns
    str_cols = df.select_dtypes(include=["object"]).columns
    for col in str_cols:
        df[col] = df[col].astype(str).str.strip()
        # Restore actual NaNs from 'nan' strings
        df[col] = df[col].replace(["nan", "NaN", "None", ""], np.nan)

    return df


def audit_physiological_bounds(df: pd.DataFrame) -> pd.DataFrame:
    """
    Audits continuous clinical indicators against physiological plausibility bounds.
    Returns a DataFrame summary of violations.
    """
    violations = []

    for feature, bounds in PHYSIOLOGICAL_BOUNDS.items():
        if feature not in df.columns:
            continue

        series = pd.to_numeric(df[feature], errors="coerce")
        below_min = (series < bounds["min"]).sum()
        above_max = (series > bounds["max"]).sum()
        null_count = series.isna().sum()

        violations.append({
            "feature": feature,
            "plausible_min": bounds["min"],
            "plausible_max": bounds["max"],
            "below_min_count": int(below_min),
            "above_max_count": int(above_max),
            "null_count": int(null_count),
            "min_observed": float(series.min()) if not series.dropna().empty else None,
            "max_observed": float(series.max()) if not series.dropna().empty else None,
        })

    return pd.DataFrame(violations)


def preprocess_clinical_indicators(
    df: pd.DataFrame, drop_duplicates: bool = True
) -> pd.DataFrame:
    """
    Standardizes categorical encodings and corrects physiological contradictions:
    - trestbps <= 0 (e.g., patient with BP 0) -> NaN (physiologically impossible)
    - chol <= 0 (unrecorded lab test in Swiss/VA sites) -> NaN (serum cholesterol cannot be 0)
    - negative oldpeak (ST depression recorded with inverted sign) -> clipped to 0.0
    - target binary variable 'target': 0 if num == 0 else 1
    """
    cleaned = df.copy()

    # Drop pure feature duplicates if requested
    if drop_duplicates:
        subset_cols = [c for c in FEATURE_COLUMNS_14 if c in cleaned.columns]
        cleaned = cleaned.drop_duplicates(subset=subset_cols, keep="first")

    # Target binarization
    if "num" in cleaned.columns:
        cleaned["num"] = pd.to_numeric(cleaned["num"], errors="coerce").fillna(0).astype(int)
        cleaned["target"] = (cleaned["num"] > 0).astype(int)

    # Categorical encodings
    if "sex" in cleaned.columns:
        cleaned["sex"] = cleaned["sex"].map(SEX_MAP)
    if "cp" in cleaned.columns:
        cleaned["cp"] = cleaned["cp"].map(CP_MAP)
    if "fbs" in cleaned.columns:
        cleaned["fbs"] = cleaned["fbs"].map(FBS_MAP)
    if "restecg" in cleaned.columns:
        cleaned["restecg"] = cleaned["restecg"].map(RESTECG_MAP)
    if "exang" in cleaned.columns:
        cleaned["exang"] = cleaned["exang"].map(EXANG_MAP)
    if "slope" in cleaned.columns:
        cleaned["slope"] = cleaned["slope"].map(SLOPE_MAP)
    if "thal" in cleaned.columns:
        cleaned["thal"] = cleaned["thal"].map(THAL_MAP)

    # Cast continuous variables to numeric
    for col in ["age", "trestbps", "chol", "thalach", "oldpeak", "ca"]:
        if col in cleaned.columns:
            cleaned[col] = pd.to_numeric(cleaned[col], errors="coerce")

    # Physiological boundary corrections
    # 1. Blood pressure = 0 is clinically invalid -> mark as NaN for MICE imputation
    if "trestbps" in cleaned.columns:
        invalid_bp = cleaned["trestbps"] < PHYSIOLOGICAL_BOUNDS["trestbps"]["min"]
        cleaned.loc[invalid_bp, "trestbps"] = np.nan

    # 2. Cholesterol = 0 is an unrecorded lab test indicator -> mark as NaN
    if "chol" in cleaned.columns:
        invalid_chol = cleaned["chol"] < PHYSIOLOGICAL_BOUNDS["chol"]["min"]
        cleaned.loc[invalid_chol, "chol"] = np.nan

    # 3. ST depression: negative values represent elevation or baseline noise -> clip to 0.0
    if "oldpeak" in cleaned.columns:
        cleaned["oldpeak"] = cleaned["oldpeak"].clip(lower=0.0, upper=6.5)

    return cleaned


def impute_with_mice(
    df: pd.DataFrame,
    feature_cols: Optional[List[str]] = None,
    max_iter: int = 25,
    random_state: int = 42,
) -> Tuple[pd.DataFrame, IterativeImputer]:
    """
    Executes Multivariate Imputation by Chained Equations (MICE) using BayesianRidge.
    CRITICAL: Excludes target/num from the imputation feature space to prevent data leakage.
    Discretizes ca, thal, and slope to their valid clinical integer states post-imputation.
    """
    if feature_cols is None:
        feature_cols = [c for c in FEATURE_COLUMNS_14 if c in df.columns]

    imputer_df = df[feature_cols].copy()

    imputer = IterativeImputer(
        estimator=BayesianRidge(),
        max_iter=max_iter,
        random_state=random_state,
        sample_posterior=False,
        initial_strategy="median",
    )

    imputed_array = imputer.fit_transform(imputer_df)
    imputed_df = pd.DataFrame(imputed_array, columns=feature_cols, index=df.index)

    # Post-imputation clinical domain discretization & bound enforcement
    if "ca" in imputed_df.columns:
        imputed_df["ca"] = imputed_df["ca"].clip(0, 3).round().astype(int)

    if "thal" in imputed_df.columns:
        # Valid thal codes: 3 (normal), 6 (fixed defect), 7 (reversible defect)
        valid_thal = np.array([3, 6, 7])

        def map_to_nearest_thal(val: float) -> int:
            return int(valid_thal[np.argmin(np.abs(valid_thal - val))])

        imputed_df["thal"] = imputed_df["thal"].apply(map_to_nearest_thal).astype(int)

    if "slope" in imputed_df.columns:
        imputed_df["slope"] = imputed_df["slope"].clip(1, 3).round().astype(int)

    if "cp" in imputed_df.columns:
        imputed_df["cp"] = imputed_df["cp"].clip(1, 4).round().astype(int)

    if "restecg" in imputed_df.columns:
        imputed_df["restecg"] = imputed_df["restecg"].clip(0, 2).round().astype(int)

    if "fbs" in imputed_df.columns:
        imputed_df["fbs"] = (imputed_df["fbs"] >= 0.5).astype(int)

    if "exang" in imputed_df.columns:
        imputed_df["exang"] = (imputed_df["exang"] >= 0.5).astype(int)

    if "sex" in imputed_df.columns:
        imputed_df["sex"] = (imputed_df["sex"] >= 0.5).astype(int)

    # Continuous variables rounded to clinical recording standards
    if "age" in imputed_df.columns:
        imputed_df["age"] = imputed_df["age"].round().astype(int)
    if "trestbps" in imputed_df.columns:
        imputed_df["trestbps"] = imputed_df["trestbps"].round().astype(int)
    if "chol" in imputed_df.columns:
        imputed_df["chol"] = imputed_df["chol"].round().astype(int)
    if "thalach" in imputed_df.columns:
        imputed_df["thalach"] = imputed_df["thalach"].round().astype(int)
    if "oldpeak" in imputed_df.columns:
        imputed_df["oldpeak"] = imputed_df["oldpeak"].round(1)

    # Reassemble complete dataframe with target and metadata
    result = df.copy()
    for col in feature_cols:
        result[col] = imputed_df[col]

    return result, imputer


def compute_vif(df: pd.DataFrame, continuous_cols: Optional[List[str]] = None) -> pd.DataFrame:
    """
    Computes Variance Inflation Factor (VIF) on continuous clinical variables
    to assess multi-collinearity.
    """
    if continuous_cols is None:
        continuous_cols = ["age", "trestbps", "chol", "thalach", "oldpeak"]

    subset = df[continuous_cols].dropna()

    vif_raw = [
        variance_inflation_factor(subset.values, i)
        for i in range(len(continuous_cols))
    ]

    # Mean-centered VIF removes intercept inflation
    centered = subset - subset.mean()
    vif_centered = [
        variance_inflation_factor(centered.values, i)
        for i in range(len(continuous_cols))
    ]

    return pd.DataFrame({
        "Feature": continuous_cols,
        "VIF_Raw": np.round(vif_raw, 3),
        "VIF_MeanCentered": np.round(vif_centered, 3),
        "Collinearity_Status": [
            "Low (< 5.0)" if v < 5.0 else "Moderate (5-10)" if v < 10.0 else "High (> 10.0)"
            for v in vif_centered
        ],
    })


def export_cleaned_data(
    df: pd.DataFrame,
    output_dir: str,
    base_filename: str = "heart_disease_cleaned",
) -> Dict[str, str]:
    """
    Exports cleaned dataset to uncorrupted Parquet and Feather formats.
    """
    os.makedirs(output_dir, exist_ok=True)
    parquet_path = os.path.join(output_dir, f"{base_filename}.parquet")
    feather_path = os.path.join(output_dir, f"{base_filename}.feather")

    # Export to Parquet
    df.to_parquet(parquet_path, engine="pyarrow", index=False)

    # Export to Feather
    df.reset_index(drop=True).to_feather(feather_path)

    return {
        "parquet": parquet_path,
        "feather": feather_path,
    }
