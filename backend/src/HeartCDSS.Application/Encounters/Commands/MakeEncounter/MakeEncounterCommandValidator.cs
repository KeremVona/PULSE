using FluentValidation;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.ValueObjects;

namespace HeartCDSS.Application.Encounters.Commands.MakeEncounter;

/// <summary>
/// FluentValidation validator enforcing clinical plausibility and physiological boundaries on encounter intake commands.
/// </summary>
public sealed class MakeEncounterCommandValidator : AbstractValidator<MakeEncounterCommand>
{
    public MakeEncounterCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.ClinicianId)
            .NotEmpty().WithMessage("Clinician ID is required.");

        RuleFor(x => x.EncounterNumber)
            .NotEmpty().WithMessage("Encounter number is required.")
            .MaximumLength(64).WithMessage("Encounter number cannot exceed 64 characters.");

        RuleFor(x => x.EncounterType)
            .IsInEnum().WithMessage("Invalid encounter type specified.");

        RuleFor(x => x.ChiefComplaint)
            .MaximumLength(1000).WithMessage("Chief complaint cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

        RuleFor(x => x.Indicators)
            .NotNull().WithMessage("Clinical indicators are required.")
            .SetValidator(new ClinicalIndicatorsInputDtoValidator()!);
    }
}

/// <summary>
/// Validator for physiological parameters strictly aligned with domain limits and UCI dataset clinical norms.
/// </summary>
public sealed class ClinicalIndicatorsInputDtoValidator : AbstractValidator<ClinicalIndicatorsInputDto>
{
    public ClinicalIndicatorsInputDtoValidator()
    {
        RuleFor(x => x.Age)
            .InclusiveBetween(1, 110)
            .WithMessage("Age must be between 1 and 110 years.");

        RuleFor(x => x.Sex)
            .Must(x => x is 0 or 1)
            .WithMessage("Sex must be 0 (Female) or 1 (Male).");

        RuleFor(x => x.ChestPainType)
            .InclusiveBetween(1, 4)
            .WithMessage("Chest pain type must be between 1 (Typical Angina) and 4 (Asymptomatic).");

        RuleFor(x => x.RestingBp)
            .InclusiveBetween(BloodPressure.MinSystolic, BloodPressure.MaxSystolic)
            .WithMessage($"Resting systolic blood pressure must be between {BloodPressure.MinSystolic} and {BloodPressure.MaxSystolic} mmHg.");

        RuleFor(x => x.DiastolicBp)
            .InclusiveBetween(BloodPressure.MinDiastolic, BloodPressure.MaxDiastolic)
            .When(x => x.DiastolicBp.HasValue)
            .WithMessage($"Diastolic blood pressure must be between {BloodPressure.MinDiastolic} and {BloodPressure.MaxDiastolic} mmHg.");

        RuleFor(x => x.SerumCholesterol)
            .InclusiveBetween(CholesterolReading.MinCholesterol, CholesterolReading.MaxCholesterol)
            .WithMessage($"Serum cholesterol must be between {CholesterolReading.MinCholesterol} and {CholesterolReading.MaxCholesterol} mg/dL.");

        RuleFor(x => x.FastingBloodSugar)
            .Must(x => x is 0 or 1)
            .WithMessage("Fasting blood sugar flag must be 0 (<= 120 mg/dL) or 1 (> 120 mg/dL).");

        RuleFor(x => x.RestingEcg)
            .InclusiveBetween(0, 2)
            .WithMessage("Resting ECG finding must be 0 (Normal), 1 (ST-T abnormality), or 2 (LV hypertrophy).");

        RuleFor(x => x.MaxHeartRate)
            .InclusiveBetween(EcgResult.MinHeartRate, EcgResult.MaxHeartRate)
            .WithMessage($"Max heart rate must be between {EcgResult.MinHeartRate} and {EcgResult.MaxHeartRate} bpm.");

        RuleFor(x => x.ExerciseAngina)
            .Must(x => x is 0 or 1)
            .WithMessage("Exercise-induced angina flag must be 0 (Absent) or 1 (Present).");

        RuleFor(x => x.OldpeakStDepression)
            .InclusiveBetween(EcgResult.MinOldpeak, EcgResult.MaxOldpeak)
            .WithMessage($"ST depression (oldpeak) must be between {EcgResult.MinOldpeak:F1} and {EcgResult.MaxOldpeak:F1} mm.");

        RuleFor(x => x.Slope)
            .InclusiveBetween(1, 3)
            .WithMessage("ST segment slope must be 1 (Upsloping), 2 (Flat), or 3 (Downsloping).");

        RuleFor(x => x.FluoroscopyVessels)
            .InclusiveBetween(0, 3)
            .WithMessage("Fluoroscopy-colored major vessels must be between 0 and 3.");

        RuleFor(x => x.Thalassemia)
            .Must(x => x is 3 or 6 or 7)
            .WithMessage("Thalassemia defect code must be 3 (Normal), 6 (Fixed defect), or 7 (Reversible defect).");
    }
}
