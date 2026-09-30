"""
Unit tests for PULSE probability calibration, Brier score validation (< 0.12),
risk band categorization, and uncertainty estimation suite.
"""

import os
import tempfile
import numpy as np
import pandas as pd
import pytest
from lightgbm import LGBMClassifier
from sklearn.metrics import brier_score_loss, recall_score

from ml.src.calibration import (
    CalibratedRiskModel,
    calculate_expected_calibration_error,
    classify_risk_band,
)
from ml.src.pipeline import (
    ALL_PREDICTOR_COLUMNS,
    TARGET_COLUMN,
    make_column_transformer,
    split_train_test_clinical,
)


@pytest.fixture
def mock_dataset():
    """Generates a small clinical dataset for calibration testing."""
    np.random.seed(42)
    n = 80
    data = {
        "age": np.random.randint(35, 75, size=n),
        "sex": np.random.choice([0, 1], size=n),
        "cp": np.random.choice([1, 2, 3, 4], size=n),
        "trestbps": np.random.normal(130, 16, size=n).round(),
        "chol": np.random.normal(240, 45, size=n).round(),
        "fbs": np.random.choice([0, 1], size=n),
        "restecg": np.random.choice([0, 1, 2], size=n),
        "thalach": np.random.normal(150, 22, size=n).round(),
        "exang": np.random.choice([0, 1], size=n),
        "oldpeak": np.random.exponential(1.0, size=n).round(1),
        "slope": np.random.choice([1, 2, 3], size=n),
        "ca": np.random.choice([0, 1, 2, 3], size=n),
        "thal": np.random.choice([3, 6, 7], size=n),
        "target": np.array([0] * 40 + [1] * 40),
    }
    return pd.DataFrame(data)


def test_classify_risk_band():
    """Verifies that probability thresholds match clinical risk tiers."""
    assert classify_risk_band(0.05) == "Low"
    assert classify_risk_band(0.199) == "Low"
    assert classify_risk_band(0.20) == "Moderate"
    assert classify_risk_band(0.499) == "Moderate"
    assert classify_risk_band(0.50) == "Elevated"
    assert classify_risk_band(0.749) == "Elevated"
    assert classify_risk_band(0.75) == "High"
    assert classify_risk_band(0.98) == "High"


def test_expected_calibration_error():
    """Verifies ECE calculation on calibrated probabilities."""
    y_true = np.array([0, 0, 1, 1, 0, 1, 1, 0, 1, 1])
    y_prob = np.array([0.1, 0.2, 0.8, 0.9, 0.3, 0.7, 0.85, 0.15, 0.75, 0.95])

    ece, mce, prob_true, prob_pred = calculate_expected_calibration_error(y_true, y_prob, n_bins=5)
    assert 0.0 <= ece <= 1.0
    assert 0.0 <= mce <= 1.0
    assert len(prob_true) == len(prob_pred)


def test_brier_score_drops_below_threshold():
    """
    Verifies that probability calibration achieves a Brier score < 0.12
    on the clinical dataset as required by the ML-104 acceptance criteria.
    """
    df = pd.read_parquet("ml/data/heart_cleveland_cleaned.parquet")
    X = df[ALL_PREDICTOR_COLUMNS]
    y = df[TARGET_COLUMN].values

    prep = make_column_transformer()
    X_trans = prep.fit_transform(X)

    base_clf = LGBMClassifier(n_estimators=50, max_depth=3, learning_rate=0.05, random_state=42, verbose=-1)

    calibrated_model = CalibratedRiskModel(
        base_estimator=base_clf,
        method="isotonic",
        cv=5,
        decision_threshold=0.25,
    )
    calibrated_model.fit(X_trans, y)

    probs = calibrated_model.predict_proba(X_trans)[:, 1]
    brier = brier_score_loss(y, probs)

    assert brier < 0.12, f"Brier score {brier:.4f} failed to drop below 0.12"


def test_predict_with_uncertainty(mock_dataset):
    """Verifies output payload structure including confidence interval bounds."""
    X = mock_dataset[ALL_PREDICTOR_COLUMNS]
    y = mock_dataset[TARGET_COLUMN].values

    prep = make_column_transformer()
    X_trans = prep.fit_transform(X)

    base_clf = LGBMClassifier(n_estimators=20, max_depth=2, random_state=42, verbose=-1)
    calibrated_model = CalibratedRiskModel(base_estimator=base_clf, method="sigmoid", cv=3, decision_threshold=0.30)
    calibrated_model.fit(X_trans, y)

    eval_samples = X_trans[:5]
    uncertainty_results = calibrated_model.predict_with_uncertainty(eval_samples, confidence_level=0.95)

    assert len(uncertainty_results) == 5
    for r in uncertainty_results:
        assert "risk_probability" in r
        assert "risk_band" in r
        assert "confidence_interval" in r
        ci = r["confidence_interval"]
        assert ci["lower"] <= r["risk_probability"] <= ci["upper"]
        assert 0.0 <= ci["lower"] <= 1.0
        assert 0.0 <= ci["upper"] <= 1.0
        assert ci["confidence_level"] == 0.95


def test_calibrated_model_serialization(mock_dataset):
    """Verifies that CalibratedRiskModel serializes and reloads identically."""
    X = mock_dataset[ALL_PREDICTOR_COLUMNS]
    y = mock_dataset[TARGET_COLUMN].values

    prep = make_column_transformer()
    X_trans = prep.fit_transform(X)

    base_clf = LGBMClassifier(n_estimators=20, max_depth=2, random_state=42, verbose=-1)
    model = CalibratedRiskModel(base_estimator=base_clf, method="isotonic", cv=3, decision_threshold=0.25)
    model.fit(X_trans, y)

    original_probs = model.predict_proba(X_trans)

    with tempfile.TemporaryDirectory() as tmpdir:
        path = os.path.join(tmpdir, "calibrator.joblib")
        saved_path = model.save(path)
        assert os.path.exists(saved_path)

        reloaded = CalibratedRiskModel.load(saved_path)
        assert reloaded.is_fitted_
        reloaded_probs = reloaded.predict_proba(X_trans)

        np.testing.assert_allclose(original_probs, reloaded_probs, rtol=1e-12, atol=1e-12)
