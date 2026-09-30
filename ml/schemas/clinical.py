"""
PULSE Clinical Decision Support System (CDSS)
Module: ml.schemas.clinical
Description: Strict Pydantic v2 schemas and physiological boundary validators
             for patient clinical indicators and prediction payloads.
"""

from typing import Any, Dict, List, Optional
import pandas as pd
from pydantic import BaseModel, Field, field_validator, model_validator



class PatientClinicalIndicators(BaseModel):
    """
    Patient physiological indicators and laboratory markers corresponding to the
    14 attributes of the UCI Heart Disease dataset.
    Enforces strict physiological plausibility boundaries matching ASP.NET Core FluentValidation.
    """

    age: int = Field(
        ...,
        description="Patient age in years at examination (Plausible: 1 - 110, Normal: 18 - 85)",
        examples=[58],
    )
    sex: int = Field(
        ...,
        description="Biological sex at birth: 0 = Female, 1 = Male",
        examples=[0],
    )
    cp: int = Field(
        ...,
        description="Chest pain type: 1 = Typical Angina, 2 = Atypical Angina, 3 = Non-Anginal, 4 = Asymptomatic",
        examples=[4],
    )
    trestbps: float = Field(
        ...,
        description="Resting blood pressure in mmHg upon clinical admission (Plausible: 60 - 260, Normal: 90 - 120)",
        examples=[145.0],
    )
    chol: float = Field(
        ...,
        description="Serum cholesterol in mg/dL (Plausible: 80 - 600, Normal: 125 - 200)",
        examples=[268.0],
    )
    fbs: int = Field(
        ...,
        description="Fasting blood sugar > 120 mg/dL: 1 = True (elevated), 0 = False (normal)",
        examples=[1],
    )
    restecg: int = Field(
        ...,
        description="Resting electrocardiogram: 0 = Normal, 1 = ST-T wave abnormality, 2 = Left ventricular hypertrophy",
        examples=[1],
    )
    thalach: float = Field(
        ...,
        description="Maximum heart rate achieved during exercise in bpm (Plausible: 40 - 220, Normal: 60 - 180)",
        examples=[115.0],
    )
    exang: int = Field(
        ...,
        description="Exercise-induced angina: 1 = Yes (present), 0 = No (absent)",
        examples=[1],
    )
    oldpeak: float = Field(
        ...,
        description="ST depression induced by exercise relative to rest in mm (Plausible: 0.0 - 6.5, Normal: 0.0 - 1.5)",
        examples=[2.8],
    )
    slope: int = Field(
        ...,
        description="Slope of peak exercise ST segment: 1 = Upsloping, 2 = Flat, 3 = Downsloping",
        examples=[2],
    )
    ca: int = Field(
        ...,
        description="Number of major vessels (0 - 3) colored by fluoroscopy",
        examples=[2],
    )
    thal: int = Field(
        ...,
        description="Thallium scintigraphy defect code: 3 = Normal, 6 = Fixed defect, 7 = Reversible defect (or 1, 2, 3)",
        examples=[7],
    )

    # -----------------------------------------------------------------------
    # Custom Physiological Field Validators (Matching FluentValidation)
    # -----------------------------------------------------------------------
    @field_validator("age")
    @classmethod
    def validate_age(cls, v: int) -> int:
        if v < 1 or v > 110:
            raise ValueError(f"Patient age ({v} yrs) violates physiological plausibility bounds (1 - 110 yrs).")
        return v

    @field_validator("sex")
    @classmethod
    def validate_sex(cls, v: int) -> int:
        if v not in (0, 1):
            raise ValueError(f"Biological sex code ({v}) is invalid. Must be 0 (Female) or 1 (Male).")
        return v

    @field_validator("cp")
    @classmethod
    def validate_cp(cls, v: int) -> int:
        if v not in (1, 2, 3, 4):
            raise ValueError(
                f"Chest pain type ({v}) must be 1 (Typical Angina), 2 (Atypical Angina), "
                f"3 (Non-Anginal), or 4 (Asymptomatic)."
            )
        return v

    @field_validator("trestbps")
    @classmethod
    def validate_resting_bp(cls, v: float) -> float:
        if v < 60.0 or v > 260.0:
            raise ValueError(
                f"Resting Blood Pressure ({v} mmHg) violates physiological limits (60 - 260 mmHg)."
            )
        return float(round(v, 1))

    @field_validator("chol")
    @classmethod
    def validate_cholesterol(cls, v: float) -> float:
        if v < 80.0 or v > 600.0:
            raise ValueError(
                f"Serum Cholesterol ({v} mg/dL) must be within plausible limits (80 - 600 mg/dL)."
            )
        return float(round(v, 1))

    @field_validator("fbs")
    @classmethod
    def validate_fbs(cls, v: int) -> int:
        if v not in (0, 1):
            raise ValueError(f"Fasting blood sugar flag ({v}) must be 0 (< 120 mg/dL) or 1 (>= 120 mg/dL).")
        return v

    @field_validator("restecg")
    @classmethod
    def validate_restecg(cls, v: int) -> int:
        if v not in (0, 1, 2):
            raise ValueError(
                f"Resting ECG code ({v}) must be 0 (Normal), 1 (ST-T abnormality), or 2 (LV hypertrophy)."
            )
        return v

    @field_validator("thalach")
    @classmethod
    def validate_max_heart_rate(cls, v: float) -> float:
        if v < 40.0 or v > 220.0:
            raise ValueError(
                f"Maximum heart rate achieved ({v} bpm) must be between 40 and 220 bpm."
            )
        return float(round(v, 1))

    @field_validator("exang")
    @classmethod
    def validate_exang(cls, v: int) -> int:
        if v not in (0, 1):
            raise ValueError(f"Exercise-induced angina flag ({v}) must be 0 (Absent) or 1 (Present).")
        return v

    @field_validator("oldpeak")
    @classmethod
    def validate_st_depression(cls, v: float) -> float:
        if v < 0.0 or v > 6.5:
            raise ValueError(
                f"ST depression (oldpeak: {v} mm) must range between 0.0 and 6.5 mm."
            )
        return float(round(v, 2))

    @field_validator("slope")
    @classmethod
    def validate_slope(cls, v: int) -> int:
        if v not in (1, 2, 3):
            raise ValueError(f"Peak exercise ST slope ({v}) must be 1 (Upsloping), 2 (Flat), or 3 (Downsloping).")
        return v

    @field_validator("ca")
    @classmethod
    def validate_fluoroscopy_vessels(cls, v: int) -> int:
        if v < 0 or v > 3:
            raise ValueError(
                f"Number of major vessels ({v}) colored by fluoroscopy must be 0, 1, 2, or 3."
            )
        return v

    @field_validator("thal")
    @classmethod
    def validate_thalassemia(cls, v: int) -> int:
        # Standard UCI coding: 3=normal, 6=fixed, 7=reversible
        # Alternate 1-based coding: 1=normal, 2=fixed, 3=reversible
        if v in (3, 6, 7):
            return v
        elif v == 1:
            return 3  # Normal (1-based alternate coding)
        elif v == 2:
            return 6  # Fixed defect (1-based alternate coding)
        else:
            raise ValueError(
                f"Thalassemia code ({v}) is invalid. Must be 3 (Normal), 6 (Fixed Defect), or 7 (Reversible Defect)."
            )



    def get_clinical_warning_flags(self) -> List[str]:
        """Returns non-blocking clinical warnings when indicators approach critical thresholds."""
        warnings = []
        if self.trestbps > 200.0:
            warnings.append(f"Critical Hypertension: Resting BP ({self.trestbps} mmHg) > 200 mmHg.")
        elif self.trestbps < 70.0:
            warnings.append(f"Severe Hypotension: Resting BP ({self.trestbps} mmHg) < 70 mmHg.")

        if self.chol > 400.0:
            warnings.append(f"Severe Hypercholesterolemia: Serum Cholesterol ({self.chol} mg/dL) > 400 mg/dL.")

        if self.thalach < 50.0:
            warnings.append(f"Critical Bradycardia: Max HR achieved ({self.thalach} bpm) < 50 bpm.")
        elif self.thalach > 205.0:
            warnings.append(f"Extreme Tachycardia: Max HR achieved ({self.thalach} bpm) > 205 bpm.")

        if self.oldpeak > 3.5:
            warnings.append(f"Severe ST Depression: oldpeak ({self.oldpeak} mm) > 3.5 mm indicates substantial ischemia.")

        if self.ca >= 2:
            warnings.append(f"Multi-Vessel Calcification: {self.ca} major coronary vessels fluoroscopy-occluded.")

        if self.fbs == 1:
            warnings.append("Diabetic Risk Factor: Fasting Blood Sugar >= 120 mg/dL.")

        if self.age < 18 or self.age > 95:
            warnings.append(f"Age Alert: Patient age ({self.age} yrs) operates outside typical clinical cohort range.")

        return warnings

    def to_dataframe(self) -> pd.DataFrame:
        """Converts validated indicators into a 1-row DataFrame matching the model feature order."""
        return pd.DataFrame([{
            "age": self.age,
            "sex": self.sex,
            "cp": self.cp,
            "trestbps": self.trestbps,
            "chol": self.chol,
            "fbs": self.fbs,
            "restecg": self.restecg,
            "thalach": self.thalach,
            "exang": self.exang,
            "oldpeak": self.oldpeak,
            "slope": self.slope,
            "ca": self.ca,
            "thal": self.thal,
        }])


