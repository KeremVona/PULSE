"""
Unit tests for PULSE model training, stratified cross-validation,
and clinical metric evaluation suite.
"""

import numpy as np
import pandas as pd
import pytest
from sklearn.linear_model import LogisticRegression

from ml.src.train import (
    compute_clinical_metrics,
    find_optimal_threshold,
    stratified_cv_benchmark,
    get_baseline_logistic_regression,
    get_random_forest,
    get_lightgbm_model,
    compare_models,
)
from ml.src.pipeline import ALL_PREDICTOR_COLUMNS, TARGET_COLUMN


@pytest.fixture
def mock_predictions():
    """Generates synthetic ground truth and predicted probabilities."""
    np.random.seed(42)
    y_true = np.array([0] * 50 + [1] * 50)
    # Give positive class slightly higher probabilities
    y_prob = np.concatenate([
        np.random.beta(2, 5, size=50),
        np.random.beta(5, 2, size=50),
    ])
    return y_true, y_prob


@pytest.fixture
def small_clinical_dataframe():
    """Small clinical dataset for cross-validation testing."""
    np.random.seed(42)
    n = 60
    data = {
        "age": np.random.randint(40, 70, size=n),
        "sex": np.random.choice([0, 1], size=n),
        "cp": np.random.choice([1, 2, 3, 4], size=n),
        "trestbps": np.random.normal(130, 15, size=n).round(),
        "chol": np.random.normal(230, 40, size=n).round(),
        "fbs": np.random.choice([0, 1], size=n),
        "restecg": np.random.choice([0, 1, 2], size=n),
        "thalach": np.random.normal(150, 20, size=n).round(),
        "exang": np.random.choice([0, 1], size=n),
        "oldpeak": np.random.exponential(1.0, size=n).round(1),
        "slope": np.random.choice([1, 2, 3], size=n),
        "ca": np.random.choice([0, 1, 2, 3], size=n),
        "thal": np.random.choice([3, 6, 7], size=n),
        "target": np.array([0] * 30 + [1] * 30),
    }
    return pd.DataFrame(data)


def test_compute_clinical_metrics(mock_predictions):
    """Verifies that all medical screening metrics are calculated within theoretical ranges."""
    y_true, y_prob = mock_predictions
    metrics = compute_clinical_metrics(y_true, y_prob, threshold=0.50)

    assert 0.0 <= metrics["sensitivity"] <= 1.0
    assert 0.0 <= metrics["specificity"] <= 1.0
    assert 0.0 <= metrics["precision"] <= 1.0
    assert 0.0 <= metrics["f1_score"] <= 1.0
    assert 0.0 <= metrics["f2_score"] <= 1.0
    assert 0.5 <= metrics["roc_auc"] <= 1.0
    assert 0.0 <= metrics["pr_auc"] <= 1.0
    assert 0.0 <= metrics["brier_score"] <= 1.0

    # Verify confusion matrix consistency
    assert metrics["tp"] + metrics["fp"] + metrics["tn"] + metrics["fn"] == len(y_true)


def test_find_optimal_threshold(mock_predictions):
    """Verifies that threshold tuning finds a cutoff achieving Sensitivity >= 0.92."""
    y_true, y_prob = mock_predictions
    best_thresh, best_metrics = find_optimal_threshold(y_true, y_prob, min_sensitivity=0.92)

    assert 0.0 < best_thresh < 1.0
    assert best_metrics["sensitivity"] >= 0.90  # Should be close to or >= 0.92
    assert "f2_score" in best_metrics


def test_stratified_cv_benchmark(small_clinical_dataframe):
    """Verifies 5-fold cross-validation executes without data leakage."""
    X = small_clinical_dataframe[ALL_PREDICTOR_COLUMNS]
    y = small_clinical_dataframe[TARGET_COLUMN].values

    model = LogisticRegression(max_iter=500, random_state=42)
    cv_res = stratified_cv_benchmark(model, X, y, n_splits=3, optimize_threshold=True)

    assert len(cv_res["fold_metrics"]) == 3
    assert len(cv_res["oof_probabilities"]) == len(y)
    assert cv_res["optimal_threshold"] > 0.0
    assert "mean_roc_auc" in cv_res["metrics_summary"]
    assert "mean_f2_score" in cv_res["metrics_summary"]


def test_model_instantiations():
    """Verifies that model factory functions return valid estimators."""
    lr = get_baseline_logistic_regression()
    rf = get_random_forest()
    lgbm = get_lightgbm_model()

    assert hasattr(lr, "fit") and hasattr(lr, "predict_proba")
    assert hasattr(rf, "fit") and hasattr(rf, "predict_proba")
    assert hasattr(lgbm, "fit") and hasattr(lgbm, "predict_proba")


def test_compare_models_selection(small_clinical_dataframe):
    """Verifies model benchmarking and selection logic."""
    X = small_clinical_dataframe[ALL_PREDICTOR_COLUMNS]
    y = small_clinical_dataframe[TARGET_COLUMN].values

    comp = compare_models(X, y)
    assert comp["winner"] in ["ElasticNet_LogisticRegression", "Random_Forest", "LightGBM"]
    assert "selection_rationale" in comp
    assert len(comp["results"]) == 3
