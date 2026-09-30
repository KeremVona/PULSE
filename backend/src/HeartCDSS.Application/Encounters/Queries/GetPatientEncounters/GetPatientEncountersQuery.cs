using HeartCDSS.Application.Encounters.Queries.GetEncounterById;
using HeartCDSS.Domain.Repositories;
using MediatR;

namespace HeartCDSS.Application.Encounters.Queries.GetPatientEncounters;

/// <summary>
/// Query retrieving all historical clinical encounters for a specified patient.
/// </summary>
public sealed record GetPatientEncountersQuery(Guid PatientId) : IRequest<IReadOnlyList<EncounterDetailDto>>;

/// <summary>
/// Handler retrieving all encounters for a patient.
/// </summary>
public sealed class GetPatientEncountersQueryHandler : IRequestHandler<GetPatientEncountersQuery, IReadOnlyList<EncounterDetailDto>>
{
    private readonly IClinicalEncounterRepository _encounterRepository;

    public GetPatientEncountersQueryHandler(IClinicalEncounterRepository encounterRepository)
    {
        _encounterRepository = encounterRepository ?? throw new ArgumentNullException(nameof(encounterRepository));
    }

    public async Task<IReadOnlyList<EncounterDetailDto>> Handle(GetPatientEncountersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var madeEncounters = await _encounterRepository.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return madeEncounters.Select(EncounterDetailDto.MakeFromDomain).ToList();
    }
}