class PredictionRequest(BaseModel):
    """Encapsulates prediction intake payload with optional clinical metadata."""
    encounter_id: Optional[str] = Field(
        None,
        description="Unique encounter identifier from the ASP.NET Core gateway",
        examples=["8d39c094-0a37-4f6c-8a03-7cb529bfa5e1"],
    )
    correlation_id: Optional[str] = Field(
        None,
        description="Distributed tracing correlation ID passed across services",
        examples=["trace-49a8123-bc94-432a"],
    )
    patient_mrn: Optional[str] = Field(
        None,
        description="Patient Medical Record Number (de-identified / anonymized)",
        examples=["MRN-847291"],
    )
    clinical_indicators: PatientClinicalIndicators

    @model_validator(mode="before")
    @classmethod
    def assemble_indicators_if_flat(cls, data: Any) -> Any:
        """
        Allows intake of either nested {'clinical_indicators': {...}} or flat
        key-value pairs at the root level, normalizing into structured indicators.
        """
        if isinstance(data, dict):
            if "clinical_indicators" not in data:
                clinical_keys = {
                    "age", "sex", "cp", "trestbps", "chol", "fbs",
                    "restecg", "thalach", "exang", "oldpeak", "slope", "ca", "thal"
                }
                if any(k in data for k in clinical_keys):
                    data_copy = dict(data)
                    indicators = {}
                    for k in clinical_keys:
                        if k in data_copy:
                            indicators[k] = data_copy.pop(k)
                    data_copy["clinical_indicators"] = indicators
                    return data_copy
        return data



