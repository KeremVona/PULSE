using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Domain entity capturing the 14 standardized UCI Heart Disease physiological indicators and laboratory markers.
/// Integrates specialized Value Objects: BloodPressure, CholesterolReading, and EcgResult.
/// </summary>
public sealed class ClinicalIndicators : Entity<Guid>
{
    public Guid EncounterId { get; private set; }
    public int AgeAtEncounter { get; private set; }
    public int Sex { get; private set; }
    public ChestPainType ChestPainType { get; private set; }
    public BloodPressure BloodPressure { get; private set; } = null!;
    public CholesterolReading Cholesterol { get; private set; } = null!;
    public EcgResult Ecg { get; private set; } = null!;
    public int FluoroscopyVessels { get; private set; }
    public ThalassemiaType Thalassemia { get; private set; }

    private ClinicalIndicators() { }

    private ClinicalIndicators(
        Guid id,
        Guid encounterId,
        int ageAtEncounter,
        int sex,
        ChestPainType chestPainType,
        BloodPressure bloodPressure,
        CholesterolReading cholesterol,
        EcgResult ecg,
        int fluoroscopyVessels,
        ThalassemiaType thalassemia)
        : base(id)
    {
        EncounterId = encounterId;
        AgeAtEncounter = ageAtEncounter;
        Sex = sex;
        ChestPainType = chestPainType;
        BloodPressure = bloodPressure;
        Cholesterol = cholesterol;
        Ecg = ecg;
        FluoroscopyVessels = fluoroscopyVessels;
        Thalassemia = thalassemia;
    }

    /// <summary>
    /// Factory method instantiating validated physiological clinical indicators.
    /// </summary>
    public static ClinicalIndicators Make(
        Guid encounterId,
        int ageAtEncounter,
        int sex,
        ChestPainType chestPainType,
        BloodPressure bloodPressure,
        CholesterolReading cholesterol,
        EcgResult ecg,
        int fluoroscopyVessels,
        ThalassemiaType thalassemia,
        Guid? id = null)
    {
        if (encounterId == Guid.Empty)
            throw new DomainException("Encounter ID cannot be empty.");

        if (ageAtEncounter < 1 || ageAtEncounter > 110)
        {
            throw new InvalidPhysiologicalValueException(nameof(AgeAtEncounter), ageAtEncounter, "1 - 110 years");
        }

        if (sex != 0 && sex != 1)
        {
            throw new InvalidPhysiologicalValueException(nameof(Sex), sex, "0 (Female) or 1 (Male)");
        }

        if (fluoroscopyVessels < 0 || fluoroscopyVessels > 3)
        {
            throw new InvalidPhysiologicalValueException(nameof(FluoroscopyVessels), fluoroscopyVessels, "0 - 3 major vessels");
        }

        ArgumentNullException.ThrowIfNull(bloodPressure);
        ArgumentNullException.ThrowIfNull(cholesterol);
        ArgumentNullException.ThrowIfNull(ecg);

        return new ClinicalIndicators(
            id ?? Guid.NewGuid(),
            encounterId,
            ageAtEncounter,
            sex,
            chestPainType,
            bloodPressure,
            cholesterol,
            ecg,
            fluoroscopyVessels,
            thalassemia
        );
    }

    /// <summary>
    /// Evaluates physiological thresholds and returns actionable non-blocking clinical alerts.
    /// </summary>
    public IReadOnlyList<string> GetClinicalWarnings()
    {
        var warnings = new List<string>();

        if (BloodPressure.Systolic > 200)
            warnings.Add($"Critical Hypertension: Resting BP ({BloodPressure.Systolic} mmHg) > 200 mmHg.");
        else if (BloodPressure.Systolic < 70)
            warnings.Add($"Severe Hypotension: Resting BP ({BloodPressure.Systolic} mmHg) < 70 mmHg.");

        if (Cholesterol.IsSevereHypercholesterolemia)
            warnings.Add($"Severe Hypercholesterolemia: Serum Cholesterol ({Cholesterol.SerumCholesterolMgDl} mg/dL) > 400 mg/dL.");

        if (Ecg.IsBradycardic)
            warnings.Add($"Critical Bradycardia: Max HR achieved ({Ecg.MaxHeartRateBpm} bpm) < 50 bpm.");
        else if (Ecg.IsTachycardic)
            warnings.Add($"Extreme Tachycardia: Max HR achieved ({Ecg.MaxHeartRateBpm} bpm) > 205 bpm.");

        if (Ecg.IsSevereStDepression)
            warnings.Add($"Severe ST Depression: oldpeak ({Ecg.OldpeakStDepression:F1} mm) > 3.5 mm indicates substantial ischemia.");

        if (FluoroscopyVessels >= 2)
            warnings.Add($"Multi-Vessel Calcification: {FluoroscopyVessels} major coronary vessels fluoroscopy-occluded.");

        if (Cholesterol.FastingBloodSugarAbove120)
            warnings.Add("Diabetic Risk Factor: Fasting Blood Sugar >= 120 mg/dL.");

        if (AgeAtEncounter < 18 || AgeAtEncounter > 95)
            warnings.Add($"Age Alert: Patient age ({AgeAtEncounter} yrs) operates outside typical clinical cohort range.");

        return warnings;
    }
}
