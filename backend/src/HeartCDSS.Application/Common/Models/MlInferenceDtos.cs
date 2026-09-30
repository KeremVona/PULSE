using System.Text.Json.Serialization;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.ValueObjects;

namespace HeartCDSS.Application.Common.Models;

/// <summary>
/// Physiological indicators and laboratory markers payload corresponding to the UCI Heart Disease schema
/// required by the FastAPI ML inference service.
/// </summary>
public sealed record MlIndicatorsPayloadDto
{
    [JsonPropertyName("age")]
    public required int Age { get; init; }

    [JsonPropertyName("sex")]
    public required int Sex { get; init; }

    [JsonPropertyName("cp")]
    public required int Cp { get; init; }

    [JsonPropertyName("trestbps")]
    public required double Trestbps { get; init; }

    [JsonPropertyName("chol")]
    public required double Chol { get; init; }

    [JsonPropertyName("fbs")]
    public required int Fbs { get; init; }

    [JsonPropertyName("restecg")]
    public required int Restecg { get; init; }

    [JsonPropertyName("thalach")]
    public required double Thalach { get; init; }

    [JsonPropertyName("exang")]
    public required int Exang { get; init; }

    [JsonPropertyName("oldpeak")]
    public required double Oldpeak { get; init; }

    [JsonPropertyName("slope")]
    public required int Slope { get; init; }

    [JsonPropertyName("ca")]
    public required int Ca { get; init; }

    [JsonPropertyName("thal")]
    public required int Thal { get; init; }

    /// <summary>
    /// Makes a raw indicators payload DTO.
    /// </summary>
    public static MlIndicatorsPayloadDto Make(
        int age,
        int sex,
        int cp,
        double trestbps,
        double chol,
        int fbs,
        int restecg,
        double thalach,
        int exang,
        double oldpeak,
        int slope,
        int ca,
        int thal) =>
        new()
        {
            Age = age,
            Sex = sex,
            Cp = cp,
            Trestbps = trestbps,
            Chol = chol,
            Fbs = fbs,
            Restecg = restecg,
            Thalach = thalach,
            Exang = exang,
            Oldpeak = oldpeak,
            Slope = slope,
            Ca = ca,
            Thal = thal
        };

    /// <summary>
    /// Makes an indicators payload from the Application layer command input DTO.
    /// </summary>
    public static MlIndicatorsPayloadDto MakeFromInputDto(ClinicalIndicatorsInputDto input) =>
        new()
        {
            Age = input.Age,
            Sex = input.Sex,
            Cp = input.ChestPainType,
            Trestbps = input.RestingBp,
            Chol = input.SerumCholesterol,
            Fbs = input.FastingBloodSugar,
            Restecg = input.RestingEcg,
            Thalach = input.MaxHeartRate,
            Exang = input.ExerciseAngina,
            Oldpeak = (double)input.OldpeakStDepression,
            Slope = input.Slope,
            Ca = input.FluoroscopyVessels,
            Thal = input.Thalassemia
        };

    /// <summary>
    /// Makes an indicators payload from the Domain ClinicalIndicators entity.
    /// </summary>
    public static MlIndicatorsPayloadDto MakeFromDomain(ClinicalIndicators indicators) =>
        new()
        {
            Age = indicators.AgeAtEncounter,
            Sex = indicators.Sex,
            Cp = (int)indicators.ChestPainType,
            Trestbps = indicators.BloodPressure.Systolic,
            Chol = indicators.Cholesterol.SerumCholesterolMgDl,
            Fbs = indicators.Cholesterol.FastingBloodSugarAbove120 ? 1 : 0,
            Restecg = (int)indicators.Ecg.RestingEcg,
            Thalach = indicators.Ecg.MaxHeartRateBpm,
            Exang = indicators.Ecg.HasExerciseInducedAngina ? 1 : 0,
            Oldpeak = (double)indicators.Ecg.OldpeakStDepression,
            Slope = (int)indicators.Ecg.Slope,
            Ca = indicators.FluoroscopyVessels,
            Thal = (int)indicators.Thalassemia
        };
}

