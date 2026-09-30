using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.Entities;

public class RiskAssessmentResultTests
{
    [Fact]
    public void Make_WithValidParameters_ShouldInstantiate()
    {
        var encounterId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var ci = ConfidenceInterval.Make(0.6500m, 0.7500m);

        var result = RiskAssessmentResult.Make(
            encounterId: encounterId,
            modelId: modelId,
            riskProbability: 0.6950m,
            riskBand: RiskBand.Elevated,
            confidenceInterval: ci,
            decisionThreshold: 0.2900m,
            baseExpectedValue: -0.4215m,
            cdssAdvisoryText: "Clinical evaluation advisory.",
            recommendedAction: "Coronary CTA recommended."
        );

        result.EncounterId.Should().Be(encounterId);
        result.ModelId.Should().Be(modelId);
        result.RiskProbability.Should().Be(0.6950m);
        result.RiskBand.Should().Be(RiskBand.Elevated);
        result.ConfidenceInterval.Should().Be(ci);
        result.DecisionThreshold.Should().Be(0.2900m);
        result.IsCadPositive.Should().BeTrue();
        result.Attributions.Should().BeEmpty();
    }

    [Fact]
    public void AddAttribution_ShouldAppendToCollection()
    {
        var result = RiskAssessmentResult.Make(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.55m,
            RiskBand.Elevated,
            ConfidenceInterval.Make(0.50m, 0.60m),
            0.29m,
            0.0m,
            "Advisory"
        );

        var attr = ShapAttribution.Make(
            riskAssessmentResultId: result.Id,
            featureKey: "oldpeak",
            featureDisplayName: "ST Depression",
            featureValueDisplay: "2.8 mm",
            attributionValue: 0.1420m,
            impactDirection: ImpactDirection.RiskIncreasing,
            percentageImpact: 14.2m,
            rankOrder: 1
        );

        result.AddAttribution(attr);

        result.Attributions.Should().ContainSingle().Which.Should().Be(attr);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Make_WithInvalidRiskProbability_ShouldThrow(decimal invalidProb)
    {
        var act = () => RiskAssessmentResult.Make(
            Guid.NewGuid(),
            Guid.NewGuid(),
            invalidProb,
            RiskBand.Low,
            ConfidenceInterval.Make(0.1m, 0.2m),
            0.29m,
            0.0m,
            "Advisory"
        );

        act.Should().Throw<InvalidPhysiologicalValueException>();
    }
}
