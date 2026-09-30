using FluentAssertions;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.ValueObjects;

public class BloodPressureTests
{
    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(145)]
    [InlineData(260)]
    public void Make_WithValidSystolic_ShouldSucceed(int systolic)
    {
        // Act
        var bp = BloodPressure.Make(systolic);

        // Assert
        bp.Systolic.Should().Be(systolic);
        bp.Diastolic.Should().BeNull();
    }

    [Theory]
    [InlineData(120, 80)]
    [InlineData(140, 90)]
    [InlineData(60, 40)]
    [InlineData(260, 140)]
    public void Make_WithValidSystolicAndDiastolic_ShouldSucceed(int systolic, int diastolic)
    {
        // Act
        var bp = BloodPressure.Make(systolic, diastolic);

        // Assert
        bp.Systolic.Should().Be(systolic);
        bp.Diastolic.Should().Be(diastolic);
    }

    [Theory]
    [InlineData(59)]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(261)]
    [InlineData(350)]
    public void Make_WithInvalidSystolic_ShouldThrowInvalidPhysiologicalValueException(int invalidSystolic)
    {
        // Act
        var act = () => BloodPressure.Make(invalidSystolic);

        // Assert
        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*Systolic*violates physiological boundaries*");
    }

    [Theory]
    [InlineData(39)]
    [InlineData(141)]
    public void Make_WithInvalidDiastolic_ShouldThrowInvalidPhysiologicalValueException(int invalidDiastolic)
    {
        // Act
        var act = () => BloodPressure.Make(120, invalidDiastolic);

        // Assert
        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*Diastolic*violates physiological boundaries*");
    }

    [Theory]
    [InlineData(115, "Normal", false, false, false)]
    [InlineData(125, "Elevated", false, false, false)]
    [InlineData(135, "Stage 1 Hypertension", true, false, false)]
    [InlineData(150, "Stage 2 Hypertension", true, false, false)]
    [InlineData(190, "Hypertensive Crisis", true, true, false)]
    [InlineData(85, "Normal", false, false, true)]
    public void GetAhaCategory_AndProperties_ShouldMatchClinicalStandards(
        int systolic,
        string expectedCategory,
        bool isHypertensive,
        bool isCrisis,
        bool isHypotensive)
    {
        var bp = BloodPressure.Make(systolic);

        bp.GetAhaCategory().Should().Be(expectedCategory);
        bp.IsHypertensive.Should().Be(isHypertensive);
        bp.IsHypertensiveCrisis.Should().Be(isCrisis);
        bp.IsHypotensive.Should().Be(isHypotensive);
    }

    [Fact]
    public void ValueObject_EqualityAndImmutability_ShouldBehaveCorrectly()
    {
        var bp1 = BloodPressure.Make(140, 90);
        var bp2 = BloodPressure.Make(140, 90);
        var bp3 = BloodPressure.Make(140, 85);

        bp1.Should().Be(bp2);
        (bp1 == bp2).Should().BeTrue();
        (bp1 != bp3).Should().BeTrue();
        bp1.GetHashCode().Should().Be(bp2.GetHashCode());
    }
}