/// <summary>
/// Encapsulates the complete intake request payload sent to the FastAPI ML inference service.
/// </summary>
public sealed record MlInferenceRequestDto
{
    [JsonPropertyName("encounter_id")]
    public string? EncounterId { get; init; }

    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("patient_mrn")]
    public string? PatientMrn { get; init; }

    [JsonPropertyName("clinical_indicators")]
    public required MlIndicatorsPayloadDto ClinicalIndicators { get; init; }

    /// <summary>
    /// Makes an ML inference request DTO.
    /// </summary>
    public static MlInferenceRequestDto Make(
        MlIndicatorsPayloadDto indicators,
        string? encounterId = null,
        string? correlationId = null,
        string? patientMrn = null) =>
        new()
        {
            ClinicalIndicators = indicators,
            EncounterId = encounterId,
            CorrelationId = correlationId,
            PatientMrn = patientMrn
        };
}

/// <summary>
/// Calibrated risk 95% confidence interval bounds.
/// </summary>
public sealed record MlConfidenceIntervalDto
{
    [JsonPropertyName("lower")]
    public double Lower { get; init; }

    [JsonPropertyName("upper")]
    public double Upper { get; init; }

    [JsonPropertyName("confidence_level")]
    public double ConfidenceLevel { get; init; } = 0.95;

    [JsonPropertyName("margin_percentage")]
    public double MarginPercentage { get; init; }

    /// <summary>
    /// Makes a confidence interval DTO.
    /// </summary>
    public static MlConfidenceIntervalDto Make(
        double lower,
        double upper,
        double confidenceLevel = 0.95,
        double marginPercentage = 0.0) =>
        new()
        {
            Lower = lower,
            Upper = upper,
            ConfidenceLevel = confidenceLevel,
            MarginPercentage = marginPercentage
        };

    /// <summary>
    /// Makes a Domain ConfidenceInterval value object.
    /// </summary>
    public ConfidenceInterval MakeDomainConfidenceInterval() =>
        ConfidenceInterval.Make((decimal)Lower, (decimal)Upper, (decimal)ConfidenceLevel);
}

/// <summary>
/// Individual local SHAP feature attribution element.
/// </summary>
public sealed record MlFeatureAttributionDto
{
    [JsonPropertyName("feature_key")]
    public string FeatureKey { get; init; } = string.Empty;

    [JsonPropertyName("feature_label")]
    public string FeatureLabel { get; init; } = string.Empty;

    [JsonPropertyName("patient_value")]
    public string PatientValue { get; init; } = string.Empty;

    [JsonPropertyName("shap_value")]
    public double ShapValue { get; init; }

    [JsonPropertyName("impact_direction")]
    public string ImpactDirection { get; init; } = string.Empty;

    [JsonPropertyName("rank")]
    public int Rank { get; init; }

    /// <summary>
    /// Makes a feature attribution DTO.
    /// </summary>
    public static MlFeatureAttributionDto Make(
        string featureKey,
        string featureLabel,
        string patientValue,
        double shapValue,
        string impactDirection,
        int rank) =>
        new()
        {
            FeatureKey = featureKey,
            FeatureLabel = featureLabel,
            PatientValue = patientValue,
            ShapValue = shapValue,
            ImpactDirection = impactDirection,
            Rank = rank
        };

    /// <summary>
    /// Makes a Domain ShapAttribution entity.
    /// </summary>
    public ShapAttribution MakeDomainShapAttribution(Guid riskAssessmentResultId)
    {
        var mappedDirection = ImpactDirection.ToUpperInvariant() switch
        {
            "RISK_INCREASING" => Domain.Enums.ImpactDirection.RiskIncreasing,
            "PROTECTIVE" => Domain.Enums.ImpactDirection.Protective,
            _ => Domain.Enums.ImpactDirection.Neutral
        };

        return ShapAttribution.Make(
            riskAssessmentResultId: riskAssessmentResultId,
            featureKey: FeatureKey,
            featureDisplayName: FeatureLabel,
            featureValueDisplay: PatientValue,
            attributionValue: (decimal)ShapValue,
            impactDirection: mappedDirection,
            percentageImpact: null,
            rankOrder: Math.Max(1, Rank)
        );
    }
}

