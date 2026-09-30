using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HeartCDSS.Infrastructure.Persistence.Repositories;

public sealed class PredictionAuditRepository : IPredictionAuditRepository
{
    private readonly HeartCDSSDbContext _context;

    public PredictionAuditRepository(HeartCDSSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PredictionAudit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PredictionAudits
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PredictionAudit?> GetByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return await _context.PredictionAudits
            .FirstOrDefaultAsync(p => p.CorrelationId == correlationId, cancellationToken);
    }

    public async Task<IReadOnlyList<PredictionAudit>> GetByEncounterIdAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        return await _context.PredictionAudits
            .Where(p => p.EncounterId == encounterId)
            .OrderByDescending(p => p.MadeAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PredictionAudit audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audit);
        await _context.PredictionAudits.AddAsync(audit, cancellationToken);
    }
}
