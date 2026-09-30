"""
Unit tests for PULSE feature transformation and preprocessing pipeline.
Verifies train/test leakage isolation, RobustScaler behavior on clinical outliers,
OneHotEncoder category handling, feature name preservation for SHAP, and serialization.
"""

import os
import tempfile
import numpy as np
import pandas as pd
import pytest
from sklearn.preprocessing import RobustScaler, StandardScaler

from ml.src.pipeline import (
    ALL_PREDICTOR_COLUMNS,
    CONTINUOUS_FEATURES,
    CATEGORICAL_FEATURES,
    BINARY_FEATURES,
    ORDINAL_FEATURES,
    CLINICAL_LABEL_MAP,
    PULSEPreprocessor,
    make_column_transformer,
    split_train_test_clinical,
)


@pytest.fixture
def synthetic_clinical_dataset():
    """Generates a realistic synthetic clinical DataFrame matching UCI schema."""
    np.random.seed(42)
    n = 100

    data = {
        "age": np.random.randint(30, 75, size=n),
        "sex": np.random.choice([0, 1], size=n),
        "cp": np.random.choice([1, 2, 3, 4], size=n),
        "trestbps": np.random.normal(130, 18, size=n).round(),
        "chol": np.random.normal(240, 50, size=n).round(),
        "fbs": np.random.choice([0, 1], size=n, p=[0.85, 0.15]),
        "restecg": np.random.choice([0, 1, 2], size=n),
        "thalach": np.random.normal(150, 22, size=n).round(),
        "exang": np.random.choice([0, 1], size=n, p=[0.7, 0.3]),
        "oldpeak": np.random.exponential(1.0, size=n).round(1),
        "slope": np.random.choice([1, 2, 3], size=n),
        "ca": np.random.choice([0, 1, 2, 3], size=n, p=[0.6, 0.2, 0.15, 0.05]),
        "thal": np.random.choice([3, 6, 7], size=n),
        "target": np.random.choice([0, 1], size=n),
    }

    # Inject a realistic clinical cholesterol outlier (familial hypercholesterolemia)
    data["chol"][0] = 580.0

    return pd.DataFrame(data)


def test_train_test_split_stratification(synthetic_clinical_dataset):
    """Verifies that split_train_test_clinical stratifies correctly and isolates features."""
    X_train, X_test, y_train, y_test = split_train_test_clinical(
        synthetic_clinical_dataset, test_size=0.20, random_state=42, stratify=True
    )

    assert len(X_train) == 80
    assert len(X_test) == 20
    assert len(y_train) == 80
    assert len(y_test) == 20

    # Verify no target in feature matrices (prevention of direct leakage)
    assert "target" not in X_train.columns
    assert "target" not in X_test.columns

    # Verify identical class prevalence across splits (stratification)
    train_prev = y_train.mean()
    test_prev = y_test.mean()
    assert abs(train_prev - test_prev) < 0.05


def test_fit_transform_separation_no_data_leakage(synthetic_clinical_dataset):
    """Verifies that transform on test set uses strictly training parameters without refitting."""
    X_train, X_test, y_train, y_test = split_train_test_clinical(
        synthetic_clinical_dataset, test_size=0.20, random_state=42
    )

    preprocessor = PULSEPreprocessor()
    preprocessor.fit(X_train)

    # Extract fitted medians from RobustScaler
    cont_scaler = preprocessor.transformer.named_transformers_["cont"]
    fitted_train_medians = cont_scaler.center_.copy()

    # Transform test set
    _ = preprocessor.transform(X_test)

    # Confirm training parameters did NOT change during test transform
    np.testing.assert_array_equal(cont_scaler.center_, fitted_train_medians)