/// <summary>
/// Complete CDSS inference response payload returned by the FastAPI inference service.
/// </summary>
public sealed record MlInferenceResultDto
{
    [JsonPropertyName("encounter_id")]
    public string? EncounterId { get; init; }

    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("risk_probability")]
    public double RiskProbability { get; init; }

    [JsonPropertyName("risk_band")]
    public string RiskBand { get; init; } = string.Empty;

    [JsonPropertyName("is_cad_positive")]
    public bool IsCadPositive { get; init; }

    [JsonPropertyName("decision_threshold")]
    public double DecisionThreshold { get; init; }

    [JsonPropertyName("confidence_interval")]
    public MlConfidenceIntervalDto ConfidenceInterval { get; init; } = null!;

    [JsonPropertyName("top_risk_drivers")]
    public IReadOnlyList<MlFeatureAttributionDto> TopRiskDrivers { get; init; } = [];

    [JsonPropertyName("top_protective_factors")]
    public IReadOnlyList<MlFeatureAttributionDto> TopProtectiveFactors { get; init; } = [];

    [JsonPropertyName("all_attributions")]
    public IReadOnlyList<MlFeatureAttributionDto> AllAttributions { get; init; } = [];

    [JsonPropertyName("explanations")]
    public IReadOnlyList<MlFeatureAttributionDto> Explanations { get; init; } = [];

    [JsonPropertyName("clinical_warnings")]
    public IReadOnlyList<string> ClinicalWarnings { get; init; } = [];

    [JsonPropertyName("recommended_action")]
    public string RecommendedAction { get; init; } = string.Empty;

    [JsonPropertyName("clinical_disclaimer")]
    public string ClinicalDisclaimer { get; init; } = string.Empty;

    [JsonPropertyName("inference_latency_ms")]
    public double InferenceLatencyMs { get; init; }

    /// <summary>
    /// Makes a Domain RiskAssessmentResult entity.
    /// </summary>
    public RiskAssessmentResult MakeRiskAssessmentResult(
        Guid encounterId,
        Guid modelId,
        decimal baseExpectedValue = 0.5m)
    {
        var mappedBand = RiskBand.ToLowerInvariant() switch
        {
            "low" => Domain.Enums.RiskBand.Low,
            "moderate" => Domain.Enums.RiskBand.Moderate,
            "elevated" => Domain.Enums.RiskBand.Elevated,
            "high" => Domain.Enums.RiskBand.High,
            _ => Domain.Enums.RiskBand.Moderate
        };

        var domainCi = ConfidenceInterval != null
            ? ConfidenceInterval.MakeDomainConfidenceInterval()
            : HeartCDSS.Domain.ValueObjects.ConfidenceInterval.Make((decimal)RiskProbability, (decimal)RiskProbability, 0.95m);

        var result = RiskAssessmentResult.Make(
            encounterId: encounterId,
            modelId: modelId,
            riskProbability: (decimal)RiskProbability,
            riskBand: mappedBand,
            confidenceInterval: domainCi,
            decisionThreshold: (decimal)DecisionThreshold,
            baseExpectedValue: baseExpectedValue,
            cdssAdvisoryText: ClinicalDisclaimer,
            recommendedAction: RecommendedAction
        );

        var attributionsList = Explanations.Count > 0 ? Explanations : AllAttributions;
        foreach (var attrDto in attributionsList)
        {
            result.AddAttribution(attrDto.MakeDomainShapAttribution(result.Id));
        }

        return result;
    }
}

/// <summary>
/// Microservice health check response schema from GET /health.
/// </summary>
public sealed record MlHealthCheckResultDto
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("model_loaded")]
    public bool ModelLoaded { get; init; }

    [JsonPropertyName("model_version")]
    public string ModelVersion { get; init; } = string.Empty;

    [JsonPropertyName("calibrator_method")]
    public string CalibratorMethod { get; init; } = string.Empty;

    [JsonPropertyName("decision_threshold")]
    public double DecisionThreshold { get; init; }

    [JsonPropertyName("uptime_seconds")]
    public double UptimeSeconds { get; init; }

    [JsonPropertyName("memory_usage_mb")]
    public double MemoryUsageMb { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    /// <summary>
    /// Makes a health check result DTO.
    /// </summary>
    public static MlHealthCheckResultDto Make(
        string status,
        bool modelLoaded,
        string modelVersion,
        string calibratorMethod,
        double decisionThreshold,
        double uptimeSeconds,
        double memoryUsageMb,
        string timestamp) =>
        new()
        {
            Status = status,
            ModelLoaded = modelLoaded,
            ModelVersion = modelVersion,
            CalibratorMethod = calibratorMethod,
            DecisionThreshold = decisionThreshold,
            UptimeSeconds = uptimeSeconds,
            MemoryUsageMb = memoryUsageMb,
            Timestamp = timestamp
        };
}
