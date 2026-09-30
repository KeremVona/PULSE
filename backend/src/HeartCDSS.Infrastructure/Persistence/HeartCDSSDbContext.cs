using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HeartCDSS.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for PULSE Clinical Decision Support System.
/// Implements PostgreSQL persistence, GIN-indexed JSONB audit logging, and IUnitOfWork.
/// </summary>
public class HeartCDSSDbContext : DbContext, IUnitOfWork
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<ClinicalEncounter> Encounters => Set<ClinicalEncounter>();
    public DbSet<ClinicalIndicators> ClinicalIndicators => Set<ClinicalIndicators>();
    public DbSet<RiskAssessmentResult> RiskAssessments => Set<RiskAssessmentResult>();
    public DbSet<ShapAttribution> ShapAttributions => Set<ShapAttribution>();
    public DbSet<PredictionAudit> PredictionAudits => Set<PredictionAudit>();
    public DbSet<MlModel> MlModels => Set<MlModel>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    public HeartCDSSDbContext(DbContextOptions<HeartCDSSDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discover and register all IEntityTypeConfiguration classes in Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeartCDSSDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Maintain timestamps for auditability
        var entries = ChangeTracker.Entries<Entity<Guid>>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // MadeAt is assigned by default constructor or generator
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
            }
        }

        // 2. Persist state changes
        var result = await base.SaveChangesAsync(cancellationToken);

        // 3. Clear domain events on aggregates after successful database commit
        var aggregates = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}