def test_robust_scaler_outlier_resilience():
    """
    Demonstrates that RobustScaler median remains stable (0.0) in the presence of extreme outliers,
    whereas StandardScaler shifts and distorts normal patient representations.
    """
    normal_chol = [180.0, 200.0, 210.0, 220.0, 240.0, 250.0, 260.0, 280.0, 300.0]
    outlier_chol = normal_chol + [600.0]  # Extreme outlier

    df_normal = pd.DataFrame({"chol": normal_chol})
    df_outlier = pd.DataFrame({"chol": outlier_chol})

    # RobustScaler on both
    robust_normal = RobustScaler().fit(df_normal)
    robust_outlier = RobustScaler().fit(df_outlier)

    # StandardScaler on both
    std_normal = StandardScaler().fit(df_normal)
    std_outlier = StandardScaler().fit(df_outlier)

    # Median shift under RobustScaler is minimal (outlier changes median by at most half step)
    assert abs(robust_normal.center_[0] - robust_outlier.center_[0]) <= 10.0

    # Mean shift under StandardScaler is substantially larger
    assert abs(std_normal.mean_[0] - std_outlier.mean_[0]) > 35.0


def test_one_hot_encoding_and_unseen_categories(synthetic_clinical_dataset):
    """Verifies that OneHotEncoder handles unknown/unseen categories gracefully."""
    X_train = synthetic_clinical_dataset.iloc[:80].copy()
    X_test = synthetic_clinical_dataset.iloc[80:].copy()

    preprocessor = PULSEPreprocessor()
    preprocessor.fit(X_train)

    # Inject an unseen category into X_test (e.g., unusual thal code 99)
    X_test_unseen = X_test.copy()
    X_test_unseen.loc[X_test_unseen.index[0], "thal"] = 99

    # Should not raise ValueError because handle_unknown='ignore'
    transformed = preprocessor.transform(X_test_unseen)
    assert transformed.shape[0] == len(X_test)


def test_feature_names_preservation_and_clinical_labels(synthetic_clinical_dataset):
    """Verifies that transformed features retain clean names and map to clinical labels."""
    preprocessor = PULSEPreprocessor()
    df_transformed = preprocessor.fit_transform_to_dataframe = preprocessor.fit(synthetic_clinical_dataset).transform_to_dataframe(synthetic_clinical_dataset)

    feature_names = preprocessor.get_feature_names()
    clinical_labels = preprocessor.get_clinical_labels()

    assert len(feature_names) == df_transformed.shape[1]
    assert len(clinical_labels) == len(feature_names)

    # Verify critical technical keys needed for SHAP TreeExplainer are present
    assert "oldpeak" in feature_names
    assert "thalach" in feature_names
    assert "ca" in feature_names
    assert any("cp_" in f for f in feature_names)
    assert any("slope_" in f for f in feature_names)
    assert any("thal_" in f for f in feature_names)

    # Verify clinical label mapping
    oldpeak_idx = feature_names.index("oldpeak")
    assert clinical_labels[oldpeak_idx] == "ST Depression (mm)"

    ca_idx = feature_names.index("ca")
    assert clinical_labels[ca_idx] == "Major Fluoroscopy Vessels Colored (0-3)"


def test_preprocessor_serialization_and_reloading(synthetic_clinical_dataset):
    """Verifies that fitted preprocessor can be saved and loaded with bit-identical outputs."""
    preprocessor = PULSEPreprocessor()
    preprocessor.fit(synthetic_clinical_dataset)
    original_output = preprocessor.transform(synthetic_clinical_dataset)

    with tempfile.TemporaryDirectory() as tmpdir:
        artifact_path = os.path.join(tmpdir, "preprocessor.joblib")
        saved_path = preprocessor.save(artifact_path)
        assert os.path.exists(saved_path)

        loaded_preprocessor = PULSEPreprocessor.load(saved_path)
        assert loaded_preprocessor.is_fitted_
        assert loaded_preprocessor.get_feature_names() == preprocessor.get_feature_names()

        loaded_output = loaded_preprocessor.transform(synthetic_clinical_dataset)
        np.testing.assert_allclose(original_output, loaded_output, rtol=1e-12, atol=1e-12)
