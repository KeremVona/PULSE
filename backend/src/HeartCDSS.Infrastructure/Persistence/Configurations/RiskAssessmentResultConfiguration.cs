using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class RiskAssessmentResultConfiguration : IEntityTypeConfiguration<RiskAssessmentResult>
{
    public void Configure(EntityTypeBuilder<RiskAssessmentResult> builder)
    {
        builder.ToTable("risk_assessments");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.EncounterId)
            .HasColumnName("encounter_id")
            .IsRequired();

        builder.Property(r => r.ModelId)
            .HasColumnName("model_id")
            .IsRequired();

        builder.Property(r => r.RiskProbability)
            .HasColumnName("risk_probability")
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(r => r.RiskBand)
            .HasColumnName("risk_band")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.DecisionThreshold)
            .HasColumnName("decision_threshold")
            .HasPrecision(4, 3)
            .IsRequired();

        builder.Property(r => r.BaseExpectedValue)
            .HasColumnName("base_expected_value")
            .HasPrecision(6, 4)
            .IsRequired();

        builder.Property(r => r.CdssAdvisoryText)
            .HasColumnName("cdss_advisory_text")
            .IsRequired();

        builder.Property(r => r.RecommendedAction)
            .HasColumnName("recommended_action");

        builder.Property(r => r.AssessedAt)
            .HasColumnName("assessed_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Owned Value Object: ConfidenceInterval
        builder.OwnsOne(r => r.ConfidenceInterval, ci =>
        {
            ci.Property(c => c.LowerBound)
                .HasColumnName("ci_lower_bound")
                .HasPrecision(5, 4)
                .IsRequired();

            ci.Property(c => c.UpperBound)
                .HasColumnName("ci_upper_bound")
                .HasPrecision(5, 4)
                .IsRequired();

            ci.Property(c => c.ConfidenceLevel)
                .HasColumnName("ci_confidence_level")
                .HasPrecision(4, 2)
                .IsRequired();

            ci.Property(c => c.MarginPercentage)
                .HasColumnName("ci_margin_percentage")
                .HasPrecision(5, 2)
                .IsRequired();
        });

        builder.HasIndex(r => new { r.EncounterId, r.AssessedAt })
            .HasDatabaseName("idx_risk_assessments_encounter");

        builder.HasIndex(r => r.RiskBand)
            .HasDatabaseName("idx_risk_assessments_band");

        builder.HasIndex(r => r.ModelId)
            .HasDatabaseName("idx_risk_assessments_model");

        builder.HasMany(r => r.Attributions)
            .WithOne()
            .HasForeignKey(a => a.RiskAssessmentResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Attributions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
