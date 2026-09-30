using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class MlModelConfiguration : IEntityTypeConfiguration<MlModel>
{
    public void Configure(EntityTypeBuilder<MlModel> builder)
    {
        builder.ToTable("ml_models");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.ModelName)
            .HasColumnName("model_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.ModelVersion)
            .HasColumnName("model_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(m => m.ModelVersion)
            .IsUnique()
            .HasDatabaseName("idx_ml_models_version");

        builder.Property(m => m.ModelFamily)
            .HasColumnName("model_family")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.CalibrationMethod)
            .HasColumnName("calibration_method")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.DecisionThreshold)
            .HasColumnName("decision_threshold")
            .HasPrecision(4, 3)
            .IsRequired();

        builder.Property(m => m.TargetSensitivity)
            .HasColumnName("target_sensitivity")
            .HasPrecision(4, 3)
            .IsRequired();

        builder.Property(m => m.BrierScore)
            .HasColumnName("brier_score")
            .HasPrecision(5, 4);

        builder.Property(m => m.RocAuc)
            .HasColumnName("roc_auc")
            .HasPrecision(4, 3);

        builder.Property(m => m.PrAuc)
            .HasColumnName("pr_auc")
            .HasPrecision(4, 3);

        builder.Property(m => m.ArtifactHash)
            .HasColumnName("artifact_hash")
            .HasMaxLength(64);

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(m => m.IsActive)
            .HasDatabaseName("idx_ml_models_active")
            .HasFilter("is_active = TRUE");

        builder.Property(m => m.DeployedAt)
            .HasColumnName("deployed_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(m => m.MadeAt)
            .HasColumnName("made_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
