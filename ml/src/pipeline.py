"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.src.pipeline
Description: Scikit-Learn feature transformation and preprocessing pipeline.
             Applies RobustScaler to continuous clinical metrics, OneHotEncoder to
             nominal categories, and passes through binary/ordinal features while
             preserving feature names for SHAP explainability.
"""

from typing import Dict, List, Optional, Tuple, Union
import os
import joblib
import numpy as np
import pandas as pd
from sklearn.base import BaseEstimator, TransformerMixin
from sklearn.compose import ColumnTransformer
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import OneHotEncoder, RobustScaler

# ---------------------------------------------------------------------------
# Feature Group Declarations (Standard UCI 14 Attributes)
# ---------------------------------------------------------------------------
CONTINUOUS_FEATURES: List[str] = [
    "age",
    "trestbps",
    "chol",
    "thalach",
    "oldpeak",
]

CATEGORICAL_FEATURES: List[str] = [
    "cp",
    "restecg",
    "slope",
    "thal",
]

BINARY_FEATURES: List[str] = [
    "sex",
    "fbs",
    "exang",
]

ORDINAL_FEATURES: List[str] = [
    "ca",
]

ALL_PREDICTOR_COLUMNS: List[str] = (
    CONTINUOUS_FEATURES + CATEGORICAL_FEATURES + BINARY_FEATURES + ORDINAL_FEATURES
)
CLINICAL_FEATURE_ORDER: List[str] = ALL_PREDICTOR_COLUMNS
NUM_EXPECTED_FEATURES: int = 22

TARGET_COLUMN: str = "target"


# ---------------------------------------------------------------------------
# Human-Readable Clinical Label Mapping (for SHAP and UI integration)
# ---------------------------------------------------------------------------
CLINICAL_LABEL_MAP: Dict[str, str] = {
    # Continuous
    "age": "Age (years)",
    "trestbps": "Resting Blood Pressure (mmHg)",
    "chol": "Serum Cholesterol (mg/dL)",
    "thalach": "Max Heart Rate Achieved (bpm)",
    "oldpeak": "ST Depression (mm)",
    # Chest Pain Type (cp)
    "cp_1": "Chest Pain: Typical Angina",
    "cp_2": "Chest Pain: Atypical Angina",
    "cp_3": "Chest Pain: Non-Anginal",
    "cp_4": "Chest Pain: Asymptomatic",
    # Resting ECG (restecg)
    "restecg_0": "Resting ECG: Normal",
    "restecg_1": "Resting ECG: ST-T Wave Abnormality",
    "restecg_2": "Resting ECG: Left Ventricular Hypertrophy",
    # ST Slope (slope)
    "slope_1": "ST Slope: Upsloping",
    "slope_2": "ST Slope: Flat",
    "slope_3": "ST Slope: Downsloping",
    # Thalassemia Defect (thal)
    "thal_3": "Thalassemia: Normal",
    "thal_6": "Thalassemia: Fixed Defect",
    "thal_7": "Thalassemia: Reversible Defect",
    # Binary & Count Passthrough
    "sex": "Biological Sex (Male=1, Female=0)",
    "fbs": "Fasting Blood Sugar > 120 mg/dL",
    "exang": "Exercise-Induced Angina (Yes=1)",
    "ca": "Major Fluoroscopy Vessels Colored (0-3)",
}


def make_column_transformer(
    drop_first_cat: bool = False,
    sparse_output: bool = False,
) -> ColumnTransformer:
    """
    Constructs an un-fitted Scikit-Learn ColumnTransformer.
    - Continuous features -> RobustScaler (robust to clinical outliers)
    - Nominal features    -> OneHotEncoder (handle_unknown='ignore')
    - Binary & Ordinal    -> Passthrough (retains 0/1 and vessel counts)
    - verbose_feature_names_out=False preserves clean names like cp_4, oldpeak.
    """
    cont_transformer = RobustScaler(
        with_centering=True,
        with_scaling=True,
        quantile_range=(25.0, 75.0),
    )

    cat_transformer = OneHotEncoder(
        drop="first" if drop_first_cat else None,
        sparse_output=sparse_output,
        handle_unknown="ignore" if not drop_first_cat else "error",
    )

    preprocessor = ColumnTransformer(
        transformers=[
            ("cont", cont_transformer, CONTINUOUS_FEATURES),
            ("cat", cat_transformer, CATEGORICAL_FEATURES),
            ("passthrough", "passthrough", BINARY_FEATURES + ORDINAL_FEATURES),
        ],
        remainder="drop",
        verbose_feature_names_out=False,
    )

    return preprocessor




class PULSEPreprocessor(BaseEstimator, TransformerMixin):
    """
    Encapsulated clinical preprocessor that provides:
    1. Validation of input columns.
    2. Strict fit/transform separation between train and test splits.
    3. Transformation to both NumPy array and DataFrame with named columns.
    4. Mapping to human-readable clinical labels for downstream SHAP explainability.
    5. Serialization and deserialization via joblib.
    """

    def __init__(self, drop_first_cat: bool = False):
        self.drop_first_cat = drop_first_cat
        self.transformer: ColumnTransformer = make_column_transformer(
            drop_first_cat=drop_first_cat
        )
        self.feature_names_out_: Optional[List[str]] = None
        self.is_fitted_: bool = False

    def _validate_columns(self, X: pd.DataFrame) -> pd.DataFrame:
        """Verifies that all required predictor columns are present."""
        if not isinstance(X, pd.DataFrame):
            X = pd.DataFrame(X, columns=ALL_PREDICTOR_COLUMNS)

        missing = [col for col in ALL_PREDICTOR_COLUMNS if col not in X.columns]
        if missing:
            raise ValueError(f"Input data missing required clinical predictors: {missing}")

        return X[ALL_PREDICTOR_COLUMNS].copy()

    def fit(self, X: Union[pd.DataFrame, np.ndarray], y: Optional[Union[pd.Series, np.ndarray]] = None):
        """Fits the RobustScaler and OneHotEncoder on training data only."""
        X_df = self._validate_columns(X if isinstance(X, pd.DataFrame) else pd.DataFrame(X, columns=ALL_PREDICTOR_COLUMNS))
        self.transformer.fit(X_df)
        self.feature_names_out_ = list(self.transformer.get_feature_names_out())
        self.is_fitted_ = True
        return self

    def transform(self, X: Union[pd.DataFrame, np.ndarray]) -> np.ndarray:
        """Transforms data using the parameters learned during fit."""
        if not self.is_fitted_:
            raise RuntimeError("PULSEPreprocessor must be fitted before calling transform.")

        X_df = self._validate_columns(X if isinstance(X, pd.DataFrame) else pd.DataFrame(X, columns=ALL_PREDICTOR_COLUMNS))
        return self.transformer.transform(X_df)

    def transform_to_dataframe(self, X: Union[pd.DataFrame, np.ndarray]) -> pd.DataFrame:
        """Transforms data and returns a pandas DataFrame with preserved feature names."""
        transformed_arr = self.transform(X)
        feature_names = self.get_feature_names()
        index = X.index if isinstance(X, pd.DataFrame) else None
        return pd.DataFrame(transformed_arr, columns=feature_names, index=index)

    def fit_transform(self, X: Union[pd.DataFrame, np.ndarray], y: Optional[Union[pd.Series, np.ndarray]] = None) -> np.ndarray:
        """Fits on training data and transforms it in a single step."""
        return self.fit(X, y).transform(X)

    def get_feature_names(self) -> List[str]:
        """Returns the list of transformed feature names."""
        if not self.is_fitted_ or self.feature_names_out_ is None:
            raise RuntimeError("Preprocessor is not fitted yet.")
        return list(self.feature_names_out_)

    def get_clinical_labels(self) -> List[str]:
        """Returns human-readable clinical labels matching transformed features."""
        feature_names = self.get_feature_names()
        return [CLINICAL_LABEL_MAP.get(feat, feat) for feat in feature_names]

    def save(self, filepath: str) -> str:
        """Serializes the fitted preprocessor to disk using joblib."""
        if not self.is_fitted_:
            raise RuntimeError("Cannot save an un-fitted preprocessor.")
        os.makedirs(os.path.dirname(os.path.abspath(filepath)), exist_ok=True)
        joblib.dump(self, filepath)
        return filepath

    @classmethod
    def load(cls, filepath: str) -> "PULSEPreprocessor":
        """Loads a serialized preprocessor from disk."""
        if not os.path.exists(filepath):
            raise FileNotFoundError(f"Preprocessor artifact not found at: {filepath}")
        obj = joblib.load(filepath)
        if not isinstance(obj, cls):
            raise TypeError(f"Loaded object is not a {cls.__name__}")
        return obj


def split_train_test_clinical(
    df: pd.DataFrame,
    test_size: float = 0.20,
    random_state: int = 42,
    stratify: bool = True,
) -> Tuple[pd.DataFrame, pd.DataFrame, pd.Series, pd.Series]:
    """
    Splits clinical dataset strictly into training and test sets.
    Stratifies by the binary target to maintain disease prevalence across splits.
    """
    if TARGET_COLUMN not in df.columns:
        raise KeyError(f"Target column '{TARGET_COLUMN}' not found in dataframe.")

    X = df[ALL_PREDICTOR_COLUMNS].copy()
    y = df[TARGET_COLUMN].copy()

    stratify_target = y if stratify else None

    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y,
        test_size=test_size,
        random_state=random_state,
        stratify=stratify_target,
    )

    return X_train, X_test, y_train, y_test
