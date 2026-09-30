using HeartCDSS.Domain.Entities;

namespace HeartCDSS.Domain.Repositories;

/// <summary>
/// Domain repository interface for managing RiskAssessmentResult persistence.
/// </summary>
public interface IRiskAssessmentRepository
{
    Task<RiskAssessmentResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RiskAssessmentResult?> GetLatestByEncounterIdAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RiskAssessmentResult>> GetHistoryByEncounterIdAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task AddAsync(RiskAssessmentResult assessment, CancellationToken cancellationToken = default);
}
