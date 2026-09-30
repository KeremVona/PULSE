using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Domain entity representing calibrated cardiovascular risk predictions and XAI explanations produced by the ML service.
/// </summary>
public sealed class RiskAssessmentResult : Entity<Guid>
{
    private readonly List<ShapAttribution> _attributions = [];

    public Guid EncounterId { get; private set; }
    public Guid ModelId { get; private set; }
    public decimal RiskProbability { get; private set; }
    public RiskBand RiskBand { get; private set; }
    public ConfidenceInterval ConfidenceInterval { get; private set; } = null!;
    public decimal DecisionThreshold { get; private set; }
    public decimal BaseExpectedValue { get; private set; }
    public string CdssAdvisoryText { get; private set; } = string.Empty;
    public string? RecommendedAction { get; private set; }
    public DateTimeOffset AssessedAt { get; private set; }

    public bool IsCadPositive => RiskProbability >= DecisionThreshold;

    public IReadOnlyCollection<ShapAttribution> Attributions => _attributions.AsReadOnly();

    private RiskAssessmentResult() { }

    private RiskAssessmentResult(
        Guid id,
        Guid encounterId,
        Guid modelId,
        decimal riskProbability,
        RiskBand riskBand,
        ConfidenceInterval confidenceInterval,
        decimal decisionThreshold,
        decimal baseExpectedValue,
        string cdssAdvisoryText,
        string? recommendedAction,
        DateTimeOffset assessedAt)
        : base(id)
    {
        EncounterId = encounterId;
        ModelId = modelId;
        RiskProbability = decimal.Round(riskProbability, 4);
        RiskBand = riskBand;
        ConfidenceInterval = confidenceInterval;
        DecisionThreshold = decimal.Round(decisionThreshold, 4);
        BaseExpectedValue = decimal.Round(baseExpectedValue, 4);
        CdssAdvisoryText = cdssAdvisoryText;
        RecommendedAction = recommendedAction;
        AssessedAt = assessedAt;
    }

    /// <summary>
    /// Factory method instantiating a validated RiskAssessmentResult.
    /// </summary>
    public static RiskAssessmentResult Make(
        Guid encounterId,
        Guid modelId,
        decimal riskProbability,
        RiskBand riskBand,
        ConfidenceInterval confidenceInterval,
        decimal decisionThreshold,
        decimal baseExpectedValue,
        string cdssAdvisoryText,
        string? recommendedAction = null,
        DateTimeOffset? assessedAt = null,
        Guid? id = null)
    {
        if (encounterId == Guid.Empty)
            throw new DomainException("Encounter ID cannot be empty.");

        if (modelId == Guid.Empty)
            throw new DomainException("Model ID cannot be empty.");

        if (riskProbability < 0.0m || riskProbability > 1.0m)
            throw new InvalidPhysiologicalValueException(nameof(RiskProbability), riskProbability, "0.0000 - 1.0000");

        if (decisionThreshold < 0.0m || decisionThreshold > 1.0m)
            throw new InvalidPhysiologicalValueException(nameof(DecisionThreshold), decisionThreshold, "0.0000 - 1.0000");

        ArgumentNullException.ThrowIfNull(confidenceInterval);

        if (string.IsNullOrWhiteSpace(cdssAdvisoryText))
            throw new DomainException("Mandatory CDSS advisory text must be supplied.");

        return new RiskAssessmentResult(
            id ?? Guid.NewGuid(),
            encounterId,
            modelId,
            riskProbability,
            riskBand,
            confidenceInterval,
            decisionThreshold,
            baseExpectedValue,
            cdssAdvisoryText.Trim(),
            recommendedAction?.Trim(),
            assessedAt ?? DateTimeOffset.UtcNow
        );
    }

    public void AddAttribution(ShapAttribution attribution)
    {
        ArgumentNullException.ThrowIfNull(attribution);
        _attributions.Add(attribution);
        SetUpdatedAt();
    }

    public void AddAttributions(IEnumerable<ShapAttribution> attributions)
    {
        ArgumentNullException.ThrowIfNull(attributions);
        _attributions.AddRange(attributions);
        SetUpdatedAt();
    }
}
