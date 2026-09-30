using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HeartCDSS.Infrastructure.Persistence.Repositories;

public sealed class RiskAssessmentRepository : IRiskAssessmentRepository
{
    private readonly HeartCDSSDbContext _context;

    public RiskAssessmentRepository(HeartCDSSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RiskAssessmentResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RiskAssessments
            .Include(r => r.Attributions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<RiskAssessmentResult?> GetLatestByEncounterIdAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        return await _context.RiskAssessments
            .Include(r => r.Attributions)
            .Where(r => r.EncounterId == encounterId)
            .OrderByDescending(r => r.AssessedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RiskAssessmentResult>> GetHistoryByEncounterIdAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        return await _context.RiskAssessments
            .Include(r => r.Attributions)
            .Where(r => r.EncounterId == encounterId)
            .OrderByDescending(r => r.AssessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RiskAssessmentResult assessment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        await _context.RiskAssessments.AddAsync(assessment, cancellationToken);
    }
}
