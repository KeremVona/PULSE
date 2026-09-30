using FluentAssertions;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.ValueObjects;

public class EcgResultTests
{
    [Fact]
    public void Make_WithValidParameters_ShouldSucceed()
    {
        var ecg = EcgResult.Make(
            RestingEcgType.Normal,
            150,
            false,
            1.5m,
            StSlope.Upsloping
        );

        ecg.RestingEcg.Should().Be(RestingEcgType.Normal);
        ecg.MaxHeartRateBpm.Should().Be(150);
        ecg.HasExerciseInducedAngina.Should().BeFalse();
        ecg.OldpeakStDepression.Should().Be(1.5m);
        ecg.Slope.Should().Be(StSlope.Upsloping);
    }

    [Theory]
    [InlineData(39)]
    [InlineData(221)]
    public void Make_WithInvalidHeartRate_ShouldThrow(int invalidHr)
    {
        var act = () => EcgResult.Make(RestingEcgType.Normal, invalidHr, false, 1.0m, StSlope.Flat);

        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*MaxHeartRateBpm*");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(6.6)]
    public void Make_WithInvalidOldpeak_ShouldThrow(decimal invalidOldpeak)
    {
        var act = () => EcgResult.Make(RestingEcgType.Normal, 120, false, invalidOldpeak, StSlope.Flat);

        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*OldpeakStDepression*");
    }

    [Fact]
    public void Make_WithIntegerCodes_ShouldMapProperly()
    {
        var ecg = EcgResult.Make(1, 140, 1, 2.5m, 2);

        ecg.RestingEcg.Should().Be(RestingEcgType.StTAbnormality);
        ecg.MaxHeartRateBpm.Should().Be(140);
        ecg.HasExerciseInducedAngina.Should().BeTrue();
        ecg.OldpeakStDepression.Should().Be(2.5m);
        ecg.Slope.Should().Be(StSlope.Flat);
        ecg.HasHighRiskEcgProfile.Should().BeTrue();
    }

    [Fact]
    public void ClinicalIndicators_BradycardiaAndTachycardia_ShouldBeIdentified()
    {
        var brady = EcgResult.Make(RestingEcgType.Normal, 45, false, 0.0m, StSlope.Upsloping);
        brady.IsBradycardic.Should().BeTrue();
        brady.IsTachycardic.Should().BeFalse();

        var tachy = EcgResult.Make(RestingEcgType.Normal, 210, false, 0.0m, StSlope.Upsloping);
        tachy.IsBradycardic.Should().BeFalse();
        tachy.IsTachycardic.Should().BeTrue();

        var severeSt = EcgResult.Make(RestingEcgType.Normal, 120, false, 4.0m, StSlope.Downsloping);
        severeSt.IsSevereStDepression.Should().BeTrue();
        severeSt.HasHighRiskEcgProfile.Should().BeTrue();
    }

    [Fact]
    public void ValueObject_Equality_ShouldBehaveCorrectly()
    {
        var e1 = EcgResult.Make(RestingEcgType.Normal, 140, false, 1.2m, StSlope.Flat);
        var e2 = EcgResult.Make(RestingEcgType.Normal, 140, false, 1.2m, StSlope.Flat);
        var e3 = EcgResult.Make(RestingEcgType.Normal, 140, true, 1.2m, StSlope.Flat);

        e1.Should().Be(e2);
        (e1 == e2).Should().BeTrue();
        (e1 != e3).Should().BeTrue();
        e1.GetHashCode().Should().Be(e2.GetHashCode());
    }
}
