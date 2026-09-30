using FluentAssertions;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Infrastructure.Persistence;
using HeartCDSS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HeartCDSS.Domain.Tests.Application;

public class MakeEncounterCommandHandlerTests
{
    private static HeartCDSSDbContext MakeInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HeartCDSSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new HeartCDSSDbContext(options);
    }

    private static ClinicalIndicatorsInputDto MakeIndicatorsDto(
        int restingBp = 135,
        int serumCholesterol = 240,
        int maxHeartRate = 160,
        decimal oldpeak = 1.8m)
    {
        return new ClinicalIndicatorsInputDto(
            Age: 56,
            Sex: 1,
            ChestPainType: 2,
            RestingBp: restingBp,
            DiastolicBp: 88,
            SerumCholesterol: serumCholesterol,
            FastingBloodSugar: 0,
            RestingEcg: 1,
            MaxHeartRate: maxHeartRate,
            ExerciseAngina: 0,
            OldpeakStDepression: oldpeak,
            Slope: 2,
            FluoroscopyVessels: 1,
            Thalassemia: 3
        );
    }

    private static MakeEncounterCommand MakeCommand(Guid patientId, string encounterNumber = "ENC-HANDLER-001", ClinicalIndicatorsInputDto? indicators = null)
    {
        return new MakeEncounterCommand(
            PatientId: patientId,
            ClinicianId: Guid.NewGuid(),
            EncounterNumber: encounterNumber,
            EncounterType: EncounterType.CardiologyFollowUp,
            ChiefComplaint: "Patient complains of chest discomfort under stress",
            Indicators: indicators ?? MakeIndicatorsDto()
        );
    }

    [Fact]
    public async Task Handle_WhenPatientExists_ShouldMakeEncounterAndPersistIndicators()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var patient = Patient.Make(
            mrn: "MRN-HANDLER-01",
            firstName: "Alex",
            lastName: "Vance",
            dateOfBirth: new DateOnly(1978, 4, 15),
            biologicalSex: BiologicalSex.Male
        );

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(patient);
            await seedContext.SaveChangesAsync();
        }

        // Act
        MakeEncounterResultDto result;
        await using (var actContext = MakeInMemoryDbContext(dbName))
        {
            var patientRepo = new PatientRepository(actContext);
            var encounterRepo = new ClinicalEncounterRepository(actContext);
            var handler = new MakeEncounterCommandHandler(patientRepo, encounterRepo, actContext);

            var command = MakeCommand(patient.Id);
            result = await handler.Handle(command, CancellationToken.None);
        }

        // Assert
        result.Should().NotBeNull();
        result.PatientId.Should().Be(patient.Id);
        result.EncounterNumber.Should().Be("ENC-HANDLER-001");
        result.Status.Should().Be(EncounterStatus.InProgress);

        await using (var verifyContext = MakeInMemoryDbContext(dbName))
        {
            var persistedEncounter = await verifyContext.Encounters
                .Include(e => e.Indicators)
                .FirstOrDefaultAsync(e => e.Id == result.EncounterId);

            persistedEncounter.Should().NotBeNull();
            persistedEncounter!.EncounterNumber.Should().Be("ENC-HANDLER-001");
            persistedEncounter.Indicators.Should().NotBeNull();
            persistedEncounter.Indicators!.AgeAtEncounter.Should().Be(56);
            persistedEncounter.Indicators.BloodPressure.Systolic.Should().Be(135);
            persistedEncounter.Indicators.Cholesterol.SerumCholesterolMgDl.Should().Be(240);
        }
    }

    [Fact]
    public async Task Handle_WhenPatientDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var nonExistentPatientId = Guid.NewGuid();

        await using var context = MakeInMemoryDbContext(dbName);
        var patientRepo = new PatientRepository(context);
        var encounterRepo = new ClinicalEncounterRepository(context);
        var handler = new MakeEncounterCommandHandler(patientRepo, encounterRepo, context);

        var command = MakeCommand(nonExistentPatientId);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{nonExistentPatientId}*");
    }

    [Fact]
    public async Task Handle_WhenEncounterNumberAlreadyExists_ShouldThrowDomainException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var patient = Patient.Make(
            mrn: "MRN-HANDLER-02",
            firstName: "Gordon",
            lastName: "Freeman",
            dateOfBirth: new DateOnly(1975, 8, 20),
            biologicalSex: BiologicalSex.Male
        );

        var existingEncounter = ClinicalEncounter.Make(
            patientId: patient.Id,
            clinicianId: Guid.NewGuid(),
            encounterNumber: "ENC-DUPLICATE",
            encounterType: EncounterType.EmergencyTriage
        );

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(patient);
            await seedContext.Encounters.AddAsync(existingEncounter);
            await seedContext.SaveChangesAsync();
        }

        // Act
        await using var actContext = MakeInMemoryDbContext(dbName);
        var patientRepo = new PatientRepository(actContext);
        var encounterRepo = new ClinicalEncounterRepository(actContext);
        var handler = new MakeEncounterCommandHandler(patientRepo, encounterRepo, actContext);

        var duplicateCommand = MakeCommand(patient.Id, encounterNumber: "ENC-DUPLICATE");

        var act = () => handler.Handle(duplicateCommand, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_WhenPhysiologicalAlertsTrigger_ShouldIncludeInResultDto()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var patient = Patient.Make(
            mrn: "MRN-HANDLER-03",
            firstName: "Eli",
            lastName: "Vance",
            dateOfBirth: new DateOnly(1952, 1, 1),
            biologicalSex: BiologicalSex.Male
        );

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(patient);
            await seedContext.SaveChangesAsync();
        }

        // Trigger critical alerts: RestingBp 210 mmHg (> 200), Cholesterol 450 (> 400), MaxHeartRate 215 (> 205)
        var highRiskIndicators = MakeIndicatorsDto(
            restingBp: 210,
            serumCholesterol: 450,
            maxHeartRate: 215,
            oldpeak: 4.2m
        );

        // Act
        MakeEncounterResultDto result;
        await using (var actContext = MakeInMemoryDbContext(dbName))
        {
            var patientRepo = new PatientRepository(actContext);
            var encounterRepo = new ClinicalEncounterRepository(actContext);
            var handler = new MakeEncounterCommandHandler(patientRepo, encounterRepo, actContext);

            var command = MakeCommand(patient.Id, encounterNumber: "ENC-ALERTS", highRiskIndicators);
            result = await handler.Handle(command, CancellationToken.None);
        }

        // Assert
        result.ClinicalAlerts.Should().NotBeEmpty();
        result.ClinicalAlerts.Should().Contain(a => a.Contains("Critical Hypertension"));
        result.ClinicalAlerts.Should().Contain(a => a.Contains("Severe Hypercholesterolemia"));
        result.ClinicalAlerts.Should().Contain(a => a.Contains("Extreme Tachycardia"));
        result.ClinicalAlerts.Should().Contain(a => a.Contains("Severe ST Depression"));
    }
}
