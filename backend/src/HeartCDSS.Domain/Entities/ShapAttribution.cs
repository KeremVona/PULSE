using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Domain entity representing an individual local SHAP feature attribution contributing to a risk assessment.
/// </summary>
public sealed class ShapAttribution : Entity<Guid>
{
    public Guid RiskAssessmentResultId { get; private set; }
    public string FeatureKey { get; private set; } = string.Empty;
    public string FeatureDisplayName { get; private set; } = string.Empty;
    public string FeatureValueDisplay { get; private set; } = string.Empty;
    public decimal AttributionValue { get; private set; }
    public ImpactDirection ImpactDirection { get; private set; }
    public decimal? PercentageImpact { get; private set; }
    public int RankOrder { get; private set; }

    private ShapAttribution() { }

    private ShapAttribution(
        Guid id,
        Guid riskAssessmentResultId,
        string featureKey,
        string featureDisplayName,
        string featureValueDisplay,
        decimal attributionValue,
        ImpactDirection impactDirection,
        decimal? percentageImpact,
        int rankOrder)
        : base(id)
    {
        RiskAssessmentResultId = riskAssessmentResultId;
        FeatureKey = featureKey;
        FeatureDisplayName = featureDisplayName;
        FeatureValueDisplay = featureValueDisplay;
        AttributionValue = decimal.Round(attributionValue, 5);
        ImpactDirection = impactDirection;
        PercentageImpact = percentageImpact.HasValue ? decimal.Round(percentageImpact.Value, 2) : null;
        RankOrder = rankOrder;
    }

    /// <summary>
    /// Factory method instantiating a validated ShapAttribution.
    /// </summary>
    public static ShapAttribution Make(
        Guid riskAssessmentResultId,
        string featureKey,
        string featureDisplayName,
        string featureValueDisplay,
        decimal attributionValue,
        ImpactDirection impactDirection,
        decimal? percentageImpact = null,
        int rankOrder = 1,
        Guid? id = null)
    {
        if (riskAssessmentResultId == Guid.Empty)
            throw new DomainException("Risk Assessment ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(featureKey))
            throw new DomainException("Feature key cannot be empty.");

        if (string.IsNullOrWhiteSpace(featureDisplayName))
            throw new DomainException("Feature display name cannot be empty.");

        if (rankOrder < 1)
            throw new DomainException($"Rank order ({rankOrder}) must be at least 1.");

        return new ShapAttribution(
            id ?? Guid.NewGuid(),
            riskAssessmentResultId,
            featureKey.Trim(),
            featureDisplayName.Trim(),
            featureValueDisplay.Trim(),
            attributionValue,
            impactDirection,
            percentageImpact,
            rankOrder
        );
    }
}
