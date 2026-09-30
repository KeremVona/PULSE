using HeartCDSS.Domain.Repositories;
using MediatR;

namespace HeartCDSS.Application.Encounters.Queries.GetEncounterById;

/// <summary>
/// Query handler retrieving an encounter with full clinical indicators and SHAP explainability breakdown.
/// </summary>
public sealed class GetEncounterByIdQueryHandler : IRequestHandler<GetEncounterByIdQuery, EncounterDetailDto?>
{
    private readonly IClinicalEncounterRepository _encounterRepository;

    public GetEncounterByIdQueryHandler(IClinicalEncounterRepository encounterRepository)
    {
        _encounterRepository = encounterRepository ?? throw new ArgumentNullException(nameof(encounterRepository));
    }

    public async Task<EncounterDetailDto?> Handle(GetEncounterByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var madeEncounter = await _encounterRepository.GetWithDetailsAsync(request.EncounterId, cancellationToken);
        if (madeEncounter is null)
        {
            return null;
        }

        if (madeEncounter.PatientId != request.PatientId)
        {
            return null;
        }

        return EncounterDetailDto.MakeFromDomain(madeEncounter);
    }
}