class ConfidenceInterval(BaseModel):
    """95% Confidence Interval bounds around calibrated risk probability."""
    lower: float = Field(..., description="Lower 95% CI bound", examples=[0.6210])
    upper: float = Field(..., description="Upper 95% CI bound", examples=[0.7400])
    confidence_level: float = Field(default=0.95, description="Confidence level (default 0.95)")
    margin_percentage: float = Field(..., description="Margin of error (+/- %)", examples=[5.9])


class FeatureAttribution(BaseModel):
    """Local SHAP feature attribution element."""
    feature_key: str = Field(..., description="Technical feature identifier", examples=["oldpeak"])
    feature_label: str = Field(..., description="Human clinical display label", examples=["ST Depression (mm)"])
    patient_value: str = Field(..., description="Observed patient measurement", examples=["2.8 mm"])
    shap_value: float = Field(..., description="Signed Shapley attribution value", examples=[0.1420])
    impact_direction: str = Field(..., description="'RISK_INCREASING' or 'PROTECTIVE'", examples=["RISK_INCREASING"])
    rank: int = Field(..., description="Impact ranking order (1 = highest)", examples=[1])


class PredictionResponse(BaseModel):
    """Complete CDSS inference response payload."""
    encounter_id: Optional[str] = Field(None, description="Bound encounter reference")
    correlation_id: Optional[str] = Field(None, description="Distributed trace correlation ID")
    risk_probability: float = Field(..., description="Calibrated posterior probability P(Y=1 | X)", examples=[0.6840])
    risk_band: str = Field(..., description="Clinical risk tier: Low, Moderate, Elevated, High", examples=["Elevated"])
    is_cad_positive: bool = Field(..., description="Whether risk exceeds operational decision threshold", examples=[True])
    decision_threshold: float = Field(..., description="Operational classification threshold", examples=[0.2900])
    confidence_interval: ConfidenceInterval
    top_risk_drivers: List[FeatureAttribution] = Field(..., description="Top 5 risk escalating factors")
    top_protective_factors: List[FeatureAttribution] = Field(..., description="Top 3 protective factors")
    all_attributions: List[FeatureAttribution] = Field(..., description="Full 22-feature attribution breakdown")
    explanations: List[FeatureAttribution] = Field(
        default_factory=list,
        description="Comprehensive array of feature attributions containing feature key, readable label, raw patient value, signed SHAP contribution, and directional impact flag (RISK_INCREASING or PROTECTIVE)",
    )

    clinical_warnings: List[str] = Field(default_factory=list, description="Physiological threshold alerts")
    recommended_action: str = Field(..., description="Guideline-directed clinical intervention recommendation")
    clinical_disclaimer: str = Field(
        default=(
            "Clinical Decision Support Notice: This risk score is an adjunct screening aid and "
            "does not constitute an automated medical diagnosis. Professional medical evaluation "
            "and diagnostic testing supersede model predictions."
        ),
        description="Persistent mandatory legal/clinical disclaimer",
    )
    inference_latency_ms: float = Field(..., description="Round-trip inference execution time in ms", examples=[12.4])


