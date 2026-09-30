using HeartCDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeartCDSS.Infrastructure.Persistence.Configurations;

public sealed class PredictionAuditConfiguration : IEntityTypeConfiguration<PredictionAudit>
{
    public void Configure(EntityTypeBuilder<PredictionAudit> builder)
    {
        builder.ToTable("prediction_audits");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.RiskAssessmentResultId)
            .HasColumnName("risk_assessment_id");

        builder.Property(p => p.EncounterId)
            .HasColumnName("encounter_id")
            .IsRequired();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.CorrelationId)
            .HasColumnName("correlation_id")
            .IsRequired();

        builder.HasIndex(p => p.CorrelationId)
            .IsUnique()
            .HasDatabaseName("idx_prediction_audits_correlation");

        // Immutable JSONB Audit Snapshots
        builder.Property(p => p.RequestPayload)
            .HasColumnName("request_payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(p => p.ResponsePayload)
            .HasColumnName("response_payload")
            .HasColumnType("jsonb");

        builder.Property(p => p.ExplainingFeatures)
            .HasColumnName("explaining_features")
            .HasColumnType("jsonb");

        builder.Property(p => p.ExecutionTimeMs)
            .HasColumnName("execution_time_ms")
            .IsRequired();

        builder.Property(p => p.InferenceStatus)
            .HasColumnName("inference_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.ErrorDetails)
            .HasColumnName("error_details");

        builder.Property(p => p.ClientIp)
            .HasColumnName("client_ip");

        builder.Property(p => p.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(255);

        builder.Property(p => p.MadeAt)
            .HasColumnName("made_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(p => p.EncounterId)
            .HasDatabaseName("idx_prediction_audits_encounter");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("idx_prediction_audits_user");

        builder.HasIndex(p => p.MadeAt)
            .HasDatabaseName("idx_prediction_audits_made");

        // PostgreSQL GIN Indexes on JSONB Columns for rapid querying of AI artifacts
        builder.HasIndex(p => p.RequestPayload)
            .HasDatabaseName("idx_prediction_audits_req_json")
            .HasMethod("gin");

        builder.HasIndex(p => p.ResponsePayload)
            .HasDatabaseName("idx_prediction_audits_res_json")
            .HasMethod("gin");

        builder.HasIndex(p => p.ExplainingFeatures)
            .HasDatabaseName("idx_prediction_audits_explaining_json")
            .HasMethod("gin");
    }
}
