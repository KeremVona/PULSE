using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.ValueObjects;

/// <summary>
/// Value Object encapsulating serum cholesterol and fasting blood sugar laboratory findings.
/// </summary>
public sealed class CholesterolReading : ValueObject
{
    public const int MinCholesterol = 80;
    public const int MaxCholesterol = 600;

    public int SerumCholesterolMgDl { get; }
    public bool FastingBloodSugarAbove120 { get; }

    private CholesterolReading() { }

    private CholesterolReading(int serumCholesterolMgDl, bool fastingBloodSugarAbove120)
    {
        SerumCholesterolMgDl = serumCholesterolMgDl;
        FastingBloodSugarAbove120 = fastingBloodSugarAbove120;
    }

    /// <summary>
    /// Makes and validates a cholesterol and glycemic laboratory indicator value object.
    /// </summary>
    /// <param name="serumCholesterol">Total serum cholesterol in mg/dL (80 - 600 mg/dL).</param>
    /// <param name="fastingBloodSugarAbove120">Whether fasting blood sugar exceeds 120 mg/dL.</param>
    /// <returns>A validated CholesterolReading value object.</returns>
    public static CholesterolReading Make(int serumCholesterol, bool fastingBloodSugarAbove120)
    {
        if (serumCholesterol < MinCholesterol || serumCholesterol > MaxCholesterol)
        {
            throw new InvalidPhysiologicalValueException(
                nameof(SerumCholesterolMgDl),
                serumCholesterol,
                $"{MinCholesterol} - {MaxCholesterol} mg/dL"
            );
        }

        return new CholesterolReading(serumCholesterol, fastingBloodSugarAbove120);
    }

    /// <summary>
    /// Makes a reading accepting binary integer fasting blood sugar flag (0 or 1).
    /// </summary>
    public static CholesterolReading Make(int serumCholesterol, int fbsFlag)
    {
        if (fbsFlag != 0 && fbsFlag != 1)
        {
            throw new InvalidPhysiologicalValueException(
                "FastingBloodSugarFlag",
                fbsFlag,
                "0 (<= 120 mg/dL) or 1 (> 120 mg/dL)"
            );
        }

        return Make(serumCholesterol, fbsFlag == 1);
    }

    public bool IsSevereHypercholesterolemia => SerumCholesterolMgDl > 400;
    public bool IsDesirable => SerumCholesterolMgDl < 200;
    public bool IsBorderlineHigh => SerumCholesterolMgDl >= 200 && SerumCholesterolMgDl < 240;
    public bool IsHigh => SerumCholesterolMgDl >= 240;

    /// <summary>
    /// Returns the NCEP ATP III total cholesterol risk classification.
    /// </summary>
    public string GetRiskCategory()
    {
        if (SerumCholesterolMgDl > 400) return "Severe Hypercholesterolemia";
        if (SerumCholesterolMgDl >= 240) return "High Risk";
        if (SerumCholesterolMgDl >= 200) return "Borderline High";
        return "Desirable";
    }

    public override string ToString()
    {
        var fbsText = FastingBloodSugarAbove120 ? ">120 mg/dL" : "<=120 mg/dL";
        return $"{SerumCholesterolMgDl} mg/dL (FBS: {fbsText})";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SerumCholesterolMgDl;
        yield return FastingBloodSugarAbove120;
    }
}
