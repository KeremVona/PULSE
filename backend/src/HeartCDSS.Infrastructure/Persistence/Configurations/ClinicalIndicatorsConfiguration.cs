using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class ClinicalIndicatorsConfiguration : IEntityTypeConfiguration<ClinicalIndicators>
{
    public void Configure(EntityTypeBuilder<ClinicalIndicators> builder)
    {
        builder.ToTable("clinical_indicators");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.EncounterId)
            .HasColumnName("encounter_id")
            .IsRequired();

        builder.HasIndex(i => i.EncounterId)
            .IsUnique()
            .HasDatabaseName("idx_clinical_indicators_encounter");

        builder.Property(i => i.AgeAtEncounter)
            .HasColumnName("age_at_encounter")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(i => i.Sex)
            .HasColumnName("sex")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(i => i.ChestPainType)
            .HasColumnName("chest_pain_type")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(i => i.FluoroscopyVessels)
            .HasColumnName("fluoroscopy_vessels")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(i => i.Thalassemia)
            .HasColumnName("thalassemia")
            .HasConversion<short>()
            .IsRequired();

        // Owned Value Object: BloodPressure
        builder.OwnsOne(i => i.BloodPressure, bp =>
        {
            bp.Property(b => b.Systolic)
                .HasColumnName("resting_bp")
                .HasConversion<short>()
                .IsRequired();

            bp.Property(b => b.Diastolic)
                .HasColumnName("diastolic_bp")
                .HasConversion<short?>();
        });

        // Owned Value Object: CholesterolReading
        builder.OwnsOne(i => i.Cholesterol, chol =>
        {
            chol.Property(c => c.SerumCholesterolMgDl)
                .HasColumnName("serum_cholesterol")
                .HasConversion<short>()
                .IsRequired();

            chol.Property(c => c.FastingBloodSugarAbove120)
                .HasColumnName("fasting_blood_sugar")
                .HasConversion<short>(v => (short)(v ? 1 : 0), v => v == 1)
                .IsRequired();
        });

        // Owned Value Object: EcgResult
        builder.OwnsOne(i => i.Ecg, ecg =>
        {
            ecg.Property(e => e.RestingEcg)
                .HasColumnName("resting_ecg")
                .HasConversion<short>()
                .IsRequired();

            ecg.Property(e => e.MaxHeartRateBpm)
                .HasColumnName("max_heart_rate")
                .HasConversion<short>()
                .IsRequired();

            ecg.Property(e => e.HasExerciseInducedAngina)
                .HasColumnName("exercise_angina")
                .HasConversion<short>(v => (short)(v ? 1 : 0), v => v == 1)
                .IsRequired();

            ecg.Property(e => e.OldpeakStDepression)
                .HasColumnName("oldpeak_st_depression")
                .HasPrecision(3, 1)
                .IsRequired();

            ecg.Property(e => e.Slope)
                .HasColumnName("st_slope")
                .HasConversion<short>()
                .IsRequired();
        });

        builder.Property(i => i.MadeAt)
            .HasColumnName("made_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
