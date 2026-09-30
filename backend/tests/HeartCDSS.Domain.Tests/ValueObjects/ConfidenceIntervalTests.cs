using FluentAssertions;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.ValueObjects;

public class ConfidenceIntervalTests
{
    [Fact]
    public void Make_WithValidBounds_ShouldCalculateMargin()
    {
        var ci = ConfidenceInterval.Make(0.6200m, 0.7400m, 0.95m);

        ci.LowerBound.Should().Be(0.6200m);
        ci.UpperBound.Should().Be(0.7400m);
        ci.ConfidenceLevel.Should().Be(0.95m);
        ci.MarginPercentage.Should().Be(6.0m);
    }

    [Fact]
    public void Make_WithLowerGreaterThanUpper_ShouldThrow()
    {
        var act = () => ConfidenceInterval.Make(0.80m, 0.60m);

        act.Should().Throw<DomainException>()
            .WithMessage("*cannot exceed upper bound*");
    }

    [Theory]
    [InlineData(-0.1, 0.5)]
    [InlineData(0.2, 1.1)]
    public void Make_WithOutOfBounds_ShouldThrow(decimal lower, decimal upper)
    {
        var act = () => ConfidenceInterval.Make(lower, upper);

        act.Should().Throw<InvalidPhysiologicalValueException>();
    }

    [Fact]
    public void ValueObject_Equality_ShouldBehaveCorrectly()
    {
        var c1 = ConfidenceInterval.Make(0.40m, 0.60m);
        var c2 = ConfidenceInterval.Make(0.40m, 0.60m);
        var c3 = ConfidenceInterval.Make(0.41m, 0.60m);

        c1.Should().Be(c2);
        (c1 == c2).Should().BeTrue();
        (c1 != c3).Should().BeTrue();
    }
}
