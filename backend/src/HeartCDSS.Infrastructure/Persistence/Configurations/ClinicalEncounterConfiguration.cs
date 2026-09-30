using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class ClinicalEncounterConfiguration : IEntityTypeConfiguration<ClinicalEncounter>
{
    public void Configure(EntityTypeBuilder<ClinicalEncounter> builder)
    {
        builder.ToTable("clinical_encounters");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(e => e.ClinicianId)
            .HasColumnName("clinician_id")
            .IsRequired();

        builder.Property(e => e.EncounterNumber)
            .HasColumnName("encounter_number")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(e => e.EncounterNumber)
            .IsUnique()
            .HasDatabaseName("idx_encounters_number");

        builder.Property(e => e.EncounterType)
            .HasColumnName("encounter_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ChiefComplaint)
            .HasColumnName("chief_complaint");

        builder.Property(e => e.EncounterDate)
            .HasColumnName("encounter_date")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(e => new { e.PatientId, e.EncounterDate })
            .HasDatabaseName("idx_encounters_patient_date");

        builder.HasIndex(e => e.ClinicianId)
            .HasDatabaseName("idx_encounters_clinician");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_encounters_status");

        builder.Property(e => e.MadeAt)
            .HasColumnName("made_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.Indicators)
            .WithOne()
            .HasForeignKey<ClinicalIndicators>(i => i.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.RiskAssessments)
            .WithOne()
            .HasForeignKey(r => r.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(e => e.RiskAssessments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
