using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;

namespace HeartCDSS.Domain.Events;

/// <summary>
/// Dispatched when a new patient clinical encounter is initiated.
/// </summary>
public sealed record EncounterMadeEvent(
    Guid EncounterId,
    Guid PatientId,
    Guid ClinicianId,
    string EncounterNumber,
    EncounterType EncounterType,
    DateTimeOffset OccurredOn,
    Guid EventId) : IDomainEvent
{
    public EncounterMadeEvent(
        Guid encounterId,
        Guid patientId,
        Guid clinicianId,
        string encounterNumber,
        EncounterType encounterType)
        : this(encounterId, patientId, clinicianId, encounterNumber, encounterType, DateTimeOffset.UtcNow, Guid.NewGuid())
    {
    }
}
