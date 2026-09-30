using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.ValueObjects;

/// <summary>
/// Value Object encapsulating 95% Confidence Interval bounds around calibrated posterior probability.
/// </summary>
public sealed class ConfidenceInterval : ValueObject
{
    public decimal LowerBound { get; }
    public decimal UpperBound { get; }
    public decimal ConfidenceLevel { get; }
    public decimal MarginPercentage { get; }

    private ConfidenceInterval() { }

    private ConfidenceInterval(decimal lowerBound, decimal upperBound, decimal confidenceLevel, decimal marginPercentage)
    {
        LowerBound = decimal.Round(lowerBound, 4);
        UpperBound = decimal.Round(upperBound, 4);
        ConfidenceLevel = confidenceLevel;
        MarginPercentage = decimal.Round(marginPercentage, 2);
    }

    /// <summary>
    /// Makes and validates a statistical Confidence Interval.
    /// </summary>
    public static ConfidenceInterval Make(decimal lowerBound, decimal upperBound, decimal confidenceLevel = 0.95m)
    {
        if (lowerBound < 0.0m || lowerBound > 1.0m)
        {
            throw new InvalidPhysiologicalValueException(nameof(LowerBound), lowerBound, "0.0000 - 1.0000");
        }

        if (upperBound < 0.0m || upperBound > 1.0m)
        {
            throw new InvalidPhysiologicalValueException(nameof(UpperBound), upperBound, "0.0000 - 1.0000");
        }

        if (lowerBound > upperBound)
        {
            throw new DomainException($"Confidence interval lower bound ({lowerBound}) cannot exceed upper bound ({upperBound}).");
        }

        if (confidenceLevel <= 0.0m || confidenceLevel >= 1.0m)
        {
            throw new DomainException($"Confidence level ({confidenceLevel}) must be strictly between 0.0 and 1.0.");
        }

        var marginPercentage = ((upperBound - lowerBound) / 2.0m) * 100.0m;

        return new ConfidenceInterval(lowerBound, upperBound, confidenceLevel, marginPercentage);
    }

    public override string ToString()
    {
        return $"[{LowerBound:F4}, {UpperBound:F4}] ({ConfidenceLevel:P0} CI, +/-{MarginPercentage:F1}%)";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LowerBound;
        yield return UpperBound;
        yield return ConfidenceLevel;
    }
}
