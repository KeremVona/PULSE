using MediatR;

namespace HeartCDSS.Application.Encounters.Queries.GetEncounterById;

/// <summary>
/// Query retrieving a clinical encounter and its risk assessment outcome by patient and encounter ID.
/// </summary>
public sealed record GetEncounterByIdQuery(
    Guid PatientId,
    Guid EncounterId
) : IRequest<EncounterDetailDto?>;
