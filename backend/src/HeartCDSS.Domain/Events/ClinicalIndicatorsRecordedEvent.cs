using HeartCDSS.Domain.Common;

namespace HeartCDSS.Domain.Events;

/// <summary>
/// Dispatched when physiological indicators and laboratory values are recorded for an encounter.
/// </summary>
public sealed record ClinicalIndicatorsRecordedEvent(
    Guid IndicatorsId,
    Guid EncounterId,
    int AgeAtEncounter,
    int SystolicBp,
    int Cholesterol,
    DateTimeOffset OccurredOn,
    Guid EventId) : IDomainEvent
{
    public ClinicalIndicatorsRecordedEvent(
        Guid indicatorsId,
        Guid encounterId,
        int ageAtEncounter,
        int systolicBp,
        int cholesterol)
        : this(indicatorsId, encounterId, ageAtEncounter, systolicBp, cholesterol, DateTimeOffset.UtcNow, Guid.NewGuid())
    {
    }
}
