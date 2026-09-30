using HeartCDSS.Application.Common.Models;
using HeartCDSS.Domain.Enums;

namespace HeartCDSS.Application.Encounters.Commands.MakeEncounter;

/// <summary>
/// Data transfer object returned upon successful encounter making, physiological intake, and automated risk evaluation.
/// </summary>
public sealed record MakeEncounterResultDto(
    Guid EncounterId,
    Guid PatientId,
    Guid ClinicianId,
    string EncounterNumber,
    EncounterType EncounterType,
    EncounterStatus Status,
    string? ChiefComplaint,
    DateTimeOffset EncounterDate,
    IReadOnlyList<string> ClinicalAlerts,
    MlInferenceResultDto? RiskAssessment = null
);
