"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.services.transformer
Description: Internal transformation utility mapping validated patient intake DTOs
             into the exact tabular and numerical matrix representations expected
             by Scikit-Learn ColumnTransformer and LightGBM.
"""

from typing import Any, Dict, List, Sequence, Union
import numpy as np
import pandas as pd

from ml.schemas.clinical import PatientClinicalIndicators
from ml.src.pipeline import CLINICAL_FEATURE_ORDER, NUM_EXPECTED_FEATURES, PULSEPreprocessor


class ClinicalDataTransformer:
    """
    Transforms clinical Pydantic schemas into model-ready numerical feature matrices.
    Guarantees:
    1. Deterministic column ordering matching training schema:
       ['age', 'sex', 'cp', 'trestbps', 'chol', 'fbs', 'restecg', 'thalach', 'exang', 'oldpeak', 'slope', 'ca', 'thal']
    2. Strict type conversions (continuous -> float64, discrete -> int64).
    3. Transformation via PULSEPreprocessor into 22-dimensional feature vectors.
    4. Validation against nulls, NaNs, and infinite values.
    """

    FEATURE_NAMES: List[str] = CLINICAL_FEATURE_ORDER
    NUM_INPUT_FEATURES: int = len(CLINICAL_FEATURE_ORDER)  # 13 features
    NUM_OUTPUT_FEATURES: int = NUM_EXPECTED_FEATURES       # 22 features

    @classmethod
    def to_dataframe(cls, indicators: PatientClinicalIndicators) -> pd.DataFrame:
        """
        Converts a single PatientClinicalIndicators model into a 1-row Pandas DataFrame
        with strictly ordered columns and appropriate types.
        """
        row_dict = {
            "age": int(indicators.age),
            "sex": int(indicators.sex),
            "cp": int(indicators.cp),
            "trestbps": float(indicators.trestbps),
            "chol": float(indicators.chol),
            "fbs": int(indicators.fbs),
            "restecg": int(indicators.restecg),
            "thalach": float(indicators.thalach),
            "exang": int(indicators.exang),
            "oldpeak": float(indicators.oldpeak),
            "slope": int(indicators.slope),
            "ca": int(indicators.ca),
            "thal": int(indicators.thal),
        }
        df = pd.DataFrame([row_dict], columns=cls.FEATURE_NAMES)
        return df

    @classmethod
    def to_batch_dataframe(
        cls, indicators_list: Sequence[PatientClinicalIndicators]
    ) -> pd.DataFrame:
        """
        Converts a list of PatientClinicalIndicators into a multi-row Pandas DataFrame.
        """
        if not indicators_list:
            raise ValueError("Input indicators list cannot be empty.")

        rows = []
        for ind in indicators_list:
            rows.append({
                "age": int(ind.age),
                "sex": int(ind.sex),
                "cp": int(ind.cp),
                "trestbps": float(ind.trestbps),
                "chol": float(ind.chol),
                "fbs": int(ind.fbs),
                "restecg": int(ind.restecg),
                "thalach": float(ind.thalach),
                "exang": int(ind.exang),
                "oldpeak": float(ind.oldpeak),
                "slope": int(ind.slope),
                "ca": int(ind.ca),
                "thal": int(ind.thal),
            })
        return pd.DataFrame(rows, columns=cls.FEATURE_NAMES)

    @classmethod
    def transform_patient(
        cls,
        indicators: PatientClinicalIndicators,
        preprocessor: PULSEPreprocessor,
    ) -> np.ndarray:
        """
        Transforms a single patient's clinical indicators into a 22-dimensional
        scaled, one-hot encoded NumPy matrix of shape (1, 22).
        """
        df = cls.to_dataframe(indicators)
        x_trans = preprocessor.transform(df)

        if not isinstance(x_trans, np.ndarray):
            x_trans = np.asarray(x_trans)

        if x_trans.shape != (1, cls.NUM_OUTPUT_FEATURES):
            raise ValueError(
                f"Transformed feature matrix shape mismatch: expected (1, {cls.NUM_OUTPUT_FEATURES}), "
                f"got {x_trans.shape}"
            )

        if np.isnan(x_trans).any() or np.isinf(x_trans).any():
            raise ValueError("Transformed patient feature matrix contains NaN or Inf values.")

        return x_trans

    @classmethod
    def transform_batch(
        cls,
        indicators_list: Sequence[PatientClinicalIndicators],
        preprocessor: PULSEPreprocessor,
    ) -> np.ndarray:
        """
        Transforms a batch of patient clinical indicators into a (N, 22) NumPy matrix.
        """
        df = cls.to_batch_dataframe(indicators_list)
        x_trans = preprocessor.transform(df)

        if not isinstance(x_trans, np.ndarray):
            x_trans = np.asarray(x_trans)

        expected_rows = len(indicators_list)
        if x_trans.shape != (expected_rows, cls.NUM_OUTPUT_FEATURES):
            raise ValueError(
                f"Batch transformed feature matrix shape mismatch: expected ({expected_rows}, {cls.NUM_OUTPUT_FEATURES}), "
                f"got {x_trans.shape}"
            )

        if np.isnan(x_trans).any() or np.isinf(x_trans).any():
            raise ValueError("Batch transformed feature matrix contains NaN or Inf values.")

        return x_trans
