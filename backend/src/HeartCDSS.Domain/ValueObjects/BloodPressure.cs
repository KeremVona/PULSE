using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.ValueObjects;

/// <summary>
/// Value Object encapsulating resting blood pressure measurements and AHA clinical tier classifications.
/// </summary>
public sealed class BloodPressure : ValueObject
{
    public const int MinSystolic = 60;
    public const int MaxSystolic = 260;
    public const int MinDiastolic = 40;
    public const int MaxDiastolic = 140;

    public int Systolic { get; }
    public int? Diastolic { get; }

    private BloodPressure() { }

    private BloodPressure(int systolic, int? diastolic)
    {
        Systolic = systolic;
        Diastolic = diastolic;
    }


    /// <summary>
    /// Makes and validates a resting blood pressure value object.
    /// </summary>
    /// <param name="systolic">Resting blood pressure in mmHg (60 - 260 mmHg).</param>
    /// <param name="diastolic">Optional diastolic pressure in mmHg (40 - 140 mmHg).</param>
    /// <returns>A validated BloodPressure value object.</returns>
    public static BloodPressure Make(int systolic, int? diastolic = null)
    {
        if (systolic < MinSystolic || systolic > MaxSystolic)
        {
            throw new InvalidPhysiologicalValueException(
                nameof(Systolic),
                systolic,
                $"{MinSystolic} - {MaxSystolic} mmHg"
            );
        }

        if (diastolic.HasValue && (diastolic.Value < MinDiastolic || diastolic.Value > MaxDiastolic))
        {
            throw new InvalidPhysiologicalValueException(
                nameof(Diastolic),
                diastolic.Value,
                $"{MinDiastolic} - {MaxDiastolic} mmHg"
            );
        }

        return new BloodPressure(systolic, diastolic);
    }

    public bool IsHypertensive => Systolic >= 130;
    public bool IsHypertensiveCrisis => Systolic >= 180;
    public bool IsHypotensive => Systolic < 90;
    public bool IsSevereHypotension => Systolic < 70;

    /// <summary>
    /// Returns the AHA (American Heart Association) clinical classification tier.
    /// </summary>
    public string GetAhaCategory()
    {
        if (Systolic >= 180) return "Hypertensive Crisis";
        if (Systolic >= 140) return "Stage 2 Hypertension";
        if (Systolic >= 130) return "Stage 1 Hypertension";
        if (Systolic >= 120) return "Elevated";
        return "Normal";
    }

    public override string ToString()
    {
        return Diastolic.HasValue ? $"{Systolic}/{Diastolic} mmHg" : $"{Systolic} mmHg";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Systolic;
        yield return Diastolic;
    }
}
