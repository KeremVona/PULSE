using HeartCDSS.Domain.Enums;
using MediatR;

namespace HeartCDSS.Application.Encounters.Commands.MakeEncounter;

/// <summary>
/// MediatR Command to make a new clinical encounter and bind physiological indicators.
/// </summary>
public sealed record MakeEncounterCommand(
    Guid PatientId,
    Guid ClinicianId,
    string EncounterNumber,
    EncounterType EncounterType,
    string? ChiefComplaint,
    ClinicalIndicatorsInputDto Indicators
) : IRequest<MakeEncounterResultDto>;
