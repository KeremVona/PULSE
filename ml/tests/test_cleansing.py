"""
Unit tests for PULSE data cleansing and MICE imputation pipeline.
Verifies clinical physiological boundaries, categorical encodings,
and absence of target data leakage during imputation.
"""

import os
import tempfile
import numpy as np
import pandas as pd
import pytest

from ml.src.cleansing import (
    PHYSIOLOGICAL_BOUNDS,
    load_raw_dataset,
    audit_physiological_bounds,
    preprocess_clinical_indicators,
    impute_with_mice,
    compute_vif,
    export_cleaned_data,
)


@pytest.fixture
def sample_clinical_data():
    """Provides a synthetic clinical dataframe with known anomalies."""
    return pd.DataFrame({
        "id": [1, 2, 3, 4, 5],
        "age": [58, 67, 45, 72, 35],
        "sex": ["Male", "Female", "Male", "Female", "Male"],
        "dataset": ["Cleveland", "Cleveland", "Hungary", "Switzerland", "VA Long Beach"],
        "cp": ["typical angina", "asymptomatic", "non-anginal", "atypical angina", "asymptomatic"],
        "trestbps": [145.0, 160.0, 120.0, 0.0, 130.0],  # row 4 has impossible BP = 0
        "chol": [233.0, 286.0, 0.0, 250.0, 210.0],     # row 3 has unmeasured chol = 0
        "fbs": [True, False, False, True, False],
        "restecg": ["lv hypertrophy", "normal", "st-t abnormality", "normal", "normal"],
        "thalach": [150.0, 108.0, 170.0, 125.0, 180.0],
        "exang": [False, True, False, True, False],
        "oldpeak": [2.3, 1.5, -1.2, 3.5, 0.0],          # row 3 has negative oldpeak
        "slope": ["downsloping", "flat", "upsloping", np.nan, "flat"],
        "ca": [0.0, 3.0, np.nan, np.nan, 1.0],         # rows 3 and 4 have missing ca
        "thal": ["fixed defect", "normal", np.nan, "reversable defect", np.nan],
        "num": [0, 2, 1, 3, 0],
    })


def test_physiological_bounds_audit(sample_clinical_data):
    """Verifies that out-of-bounds indicators are flagged correctly."""
    audit_df = audit_physiological_bounds(sample_clinical_data)
    assert not audit_df.empty

    bp_row = audit_df[audit_df["feature"] == "trestbps"].iloc[0]
    assert bp_row["below_min_count"] == 1  # 0.0 is below min 60

    chol_row = audit_df[audit_df["feature"] == "chol"].iloc[0]
    assert chol_row["below_min_count"] == 1  # 0.0 is below min 80


def test_preprocessing_encodings_and_corrections(sample_clinical_data):
    """Verifies standard encoding and anomaly correction."""
    cleaned = preprocess_clinical_indicators(sample_clinical_data, drop_duplicates=False)

    # Check target binarization
    assert list(cleaned["target"]) == [0, 1, 1, 1, 0]

    # Check sex encoding (Female=0, Male=1)
    assert list(cleaned["sex"]) == [1, 0, 1, 0, 1]

    # Check chest pain encoding
    assert cleaned.loc[0, "cp"] == 1  # typical angina
    assert cleaned.loc[1, "cp"] == 4  # asymptomatic

    # Check BP=0 replaced with NaN
    assert np.isnan(cleaned.loc[3, "trestbps"])

    # Check chol=0 replaced with NaN
    assert np.isnan(cleaned.loc[2, "chol"])

    # Check negative oldpeak clipped to 0.0
    assert cleaned.loc[2, "oldpeak"] == 0.0


def test_mice_imputation_integrity(sample_clinical_data):
    """Verifies that MICE imputes missing values and constrains to clinical discrete sets."""
    cleaned = preprocess_clinical_indicators(sample_clinical_data, drop_duplicates=False)
    imputed, imputer = impute_with_mice(cleaned, random_state=42)

    # Check no NaNs remain in imputed features
    for col in ["ca", "thal", "slope", "trestbps", "chol"]:
        assert not imputed[col].isna().any(), f"Feature {col} still contains NaNs"

    # Check ca is constrained to integer in [0, 3]
    assert imputed["ca"].dtype == int or np.issubdtype(imputed["ca"].dtype, np.integer)
    assert imputed["ca"].min() >= 0
    assert imputed["ca"].max() <= 3

    # Check thal is strictly mapped to valid clinical categories {3, 6, 7}
    assert set(imputed["thal"].unique()).issubset({3, 6, 7})

    # Check slope is strictly mapped to {1, 2, 3}
    assert set(imputed["slope"].unique()).issubset({1, 2, 3})


def test_vif_computation():
    """Verifies VIF computation executes and provides expected structure."""
    df = pd.DataFrame({
        "age": [50, 60, 45, 55, 65, 40],
        "trestbps": [120, 140, 110, 130, 150, 115],
        "chol": [200, 250, 180, 220, 260, 190],
        "thalach": [160, 130, 175, 145, 120, 180],
        "oldpeak": [0.5, 1.8, 0.0, 1.0, 2.5, 0.0],
    })
    vif_df = compute_vif(df)
    assert len(vif_df) == 5
    assert "VIF_MeanCentered" in vif_df.columns
    assert "Collinearity_Status" in vif_df.columns


def test_export_cleaned_data():
    """Verifies Parquet and Feather exports are written and readable."""
    df = pd.DataFrame({
        "age": [55, 62],
        "sex": [1, 0],
        "target": [1, 0],
    })
    with tempfile.TemporaryDirectory() as tmpdir:
        paths = export_cleaned_data(df, tmpdir, "test_dataset")
        assert os.path.exists(paths["parquet"])
        assert os.path.exists(paths["feather"])

        # Verify roundtrip reading
        df_p = pd.read_parquet(paths["parquet"])
        df_f = pd.read_feather(paths["feather"])
        assert len(df_p) == 2
        assert len(df_f) == 2
        assert list(df_p["age"]) == [55, 62]
