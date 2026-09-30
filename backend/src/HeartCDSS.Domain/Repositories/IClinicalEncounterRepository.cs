using HeartCDSS.Domain.Entities;

namespace HeartCDSS.Domain.Repositories;

/// <summary>
/// Domain repository interface for managing ClinicalEncounter aggregate persistence.
/// </summary>
public interface IClinicalEncounterRepository
{
    Task<ClinicalEncounter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClinicalEncounter?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClinicalEncounter?> GetByEncounterNumberAsync(string encounterNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicalEncounter>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicalEncounter>> GetRecentByClinicianAsync(Guid clinicianId, int count = 20, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicalEncounter encounter, CancellationToken cancellationToken = default);
    void Update(ClinicalEncounter encounter);
}
