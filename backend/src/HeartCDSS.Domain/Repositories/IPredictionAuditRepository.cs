using HeartCDSS.Domain.Entities;

namespace HeartCDSS.Domain.Repositories;

/// <summary>
/// Domain repository interface for immutable PredictionAudit persistence.
/// </summary>
public interface IPredictionAuditRepository
{
    Task<PredictionAudit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PredictionAudit?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PredictionAudit>> GetByEncounterIdAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task AddAsync(PredictionAudit audit, CancellationToken cancellationToken = default);
}
