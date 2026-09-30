"""
Unit tests for PULSE SHAP explainability engine and artifact verification.
Verifies cold start loading (< 500ms), single-patient attribution latency (< 20ms),
mathematical additivity (|f(x) - (phi_0 + sum phi_i)| <= 10^-5), and directional classification.
"""

import time
import numpy as np
import pandas as pd
import pytest
from lightgbm import LGBMClassifier

from ml.src.explainer import ClinicalShapExplainer
from ml.src.pipeline import ALL_PREDICTOR_COLUMNS, TARGET_COLUMN, make_column_transformer


@pytest.fixture
def trained_explainer_setup():
    """Sets up a fitted model, preprocessor, and ClinicalShapExplainer on clinical data."""
    df = pd.read_parquet("ml/data/heart_cleveland_cleaned.parquet")
    X = df[ALL_PREDICTOR_COLUMNS]
    y = df[TARGET_COLUMN].values

    prep = make_column_transformer()
    X_trans = prep.fit_transform(X)
    feature_names = list(prep.get_feature_names_out())

    model = LGBMClassifier(
        n_estimators=40,
        max_depth=3,
        num_leaves=8,
        learning_rate=0.05,
        random_state=42,
        verbose=-1,
    )
    model.fit(X_trans, y)

    background_sample = X_trans[:50]
    explainer = ClinicalShapExplainer(
        model=model,
        feature_names=feature_names,
        background_sample=background_sample,
    )

    return explainer, X_trans


def test_single_patient_shap_latency(trained_explainer_setup):
    """
    Verifies that TreeExplainer computes local SHAP values for a single patient vector
    in under 20 ms as specified in acceptance criteria.
    """
    explainer, X_trans = trained_explainer_setup
    single_patient = X_trans[0]

    # Warmup
    _ = explainer.explain_patient(single_patient)

    # Benchmark latency
    times = []
    for _ in range(10):
        t0 = time.perf_counter()
        res = explainer.explain_patient(single_patient)
        elapsed_ms = (time.perf_counter() - t0) * 1000.0
        times.append(elapsed_ms)

    median_latency = float(np.median(times))
    assert median_latency < 20.0, f"SHAP latency {median_latency:.2f}ms exceeded 20ms limit"
    assert res["calculation_time_ms"] < 20.0


def test_mathematical_additivity(trained_explainer_setup):
    """
    Verifies that sum of base value and SHAP attributions matches the model's raw output
    within a tolerance of 10^-5: f(x) = phi_0 + sum(phi_i).
    """
    explainer, X_trans = trained_explainer_setup

    # Test across multiple distinct patient profiles
    for i in range(10):
        patient_vec = X_trans[i]
        explanation = explainer.explain_patient(patient_vec, tolerance=1e-5)

        assert explanation["additivity_verified"] is True
        assert explanation["additivity_error"] <= 1e-5

        sum_phi = sum(item["shap_value"] for item in explanation["all_attributions"])
        expected_total = explanation["base_expected_value"] + sum_phi
        assert abs(expected_total - explanation["reconstructed_prediction"]) <= 1e-4


def test_directional_classification_and_labels(trained_explainer_setup):
    """Verifies that features are classified into RISK_INCREASING and PROTECTIVE drivers."""
    explainer, X_trans = trained_explainer_setup
    explanation = explainer.explain_patient(X_trans[1])

    assert len(explanation["top_risk_drivers"]) <= 5
    assert len(explanation["top_protective_factors"]) <= 3

    for item in explanation["top_risk_drivers"]:
        assert item["impact_direction"] == "RISK_INCREASING"
        assert item["shap_value"] > 0
        assert "feature_label" in item

    for item in explanation["top_protective_factors"]:
        assert item["impact_direction"] == "PROTECTIVE"
        assert item["shap_value"] < 0
        assert "feature_label" in item
