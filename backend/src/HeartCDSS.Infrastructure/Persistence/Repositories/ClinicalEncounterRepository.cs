using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HeartCDSS.Infrastructure.Persistence.Repositories;

public sealed class ClinicalEncounterRepository : IClinicalEncounterRepository
{
    private readonly HeartCDSSDbContext _context;

    public ClinicalEncounterRepository(HeartCDSSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ClinicalEncounter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Encounters
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<ClinicalEncounter?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Encounters
            .Include(e => e.Indicators)
            .Include(e => e.RiskAssessments)
                .ThenInclude(r => r.Attributions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<ClinicalEncounter?> GetByEncounterNumberAsync(string encounterNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Encounters
            .Include(e => e.Indicators)
            .Include(e => e.RiskAssessments)
            .FirstOrDefaultAsync(e => e.EncounterNumber == encounterNumber.Trim(), cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicalEncounter>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.Encounters
            .Include(e => e.Indicators)
            .Include(e => e.RiskAssessments)
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.EncounterDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicalEncounter>> GetRecentByClinicianAsync(Guid clinicianId, int count = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Encounters
            .Include(e => e.Indicators)
            .Where(e => e.ClinicianId == clinicianId)
            .OrderByDescending(e => e.EncounterDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ClinicalEncounter encounter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        await _context.Encounters.AddAsync(encounter, cancellationToken);
    }

    public void Update(ClinicalEncounter encounter)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        _context.Encounters.Update(encounter);
    }
}
