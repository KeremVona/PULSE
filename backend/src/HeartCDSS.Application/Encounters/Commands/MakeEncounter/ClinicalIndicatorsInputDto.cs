namespace HeartCDSS.Application.Encounters.Commands.MakeEncounter;

/// <summary>
/// Data transfer object carrying the 14 standard physiological features and laboratory markers.
/// </summary>
public sealed record ClinicalIndicatorsInputDto(
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
