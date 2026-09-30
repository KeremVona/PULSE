using HeartCDSS.Domain.Entities;

namespace HeartCDSS.Domain.Repositories;

/// <summary>
/// Domain repository interface for managing Patient aggregate persistence.
/// </summary>
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Patient?> GetByMrnAsync(string mrn, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> SearchByNameAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithMrnAsync(string mrn, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    void Update(Patient patient);
    void Delete(Patient patient);
}
