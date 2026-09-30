using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Immutable clinical accountability audit record tracking every inference request, raw JSON payloads, and SHAP vectors.
/// </summary>
public sealed class PredictionAudit : Entity<Guid>
{
    public Guid? RiskAssessmentResultId { get; private set; }
    public Guid EncounterId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string RequestPayload { get; private set; } = string.Empty;
    public string? ResponsePayload { get; private set; }
    public string? ExplainingFeatures { get; private set; }
    public int ExecutionTimeMs { get; private set; }
    public InferenceExecutionStatus InferenceStatus { get; private set; }
    public string? ErrorDetails { get; private set; }
    public string? ClientIp { get; private set; }
    public string? UserAgent { get; private set; }

    private PredictionAudit() { }

    private PredictionAudit(
        Guid id,
        Guid? riskAssessmentResultId,
        Guid encounterId,
        Guid userId,
        Guid correlationId,
        string requestPayload,
        string? responsePayload,
        string? explainingFeatures,
        int executionTimeMs,
        InferenceExecutionStatus inferenceStatus,
        string? errorDetails,
        string? clientIp,
        string? userAgent)
        : base(id)
    {
        RiskAssessmentResultId = riskAssessmentResultId;
        EncounterId = encounterId;
        UserId = userId;
        CorrelationId = correlationId;
        RequestPayload = requestPayload;
        ResponsePayload = responsePayload;
        ExplainingFeatures = explainingFeatures;
        ExecutionTimeMs = executionTimeMs;
        InferenceStatus = inferenceStatus;
        ErrorDetails = errorDetails;
        ClientIp = clientIp;
        UserAgent = userAgent;
    }


    /// <summary>
    /// Factory method recording a successful ML inference roundtrip.
    /// </summary>
    public static PredictionAudit RecordSuccess(
        Guid encounterId,
        Guid userId,
        Guid correlationId,
        string requestPayload,
        string responsePayload,
        int executionTimeMs,
        string? explainingFeatures = null,
        Guid? riskAssessmentResultId = null,
        string? clientIp = null,
        string? userAgent = null,
        Guid? id = null)
    {
        if (encounterId == Guid.Empty)
            throw new DomainException("Encounter ID cannot be empty.");

        if (userId == Guid.Empty)
            throw new DomainException("User ID cannot be empty.");

        if (correlationId == Guid.Empty)
            throw new DomainException("Correlation ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(requestPayload))
            throw new DomainException("Request payload cannot be empty.");

        if (string.IsNullOrWhiteSpace(responsePayload))
            throw new DomainException("Response payload cannot be empty for successful prediction.");

        if (executionTimeMs < 0)
            throw new DomainException($"Execution time ({executionTimeMs} ms) cannot be negative.");

        return new PredictionAudit(
            id ?? Guid.NewGuid(),
            riskAssessmentResultId,
            encounterId,
            userId,
            correlationId,
            requestPayload.Trim(),
            responsePayload.Trim(),
            explainingFeatures?.Trim(),
            executionTimeMs,
            InferenceExecutionStatus.Success,
            null,
            clientIp?.Trim(),
            userAgent?.Trim()
        );
    }

    /// <summary>
    /// Factory method recording a failed or circuit-broken ML inference attempt.
    /// </summary>
    public static PredictionAudit RecordFailure(
        Guid encounterId,
        Guid userId,
        Guid correlationId,
        string requestPayload,
        int executionTimeMs,
        InferenceExecutionStatus failureStatus,
        string errorDetails,
        string? clientIp = null,
        string? userAgent = null,
        Guid? id = null)
    {
        if (encounterId == Guid.Empty)
            throw new DomainException("Encounter ID cannot be empty.");

        if (userId == Guid.Empty)
            throw new DomainException("User ID cannot be empty.");

        if (correlationId == Guid.Empty)
            throw new DomainException("Correlation ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(requestPayload))
            throw new DomainException("Request payload cannot be empty.");

        if (failureStatus == InferenceExecutionStatus.Success)
            throw new DomainException("Failure audit cannot have Success status.");

        if (string.IsNullOrWhiteSpace(errorDetails))
            throw new DomainException("Error details are mandatory when recording prediction failure.");

        return new PredictionAudit(
            id ?? Guid.NewGuid(),
            null,
            encounterId,
            userId,
            correlationId,
            requestPayload.Trim(),
            null,
            null,
            Math.Max(0, executionTimeMs),
            failureStatus,
            errorDetails.Trim(),
            clientIp?.Trim(),
            userAgent?.Trim()
        );
    }

}
