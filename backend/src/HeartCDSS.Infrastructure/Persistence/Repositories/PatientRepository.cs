using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HeartCDSS.Infrastructure.Persistence.Repositories;

public sealed class PatientRepository : IPatientRepository
{
    private readonly HeartCDSSDbContext _context;

    public PatientRepository(HeartCDSSDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .Include(p => p.Encounters)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Patient?> GetByMrnAsync(string mrn, CancellationToken cancellationToken = default)
    {
        var normalized = mrn.Trim().ToUpperInvariant();
        return await _context.Patients
            .Include(p => p.Encounters)
            .FirstOrDefaultAsync(p => p.Mrn == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> SearchByNameAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        var cleanQuery = query.Trim().ToLowerInvariant();
        return await _context.Patients
            .Where(p => p.LastName.ToLower().Contains(cleanQuery) || p.FirstName.ToLower().Contains(cleanQuery) || p.Mrn.ToLower().Contains(cleanQuery))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithMrnAsync(string mrn, CancellationToken cancellationToken = default)
    {
        var normalized = mrn.Trim().ToUpperInvariant();
        return await _context.Patients.AnyAsync(p => p.Mrn == normalized, cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patient);
        await _context.Patients.AddAsync(patient, cancellationToken);
    }

    public void Update(Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);
        _context.Patients.Update(patient);
    }

    public void Delete(Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);
        patient.MarkDeleted();
        _context.Patients.Update(patient);
    }
}
