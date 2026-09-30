using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;

namespace HeartCDSS.Application.Encounters.Queries.GetEncounterById;

/// <summary>
/// Detail DTO representing individual SHAP feature attributions contributing to an assessment.
/// </summary>
public sealed record ShapAttributionDetailDto(
    string FeatureKey,
    string FeatureDisplayName,
    string FeatureValueDisplay,
    decimal AttributionValue,
    string ImpactDirection,
    int RankOrder
);

/// <summary>
/// Detail DTO representing a historical or calculated risk assessment outcome.
/// </summary>
public sealed record RiskAssessmentDetailDto(
    Guid Id,
    decimal RiskProbability,
    string RiskBand,
    decimal CiLowerBound,
    decimal CiUpperBound,
    decimal DecisionThreshold,
    string CdssAdvisoryText,
    string? RecommendedAction,
    DateTimeOffset AssessedAt,
    bool IsCadPositive,
    IReadOnlyList<ShapAttributionDetailDto> Attributions
);

/// <summary>
/// Detail DTO representing the 14 standard physiological features captured during the encounter.
/// </summary>
public sealed record ClinicalIndicatorsDetailDto(
    int Age,
    int Sex,
    int ChestPainType,
    int RestingBp,
    int? DiastolicBp,
    int SerumCholesterol,
    int FastingBloodSugar,
    int RestingEcg,
    int MaxHeartRate,
    int ExerciseAngina,
    decimal OldpeakStDepression,
    int Slope,
    int FluoroscopyVessels,
    int Thalassemia
);

/// <summary>
/// Comprehensive DTO returned when querying a clinical encounter by ID.
/// </summary>
public sealed record EncounterDetailDto(
    Guid EncounterId,
    Guid PatientId,
    Guid ClinicianId,
    string EncounterNumber,
    string EncounterType,
    string Status,
    string? ChiefComplaint,
    DateTimeOffset EncounterDate,
    ClinicalIndicatorsDetailDto? Indicators,
    IReadOnlyList<RiskAssessmentDetailDto> RiskAssessments,
    IReadOnlyList<string> ClinicalAlerts
)
{
    /// <summary>
    /// Factory method mapping a domain ClinicalEncounter aggregate into an EncounterDetailDto.
    /// </summary>
    public static EncounterDetailDto MakeFromDomain(ClinicalEncounter encounter)
    {
        ArgumentNullException.ThrowIfNull(encounter);

        ClinicalIndicatorsDetailDto? madeIndicators = null;
        IReadOnlyList<string> madeAlerts = [];

        if (encounter.Indicators is not null)
        {
            var ind = encounter.Indicators;
            madeIndicators = new ClinicalIndicatorsDetailDto(
                Age: ind.AgeAtEncounter,
                Sex: ind.Sex,
                ChestPainType: (int)ind.ChestPainType,
                RestingBp: ind.BloodPressure.Systolic,
                DiastolicBp: ind.BloodPressure.Diastolic,
                SerumCholesterol: ind.Cholesterol.SerumCholesterolMgDl,
                FastingBloodSugar: ind.Cholesterol.FastingBloodSugarAbove120 ? 1 : 0,
                RestingEcg: (int)ind.Ecg.RestingEcg,
                MaxHeartRate: ind.Ecg.MaxHeartRateBpm,
                ExerciseAngina: ind.Ecg.HasExerciseInducedAngina ? 1 : 0,
                OldpeakStDepression: ind.Ecg.OldpeakStDepression,
                Slope: (int)ind.Ecg.Slope,
                FluoroscopyVessels: ind.FluoroscopyVessels,
                Thalassemia: (int)ind.Thalassemia
            );

            madeAlerts = ind.GetClinicalWarnings();
        }

        var madeAssessments = encounter.RiskAssessments.Select(r => new RiskAssessmentDetailDto(
            Id: r.Id,
            RiskProbability: r.RiskProbability,
            RiskBand: r.RiskBand.ToString(),
            CiLowerBound: r.ConfidenceInterval.LowerBound,
            CiUpperBound: r.ConfidenceInterval.UpperBound,
            DecisionThreshold: r.DecisionThreshold,
            CdssAdvisoryText: r.CdssAdvisoryText,
            RecommendedAction: r.RecommendedAction,
            AssessedAt: r.AssessedAt,
            IsCadPositive: r.IsCadPositive,
            Attributions: r.Attributions.Select(a => new ShapAttributionDetailDto(
                FeatureKey: a.FeatureKey,
                FeatureDisplayName: a.FeatureDisplayName,
                FeatureValueDisplay: a.FeatureValueDisplay,
                AttributionValue: a.AttributionValue,
                ImpactDirection: a.ImpactDirection.ToString(),
                RankOrder: a.RankOrder
            )).ToList()
        )).ToList();

        return new EncounterDetailDto(
            EncounterId: encounter.Id,
            PatientId: encounter.PatientId,
            ClinicianId: encounter.ClinicianId,
            EncounterNumber: encounter.EncounterNumber,
            EncounterType: encounter.EncounterType.ToString(),
            Status: encounter.Status.ToString(),
            ChiefComplaint: encounter.ChiefComplaint,
            EncounterDate: encounter.EncounterDate,
            Indicators: madeIndicators,
            RiskAssessments: madeAssessments,
            ClinicalAlerts: madeAlerts
        );
    }
}
