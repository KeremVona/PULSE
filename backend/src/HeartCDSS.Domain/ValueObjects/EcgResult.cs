using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.ValueObjects;

/// <summary>
/// Value Object encapsulating electrocardiographic indicators, peak exercise response, and ST-T segment dynamics.
/// </summary>
public sealed class EcgResult : ValueObject
{
    public const int MinHeartRate = 40;
    public const int MaxHeartRate = 220;
    public const decimal MinOldpeak = 0.0m;
    public const decimal MaxOldpeak = 6.5m;

    public RestingEcgType RestingEcg { get; }
    public int MaxHeartRateBpm { get; }
    public bool HasExerciseInducedAngina { get; }
    public decimal OldpeakStDepression { get; }
    public StSlope Slope { get; }

    private EcgResult() { }

    private EcgResult(
        RestingEcgType restingEcg,
        int maxHeartRateBpm,
        bool hasExerciseInducedAngina,
        decimal oldpeakStDepression,
        StSlope slope)
    {
        RestingEcg = restingEcg;
        MaxHeartRateBpm = maxHeartRateBpm;
        HasExerciseInducedAngina = hasExerciseInducedAngina;
        OldpeakStDepression = decimal.Round(oldpeakStDepression, 1);
        Slope = slope;
    }

    /// <summary>
    /// Makes and validates an EcgResult value object.
    /// </summary>
    public static EcgResult Make(
        RestingEcgType restingEcg,
        int maxHeartRateBpm,
        bool hasExerciseInducedAngina,
        decimal oldpeakStDepression,
        StSlope slope)
    {
        if (maxHeartRateBpm < MinHeartRate || maxHeartRateBpm > MaxHeartRate)
        {
            throw new InvalidPhysiologicalValueException(
                nameof(MaxHeartRateBpm),
                maxHeartRateBpm,
                $"{MinHeartRate} - {MaxHeartRate} bpm"
            );
        }

        if (oldpeakStDepression < MinOldpeak || oldpeakStDepression > MaxOldpeak)
        {
            throw new InvalidPhysiologicalValueException(
                nameof(OldpeakStDepression),
                oldpeakStDepression,
                $"{MinOldpeak:F1} - {MaxOldpeak:F1} mm"
            );
        }

        return new EcgResult(
            restingEcg,
            maxHeartRateBpm,
            hasExerciseInducedAngina,
            oldpeakStDepression,
            slope
        );
    }

    /// <summary>
    /// Overload accepting integer representation of exercise angina flag (0 or 1).
    /// </summary>
    public static EcgResult Make(
        int restingEcgCode,
        int maxHeartRateBpm,
        int exangCode,
        decimal oldpeakStDepression,
        int slopeCode)
    {
        if (!Enum.IsDefined(typeof(RestingEcgType), restingEcgCode))
        {
            throw new InvalidPhysiologicalValueException("RestingEcgCode", restingEcgCode, "0 (Normal), 1 (ST-T abnormality), or 2 (LV hypertrophy)");
        }

        if (exangCode != 0 && exangCode != 1)
        {
            throw new InvalidPhysiologicalValueException("ExerciseAnginaCode", exangCode, "0 (Absent) or 1 (Present)");
        }

        if (!Enum.IsDefined(typeof(StSlope), slopeCode))
        {
            throw new InvalidPhysiologicalValueException("SlopeCode", slopeCode, "1 (Upsloping), 2 (Flat), or 3 (Downsloping)");
        }

        return Make(
            (RestingEcgType)restingEcgCode,
            maxHeartRateBpm,
            exangCode == 1,
            oldpeakStDepression,
            (StSlope)slopeCode
        );
    }

    public bool IsBradycardic => MaxHeartRateBpm < 50;
    public bool IsTachycardic => MaxHeartRateBpm > 205;
    public bool IsSevereStDepression => OldpeakStDepression > 3.5m;
    public bool HasHighRiskEcgProfile => HasExerciseInducedAngina || OldpeakStDepression >= 2.0m || Slope == StSlope.Downsloping;

    public override string ToString()
    {
        var exangStr = HasExerciseInducedAngina ? "Angina+" : "Angina-";
        return $"ECG: {RestingEcg}, MaxHR: {MaxHeartRateBpm} bpm, {exangStr}, ST: {OldpeakStDepression} mm ({Slope})";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RestingEcg;
        yield return MaxHeartRateBpm;
        yield return HasExerciseInducedAngina;
        yield return OldpeakStDepression;
        yield return Slope;
    }
}
