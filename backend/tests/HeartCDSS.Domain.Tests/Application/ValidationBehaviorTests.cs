using FluentAssertions;
using FluentValidation;
using HeartCDSS.Application.Common.Behaviors;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Domain.Enums;
using MediatR;
using Xunit;

namespace HeartCDSS.Domain.Tests.Application;

public class ValidationBehaviorTests
{
    private static ClinicalIndicatorsInputDto MakeValidIndicators()
    {
        return new ClinicalIndicatorsInputDto(
            Age: 52,
            Sex: 1,
            ChestPainType: 1,
            RestingBp: 125,
            DiastolicBp: 80,
            SerumCholesterol: 210,
            FastingBloodSugar: 0,
            RestingEcg: 0,
            MaxHeartRate: 160,
            ExerciseAngina: 0,
            OldpeakStDepression: 1.0m,
            Slope: 1,
            FluoroscopyVessels: 0,
            Thalassemia: 3
        );
    }

    private static MakeEncounterCommand MakeValidCommand()
    {
        return new MakeEncounterCommand(
            PatientId: Guid.NewGuid(),
            ClinicianId: Guid.NewGuid(),
            EncounterNumber: "ENC-PIPE-001",
            EncounterType: EncounterType.CardiologyFollowUp,
            ChiefComplaint: "Routine cardiology follow-up",
            Indicators: MakeValidIndicators()
        );
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNextDelegate()
    {
        // Arrange
        var validators = new List<IValidator<MakeEncounterCommand>>
        {
            new MakeEncounterCommandValidator()
        };

        var behavior = new ValidationBehavior<MakeEncounterCommand, MakeEncounterResultDto>(validators);
        var validCommand = MakeValidCommand();
        var expectedResult = new MakeEncounterResultDto(
            EncounterId: Guid.NewGuid(),
            PatientId: validCommand.PatientId,
            ClinicianId: validCommand.ClinicianId,
            EncounterNumber: validCommand.EncounterNumber,
            EncounterType: validCommand.EncounterType,
            Status: EncounterStatus.InProgress,
            ChiefComplaint: validCommand.ChiefComplaint,
            EncounterDate: DateTimeOffset.UtcNow,
            ClinicalAlerts: []
        );

        RequestHandlerDelegate<MakeEncounterResultDto> nextDelegate = ct => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(validCommand, nextDelegate, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowAppValidationException_WithoutCallingNext()
    {
        // Arrange
        var validators = new List<IValidator<MakeEncounterCommand>>
        {
            new MakeEncounterCommandValidator()
        };

        var behavior = new ValidationBehavior<MakeEncounterCommand, MakeEncounterResultDto>(validators);

        // Invalid: Age is 0, RestingBp is 300, Cholesterol is 50
        var invalidIndicators = MakeValidIndicators() with
        {
            Age = 0,
            RestingBp = 300,
            SerumCholesterol = 50
        };

        var invalidCommand = MakeValidCommand() with { Indicators = invalidIndicators };

        var delegateCalled = false;
        RequestHandlerDelegate<MakeEncounterResultDto> nextDelegate = ct =>
        {
            delegateCalled = true;
            return Task.FromResult<MakeEncounterResultDto>(null!);
        };

        // Act
        var act = () => behavior.Handle(invalidCommand, nextDelegate, CancellationToken.None);

        // Assert
        var exceptionAssertion = await act.Should().ThrowAsync<AppValidationException>();
        var exception = exceptionAssertion.Which;

        delegateCalled.Should().BeFalse();
        exception.Errors.Should().ContainKey("Indicators.Age");
        exception.Errors.Should().ContainKey("Indicators.RestingBp");
        exception.Errors.Should().ContainKey("Indicators.SerumCholesterol");
    }

    [Fact]
    public async Task Handle_WithoutValidators_ShouldCallNextDirectly()
    {
        // Arrange
        var emptyValidators = Enumerable.Empty<IValidator<MakeEncounterCommand>>();
        var behavior = new ValidationBehavior<MakeEncounterCommand, MakeEncounterResultDto>(emptyValidators);
        var command = MakeValidCommand();
        var expectedResult = new MakeEncounterResultDto(
            EncounterId: Guid.NewGuid(),
            PatientId: command.PatientId,
            ClinicianId: command.ClinicianId,
            EncounterNumber: command.EncounterNumber,
            EncounterType: command.EncounterType,
            Status: EncounterStatus.InProgress,
            ChiefComplaint: null,
            EncounterDate: DateTimeOffset.UtcNow,
            ClinicalAlerts: []
        );

        RequestHandlerDelegate<MakeEncounterResultDto> nextDelegate = ct => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, nextDelegate, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }
}
