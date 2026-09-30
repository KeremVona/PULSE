using FluentAssertions;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.ValueObjects;

public class CholesterolReadingTests
{
    [Theory]
    [InlineData(80, false)]
    [InlineData(200, true)]
    [InlineData(268, false)]
    [InlineData(600, true)]
    public void Make_WithValidParameters_ShouldSucceed(int cholesterol, bool fbs)
    {
        var reading = CholesterolReading.Make(cholesterol, fbs);

        reading.SerumCholesterolMgDl.Should().Be(cholesterol);
        reading.FastingBloodSugarAbove120.Should().Be(fbs);
    }

    [Theory]
    [InlineData(79)]
    [InlineData(0)]
    [InlineData(601)]
    [InlineData(1000)]
    public void Make_WithInvalidCholesterol_ShouldThrowInvalidPhysiologicalValueException(int invalidChol)
    {
        var act = () => CholesterolReading.Make(invalidChol, false);

        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*SerumCholesterolMgDl*violates physiological boundaries*");
    }

    [Theory]
    [InlineData(200, 0, false)]
    [InlineData(200, 1, true)]
    public void Make_WithIntegerFbsFlag_ShouldMapCorrectly(int chol, int fbsFlag, bool expectedFbs)
    {
        var reading = CholesterolReading.Make(chol, fbsFlag);

        reading.FastingBloodSugarAbove120.Should().Be(expectedFbs);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void Make_WithInvalidFbsFlag_ShouldThrow(int invalidFlag)
    {
        var act = () => CholesterolReading.Make(200, invalidFlag);

        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*FastingBloodSugarFlag*");
    }

    [Theory]
    [InlineData(175, "Desirable", true, false, false, false)]
    [InlineData(215, "Borderline High", false, true, false, false)]
    [InlineData(250, "High Risk", false, false, true, false)]
    [InlineData(450, "Severe Hypercholesterolemia", false, false, true, true)]
    public void GetRiskCategory_ShouldClassifyCorrectly(
        int cholesterol,
        string expectedCategory,
        bool isDesirable,
        bool isBorderline,
        bool isHigh,
        bool isSevere)
    {
        var reading = CholesterolReading.Make(cholesterol, false);

        reading.GetRiskCategory().Should().Be(expectedCategory);
        reading.IsDesirable.Should().Be(isDesirable);
        reading.IsBorderlineHigh.Should().Be(isBorderline);
        reading.IsHigh.Should().Be(isHigh);
        reading.IsSevereHypercholesterolemia.Should().Be(isSevere);
    }

    [Fact]
    public void ValueObject_Equality_ShouldBehaveCorrectly()
    {
        var r1 = CholesterolReading.Make(220, true);
        var r2 = CholesterolReading.Make(220, true);
        var r3 = CholesterolReading.Make(220, false);

        r1.Should().Be(r2);
        (r1 == r2).Should().BeTrue();
        (r1 != r3).Should().BeTrue();
        r1.GetHashCode().Should().Be(r2.GetHashCode());
    }
}
