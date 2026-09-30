namespace HeartCDSS.Domain.Enums;

/// <summary>
/// Biological sex classification aligning with UCI Heart Disease dataset and FHIR Patient administrative sex.
/// </summary>
public enum BiologicalSex
{
    Female = 0,
    Male = 1
}

/// <summary>
/// Current clinical care status of the patient within the healthcare system.
/// </summary>
public enum PatientStatus
{
    Active = 1,
    Discharged = 2,
    Deceased = 3,
    Archived = 4
}

/// <summary>
/// Clinical setting where the patient encounter takes place.
/// </summary>
public enum EncounterType
{
    OutpatientScreening = 1,
    EmergencyTriage = 2,
    InpatientConsult = 3,
    CardiologyFollowUp = 4
}

/// <summary>
/// Transactional lifecycle status of a clinical encounter.
/// </summary>
public enum EncounterStatus
{
    InProgress = 1,
    AssessmentComplete = 2,
    Signed = 3,
    Cancelled = 4
}

/// <summary>
/// Anginal chest pain symptomatology classification (cp 1-4).
/// </summary>
public enum ChestPainType
{
    TypicalAngina = 1,
    AtypicalAngina = 2,
    NonAnginal = 3,
    Asymptomatic = 4
}

/// <summary>
/// Resting electrocardiographic findings (restecg 0-2).
/// </summary>
public enum RestingEcgType
{
    Normal = 0,
    StTAbnormality = 1,
    LvHypertrophy = 2
}

/// <summary>
/// Slope of the peak exercise ST segment (slope 1-3).
/// </summary>
public enum StSlope
{
    Upsloping = 1,
    Flat = 2,
    Downsloping = 3
}

/// <summary>
/// Myocardial perfusion scintigraphy thalassemia defect status (thal 3, 6, 7).
/// </summary>
public enum ThalassemiaType
{
    Normal = 3,
    FixedDefect = 6,
    ReversibleDefect = 7
}

/// <summary>
/// Stratified cardiovascular risk band tier based on calibrated posterior probability.
/// </summary>
public enum RiskBand
{
    Low = 1,        // P < 0.20
    Moderate = 2,   // 0.20 <= P < 0.50
    Elevated = 3,   // 0.50 <= P < 0.75
    High = 4        // P >= 0.75
}

/// <summary>
/// Directional clinical impact of a SHAP attribution factor on cardiovascular risk.
/// </summary>
public enum ImpactDirection
{
    RiskIncreasing = 1,
    Protective = 2,
    Neutral = 3
}

/// <summary>
/// Result status of an external ML inference execution attempt.
/// </summary>
public enum InferenceExecutionStatus
{
    Success = 1,
    FailedValidation = 2,
    CircuitBreakerOpen = 3,
    Timeout = 4,
    InternalError = 5
}
