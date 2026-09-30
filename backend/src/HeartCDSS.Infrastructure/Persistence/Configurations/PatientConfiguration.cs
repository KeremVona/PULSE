using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Mrn)
            .HasColumnName("mrn")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(p => p.Mrn)
            .IsUnique()
            .HasDatabaseName("idx_patients_mrn")
            .HasFilter("is_deleted = FALSE");

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => new { p.LastName, p.FirstName })
            .HasDatabaseName("idx_patients_name");

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth")
            .IsRequired();

        builder.HasIndex(p => p.DateOfBirth)
            .HasDatabaseName("idx_patients_dob");

        builder.Property(p => p.BiologicalSex)
            .HasColumnName("biological_sex")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(p => p.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(25);

        builder.Property(p => p.EmergencyContactName)
            .HasColumnName("emergency_contact_name")
            .HasMaxLength(100);

        builder.Property(p => p.EmergencyContactPhone)
            .HasColumnName("emergency_contact_phone")
            .HasMaxLength(25);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PatientStatus.Active)
            .HasSentinel((PatientStatus)0)
            .IsRequired();

        builder.Property(p => p.MadeAt)
            .HasColumnName("made_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasMany(p => p.Encounters)
            .WithOne()
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(p => p.Encounters)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
