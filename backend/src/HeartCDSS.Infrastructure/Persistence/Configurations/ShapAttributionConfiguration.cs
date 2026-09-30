using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class ShapAttributionConfiguration : IEntityTypeConfiguration<ShapAttribution>
{
    public void Configure(EntityTypeBuilder<ShapAttribution> builder)
    {
        builder.ToTable("shap_attributions");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.RiskAssessmentResultId)
            .HasColumnName("risk_assessment_id")
            .IsRequired();

        builder.Property(a => a.FeatureKey)
            .HasColumnName("feature_key")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.FeatureDisplayName)
            .HasColumnName("feature_display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.FeatureValueDisplay)
            .HasColumnName("feature_value_display")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.AttributionValue)
            .HasColumnName("attribution_value")
            .HasPrecision(7, 5)
            .IsRequired();

        builder.Property(a => a.ImpactDirection)
            .HasColumnName("impact_direction")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.PercentageImpact)
            .HasColumnName("percentage_impact")
            .HasPrecision(5, 2);

        builder.Property(a => a.RankOrder)
            .HasColumnName("rank_order")
            .HasConversion<short>()
            .IsRequired();

        builder.HasIndex(a => new { a.RiskAssessmentResultId, a.RankOrder })
            .HasDatabaseName("idx_shap_attributions_assessment");

        builder.HasIndex(a => new { a.RiskAssessmentResultId, a.ImpactDirection })
            .HasDatabaseName("idx_shap_attributions_direction");
    }
}
