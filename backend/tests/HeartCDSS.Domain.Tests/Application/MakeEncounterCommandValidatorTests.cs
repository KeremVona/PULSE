using FluentAssertions;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Domain.Enums;
using Xunit;

namespace HeartCDSS.Domain.Tests.Application;

public class MakeEncounterCommandValidatorTests
{
    private readonly MakeEncounterCommandValidator _validator = new();

    private static ClinicalIndicatorsInputDto MakeValidIndicators()
    {
        return new ClinicalIndicatorsInputDto(
            Age: 55,
            Sex: 1,
            ChestPainType: 2,
            RestingBp: 130,
            DiastolicBp: 85,
            SerumCholesterol: 240,
            FastingBloodSugar: 0,
            RestingEcg: 0,
            MaxHeartRate: 155,
            ExerciseAngina: 0,
            OldpeakStDepression: 1.5m,
            Slope: 1,
            FluoroscopyVessels: 0,
            Thalassemia: 3
        );
    }

    private static MakeEncounterCommand MakeValidCommand(ClinicalIndicatorsInputDto? indicators = null)
    {
        return new MakeEncounterCommand(
            PatientId: Guid.NewGuid(),
            ClinicianId: Guid.NewGuid(),
            EncounterNumber: "ENC-VALID-001",
            EncounterType: EncounterType.CardiologyFollowUp,
            ChiefComplaint: "Routine cardiovascular checkup",
            Indicators: indicators ?? MakeValidIndicators()
        );
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var validCommand = MakeValidCommand();

        var validationResult = await _validator.ValidateAsync(validCommand);

        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithEmptyPatientId_ShouldFail()
    {
        var invalidCommand = MakeValidCommand() with { PatientId = Guid.Empty };

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(e => e.PropertyName == nameof(MakeEncounterCommand.PatientId));
    }

    [Fact]
    public async Task Validate_WithEmptyEncounterNumber_ShouldFail()
    {
        var invalidCommand = MakeValidCommand() with { EncounterNumber = "" };

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(MakeEncounterCommand.EncounterNumber));
    }

    [Fact]
    public async Task Validate_WithExcessiveEncounterNumberLength_ShouldFail()
    {
        var invalidCommand = MakeValidCommand() with { EncounterNumber = new string('A', 65) };

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(MakeEncounterCommand.EncounterNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(111)]
    [InlineData(-5)]
    public async Task Validate_WithInvalidAge_ShouldFail(int invalidAge)
    {
        var invalidIndicators = MakeValidIndicators() with { Age = invalidAge };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.Age");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task Validate_WithInvalidSex_ShouldFail(int invalidSex)
    {
        var invalidIndicators = MakeValidIndicators() with { Sex = invalidSex };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.Sex");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task Validate_WithInvalidChestPainType_ShouldFail(int invalidCp)
    {
        var invalidIndicators = MakeValidIndicators() with { ChestPainType = invalidCp };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.ChestPainType");
    }

    [Theory]
    [InlineData(59)]
    [InlineData(261)]
    public async Task Validate_WithInvalidSystolicBp_ShouldFail(int invalidSystolic)
    {
        var invalidIndicators = MakeValidIndicators() with { RestingBp = invalidSystolic };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.RestingBp");
    }

    [Theory]
    [InlineData(39)]
    [InlineData(141)]
    public async Task Validate_WithInvalidDiastolicBp_ShouldFail(int invalidDiastolic)
    {
        var invalidIndicators = MakeValidIndicators() with { DiastolicBp = invalidDiastolic };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.DiastolicBp");
    }

    [Theory]
    [InlineData(79)]
    [InlineData(601)]
    public async Task Validate_WithInvalidCholesterol_ShouldFail(int invalidChol)
    {
        var invalidIndicators = MakeValidIndicators() with { SerumCholesterol = invalidChol };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.SerumCholesterol");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task Validate_WithInvalidFastingBloodSugar_ShouldFail(int invalidFbs)
    {
        var invalidIndicators = MakeValidIndicators() with { FastingBloodSugar = invalidFbs };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.FastingBloodSugar");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task Validate_WithInvalidRestingEcg_ShouldFail(int invalidEcg)
    {
        var invalidIndicators = MakeValidIndicators() with { RestingEcg = invalidEcg };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.RestingEcg");
    }

    [Theory]
    [InlineData(39)]
    [InlineData(221)]
    public async Task Validate_WithInvalidMaxHeartRate_ShouldFail(int invalidHr)
    {
        var invalidIndicators = MakeValidIndicators() with { MaxHeartRate = invalidHr };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.MaxHeartRate");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task Validate_WithInvalidExerciseAngina_ShouldFail(int invalidExang)
    {
        var invalidIndicators = MakeValidIndicators() with { ExerciseAngina = invalidExang };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.ExerciseAngina");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(6.6)]
    public async Task Validate_WithInvalidOldpeak_ShouldFail(decimal invalidOldpeak)
    {
        var invalidIndicators = MakeValidIndicators() with { OldpeakStDepression = invalidOldpeak };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.OldpeakStDepression");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task Validate_WithInvalidSlope_ShouldFail(int invalidSlope)
    {
        var invalidIndicators = MakeValidIndicators() with { Slope = invalidSlope };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.Slope");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task Validate_WithInvalidFluoroscopyVessels_ShouldFail(int invalidVessels)
    {
        var invalidIndicators = MakeValidIndicators() with { FluoroscopyVessels = invalidVessels };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.FluoroscopyVessels");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    public async Task Validate_WithInvalidThalassemia_ShouldFail(int invalidThal)
    {
        var invalidIndicators = MakeValidIndicators() with { Thalassemia = invalidThal };
        var invalidCommand = MakeValidCommand(invalidIndicators);

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Indicators.Thalassemia");
    }

    [Fact]
    public async Task Validate_WithNullIndicators_ShouldFail()
    {
        var invalidCommand = new MakeEncounterCommand(
            PatientId: Guid.NewGuid(),
            ClinicianId: Guid.NewGuid(),
            EncounterNumber: "ENC-NO-IND",
            EncounterType: EncounterType.CardiologyFollowUp,
            ChiefComplaint: null,
            Indicators: null!
        );

        var validationResult = await _validator.ValidateAsync(invalidCommand);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(MakeEncounterCommand.Indicators));
    }
}
