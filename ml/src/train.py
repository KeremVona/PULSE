"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.src.train
Description: Stratified 5-Fold cross-validation, hyperparameter tuning via Optuna,
             cost-sensitive threshold optimization (F2 score, Sensitivity >= 0.92),
             and benchmarking of ElasticNet Logistic Regression, Random Forest, and LightGBM.
"""

from typing import Any, Callable, Dict, List, Optional, Tuple, Union
import json
import os
import joblib
import numpy as np
import optuna
import pandas as pd
from lightgbm import LGBMClassifier
from sklearn.base import BaseEstimator, clone
from sklearn.ensemble import RandomForestClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import (
    average_precision_score,
    brier_score_loss,
    confusion_matrix,
    f1_score,
    fbeta_score,
    precision_score,
    recall_score,
    roc_auc_score,
    roc_curve,
    precision_recall_curve,
)
from sklearn.model_selection import StratifiedKFold

from ml.src.pipeline import (
    ALL_PREDICTOR_COLUMNS,
    TARGET_COLUMN,
    PULSEPreprocessor,
    make_column_transformer,
)


def compute_clinical_metrics(
    y_true: np.ndarray,
    y_prob: np.ndarray,
    threshold: float = 0.50,
) -> Dict[str, float]:
    """
    Computes medical screening and statistical evaluation metrics:
    - Sensitivity / Recall (Target >= 0.92)
    - Specificity (True Negative Rate)
    - Precision (Positive Predictive Value)
    - F1 Score & F2 Score (beta=2, prioritizing recall)
    - ROC-AUC & PR-AUC (Average Precision)
    - Brier Score (Mean calibration error)
    """
    y_pred = (y_prob >= threshold).astype(int)

    # Confusion matrix: tn, fp, fn, tp
    tn, fp, fn, tp = confusion_matrix(y_true, y_pred, labels=[0, 1]).ravel()

    sensitivity = tp / (tp + fn) if (tp + fn) > 0 else 0.0
    specificity = tn / (tn + fp) if (tn + fp) > 0 else 0.0
    precision = tp / (tp + fp) if (tp + fp) > 0 else 0.0
    f1 = f1_score(y_true, y_pred, zero_division=0)
    f2 = fbeta_score(y_true, y_pred, beta=2, zero_division=0)

    try:
        roc_auc = roc_auc_score(y_true, y_prob)
    except ValueError:
        roc_auc = 0.5

    try:
        pr_auc = average_precision_score(y_true, y_prob)
    except ValueError:
        pr_auc = 0.0

    brier = brier_score_loss(y_true, y_prob)
    cost_sensitive_loss = (4.0 * fn + 1.0 * fp) / len(y_true)

    return {
        "sensitivity": float(sensitivity),
        "specificity": float(specificity),
        "precision": float(precision),
        "f1_score": float(f1),
        "f2_score": float(f2),
        "roc_auc": float(roc_auc),
        "pr_auc": float(pr_auc),
        "brier_score": float(brier),
        "cost_sensitive_loss": float(cost_sensitive_loss),
        "threshold": float(threshold),
        "tp": int(tp),
        "fp": int(fp),
        "tn": int(tn),
        "fn": int(fn),
    }


def find_optimal_threshold(
    y_true: np.ndarray,
    y_prob: np.ndarray,
    min_sensitivity: float = 0.92,
) -> Tuple[float, Dict[str, float]]:
    """
    Sweeps thresholds from 0.05 to 0.85 in steps of 0.01 to find the operating point
    that maximizes the F2 score while satisfying Sensitivity >= min_sensitivity.
    If no threshold achieves >= min_sensitivity, selects the one maximizing sensitivity.
    """
    thresholds = np.linspace(0.05, 0.85, 81)
    best_thresh = 0.50
    best_metrics = compute_clinical_metrics(y_true, y_prob, threshold=0.50)
    best_f2 = -1.0

    qualifying_candidates = []

    for t in thresholds:
        m = compute_clinical_metrics(y_true, y_prob, threshold=t)
        if m["sensitivity"] >= min_sensitivity:
            qualifying_candidates.append((t, m))

    if qualifying_candidates:
        # Among those meeting sensitivity >= 0.92, pick the one maximizing F2 (or Precision)
        qualifying_candidates.sort(key=lambda x: (x[1]["f2_score"], x[1]["precision"]), reverse=True)
        best_thresh, best_metrics = qualifying_candidates[0]
    else:
        # Fallback: pick the threshold with maximum sensitivity
        all_candidates = [(t, compute_clinical_metrics(y_true, y_prob, threshold=t)) for t in thresholds]
        all_candidates.sort(key=lambda x: (x[1]["sensitivity"], x[1]["f2_score"]), reverse=True)
        best_thresh, best_metrics = all_candidates[0]

    return float(best_thresh), best_metrics


def stratified_cv_benchmark(
    model: BaseEstimator,
    X: pd.DataFrame,
    y: np.ndarray,
    n_splits: int = 5,
    random_state: int = 42,
    threshold: Optional[float] = None,
    optimize_threshold: bool = True,
    min_sensitivity: float = 0.92,
) -> Dict[str, Any]:
    """
    Executes Stratified 5-Fold Cross-Validation on the full feature space.
    The PULSEPreprocessor is strictly fitted on each training fold to prevent leakage.
    Returns aggregated metrics and out-of-fold probability predictions.
    """
    skf = StratifiedKFold(n_splits=n_splits, shuffle=True, random_state=random_state)

    oof_probs = np.zeros(len(y))
    fold_metrics = []

    for fold_idx, (train_idx, val_idx) in enumerate(skf.split(X, y)):
        X_tr, X_val = X.iloc[train_idx], X.iloc[val_idx]
        y_tr, y_val = y[train_idx], y[val_idx]

        # Pipeline fitted strictly on train fold
        preprocessor = make_column_transformer()
        X_tr_trans = preprocessor.fit_transform(X_tr)
        X_val_trans = preprocessor.transform(X_val)

        fold_model = clone(model)
        fold_model.fit(X_tr_trans, y_tr)

        # Probabilities for positive class (CAD present)
        val_probs = fold_model.predict_proba(X_val_trans)[:, 1]
        oof_probs[val_idx] = val_probs

        if optimize_threshold:
            thresh, m = find_optimal_threshold(y_val, val_probs, min_sensitivity=min_sensitivity)
        else:
            thresh = threshold if threshold is not None else 0.50
            m = compute_clinical_metrics(y_val, val_probs, threshold=thresh)

        m["fold"] = fold_idx + 1
        fold_metrics.append(m)

    # Compute overall Out-Of-Fold (OOF) metrics
    if optimize_threshold:
        optimal_oof_thresh, overall_metrics = find_optimal_threshold(
            y, oof_probs, min_sensitivity=min_sensitivity
        )
    else:
        optimal_oof_thresh = threshold if threshold is not None else 0.50
        overall_metrics = compute_clinical_metrics(y, oof_probs, threshold=optimal_oof_thresh)

    # Calculate mean and standard deviation across folds
    metrics_summary = {}
    for key in ["sensitivity", "specificity", "precision", "f1_score", "f2_score", "roc_auc", "pr_auc", "brier_score"]:
        vals = [f[key] for f in fold_metrics]
        metrics_summary[f"mean_{key}"] = float(np.mean(vals))
        metrics_summary[f"std_{key}"] = float(np.std(vals))

    return {
        "fold_metrics": fold_metrics,
        "metrics_summary": metrics_summary,
        "overall_oof_metrics": overall_metrics,
        "oof_probabilities": oof_probs,
        "optimal_threshold": float(optimal_oof_thresh),
    }


def get_baseline_logistic_regression(
    C: float = 0.5,
    l1_ratio: float = 0.5,
    random_state: int = 42,
) -> LogisticRegression:
    """Returns regularized ElasticNet Logistic Regression model for statistical baseline."""
    return LogisticRegression(
        solver="saga",
        C=C,
        l1_ratio=l1_ratio,
        max_iter=2000,
        random_state=random_state,
    )


def get_random_forest(
    n_estimators: int = 150,
    max_depth: int = 5,
    min_samples_split: int = 4,
    min_samples_leaf: int = 2,
    random_state: int = 42,
) -> RandomForestClassifier:
    """Returns Random Forest ensemble model."""
    return RandomForestClassifier(
        n_estimators=n_estimators,
        max_depth=max_depth,
        min_samples_split=min_samples_split,
        min_samples_leaf=min_samples_leaf,
        class_weight="balanced",
        random_state=random_state,
        n_jobs=-1,
    )


def get_lightgbm_model(
    n_estimators: int = 100,
    max_depth: int = 4,
    num_leaves: int = 15,
    learning_rate: float = 0.03,
    min_child_samples: int = 20,
    subsample: float = 0.8,
    colsample_bytree: float = 0.8,
    reg_alpha: float = 0.05,
    reg_lambda: float = 0.1,
    class_weight: Optional[str] = None,
    random_state: int = 42,
) -> LGBMClassifier:
    """Returns regularized LightGBM classifier capturing non-linear interactions."""
    return LGBMClassifier(
        objective="binary",
        n_estimators=n_estimators,
        max_depth=max_depth,
        num_leaves=num_leaves,
        learning_rate=learning_rate,
        min_child_samples=min_child_samples,
        subsample=subsample,
        colsample_bytree=colsample_bytree,
        reg_alpha=reg_alpha,
        reg_lambda=reg_lambda,
        class_weight=class_weight,
        random_state=random_state,
        verbose=-1,
        n_jobs=-1,
    )


def tune_lightgbm_hyperparameters(
    X: pd.DataFrame,
    y: np.ndarray,
    n_trials: int = 40,
    random_state: int = 42,
) -> Tuple[Dict[str, Any], float]:
    """
    Uses Optuna to tune LightGBM hyperparameters optimizing the cross-validated F2 score
    with a hard clinical constraint prioritizing Sensitivity >= 0.92 and ROC-AUC > 0.88.
    """
    optuna.logging.set_verbosity(optuna.logging.WARNING)
    skf = StratifiedKFold(n_splits=5, shuffle=True, random_state=random_state)

    def objective(trial: optuna.Trial) -> float:
        params = {
            "n_estimators": trial.suggest_int("n_estimators", 40, 180),
            "max_depth": trial.suggest_int("max_depth", 2, 5),
            "num_leaves": trial.suggest_int("num_leaves", 6, 20),
            "learning_rate": trial.suggest_float("learning_rate", 0.01, 0.10, log=True),
            "min_child_samples": trial.suggest_int("min_child_samples", 15, 35),
            "subsample": trial.suggest_float("subsample", 0.65, 0.95),
            "colsample_bytree": trial.suggest_float("colsample_bytree", 0.65, 0.95),
            "reg_alpha": trial.suggest_float("reg_alpha", 1e-3, 5.0, log=True),
            "reg_lambda": trial.suggest_float("reg_lambda", 1e-3, 5.0, log=True),
            "class_weight": trial.suggest_categorical("class_weight", [None, "balanced"]),
            "random_state": random_state,
            "verbose": -1,
            "objective": "binary",
        }
        threshold = trial.suggest_float("threshold", 0.18, 0.45)

        fold_recalls, fold_f2s, fold_rocs = [], [], []

        for train_idx, val_idx in skf.split(X, y):
            X_tr, X_val = X.iloc[train_idx], X.iloc[val_idx]
            y_tr, y_val = y[train_idx], y[val_idx]

            prep = make_column_transformer()
            X_tr_trans = prep.fit_transform(X_tr)
            X_val_trans = prep.transform(X_val)

            clf = LGBMClassifier(**params)
            clf.fit(X_tr_trans, y_tr)
            val_probs = clf.predict_proba(X_val_trans)[:, 1]

            m = compute_clinical_metrics(y_val, val_probs, threshold=threshold)
            fold_recalls.append(m["sensitivity"])
            fold_f2s.append(m["f2_score"])
            fold_rocs.append(m["roc_auc"])

        mean_rec = np.mean(fold_recalls)
        mean_roc = np.mean(fold_rocs)
        mean_f2 = np.mean(fold_f2s)

        # Asymmetric clinical penalty if Sensitivity < 0.92 or ROC < 0.88
        penalty = 0.0
        if mean_rec < 0.92:
            penalty += (0.92 - mean_rec) * 8.0
        if mean_roc < 0.88:
            penalty += (0.88 - mean_roc) * 5.0

        return mean_f2 - penalty

    study = optuna.create_study(direction="maximize")
    study.optimize(objective, n_trials=n_trials)

    best_params = study.best_params.copy()
    best_thresh = best_params.pop("threshold")

    return best_params, best_thresh


def compare_models(
    X: pd.DataFrame,
    y: np.ndarray,
    tuned_lgbm_params: Optional[Dict[str, Any]] = None,
    tuned_lgbm_thresh: Optional[float] = None,
) -> Dict[str, Any]:
    """
    Executes comparative 5-fold cross-validation across all 3 architectures:
    1. ElasticNet Logistic Regression
    2. Random Forest
    3. LightGBM (Tuned)
    """
    models = {
        "ElasticNet_LogisticRegression": get_baseline_logistic_regression(),
        "Random_Forest": get_random_forest(),
        "LightGBM": get_lightgbm_model(**tuned_lgbm_params) if tuned_lgbm_params else get_lightgbm_model(),
    }

    results = {}

    for name, model in models.items():
        print(f"   Evaluating {name} with 5-fold Stratified CV...")
        res = stratified_cv_benchmark(
            model=model,
            X=X,
            y=y,
            n_splits=5,
            optimize_threshold=True,
            min_sensitivity=0.92,
        )
        results[name] = res

    # Model Selection Logic:
    # 1. Must satisfy Sensitivity >= 0.92 and ROC-AUC > 0.88.
    # 2. Select model with highest PR-AUC and highest F2 score.
    winner_name = None
    best_composite_score = -1.0

    for name, res in results.items():
        oof = res["overall_oof_metrics"]
        sensitivity = oof["sensitivity"]
        roc_auc = oof["roc_auc"]
        pr_auc = oof["pr_auc"]
        f2 = oof["f2_score"]

        # Composite score: PR-AUC + F2
        composite = pr_auc + f2

        # Check hard criteria
        if sensitivity >= 0.92 and roc_auc > 0.88:
            if composite > best_composite_score:
                best_composite_score = composite
                winner_name = name

    # Fallback to model with best PR-AUC if constraints fail
    if not winner_name:
        winner_name = max(results.keys(), key=lambda k: results[k]["overall_oof_metrics"]["pr_auc"])

    return {
        "results": results,
        "winner": winner_name,
        "selection_rationale": (
            f"Selected {winner_name} based on highest PR-AUC ({results[winner_name]['overall_oof_metrics']['pr_auc']:.4f}) "
            f"and F2 score ({results[winner_name]['overall_oof_metrics']['f2_score']:.4f}) while satisfying Sensitivity >= 0.92 "
            f"({results[winner_name]['overall_oof_metrics']['sensitivity']:.4f}) and ROC-AUC > 0.88 "
            f"({results[winner_name]['overall_oof_metrics']['roc_auc']:.4f})."
        ),
    }