class HealthCheckResponse(BaseModel):
    """Microservice health check and readiness status schema."""
    status: str = Field(default="healthy", examples=["healthy"])
    model_loaded: bool = Field(..., examples=[True])
    model_version: str = Field(default="v1.0.0", examples=["v1.0.0"])
    calibrator_method: str = Field(default="IsotonicRegression", examples=["IsotonicRegression"])
    decision_threshold: float = Field(default=0.2900, examples=[0.2900])
    uptime_seconds: float = Field(..., examples=[124.5])
    memory_usage_mb: float = Field(..., description="Resident Set Size (RSS) memory consumption in MB", examples=[115.4])
    timestamp: str = Field(..., description="ISO 8601 UTC timestamp of health evaluation", examples=["2026-09-30T11:00:00Z"])


class ValidationErrorDetail(BaseModel):
    """Specific field-level validation violation."""
    loc: List[str] = Field(..., description="Path to invalid field", examples=[["body", "trestbps"]])
    msg: str = Field(..., description="Actionable clinical validation error message", examples=["Resting Blood Pressure (50.0 mmHg) violates physiological limits (60 - 260 mmHg)."])
    type: str = Field(..., description="Error classification type", examples=["value_error"])
    input: Optional[Any] = Field(None, description="Rejected input value", examples=[50.0])


class HTTPValidationErrorResponse(BaseModel):
    """RFC 7807-compatible structured problem details for validation failures."""
    title: str = Field(default="Validation Error", description="Human-readable summary of problem")
    status: int = Field(default=422, description="HTTP status code")
    detail: str = Field(..., description="High-level description of why validation failed")
    errors: List[ValidationErrorDetail] = Field(default_factory=list, description="List of specific field violations")

