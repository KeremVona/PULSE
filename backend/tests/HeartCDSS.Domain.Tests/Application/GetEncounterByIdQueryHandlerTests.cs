using FluentAssertions;
using HeartCDSS.Application.Encounters.Queries.GetEncounterById;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.ValueObjects;
using HeartCDSS.Infrastructure.Persistence;
using HeartCDSS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HeartCDSS.Domain.Tests.Application;

public class GetEncounterByIdQueryHandlerTests
{
    private static HeartCDSSDbContext MakeInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HeartCDSSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new HeartCDSSDbContext(options);
    }

    private static (Patient Patient, ClinicalEncounter Encounter) MakeSeededEncounter(string dbName)
    {
        var madePatient = Patient.Make(
            mrn: "MRN-QUERY-01",
            firstName: "Ellen",
            lastName: "Ripley",
            dateOfBirth: new DateOnly(1979, 1, 7),
            biologicalSex: BiologicalSex.Female
        );

        var madeEncounter = ClinicalEncounter.Make(
            patientId: madePatient.Id,
            clinicianId: Guid.NewGuid(),
            encounterNumber: "ENC-QUERY-001",
            encounterType: EncounterType.CardiologyFollowUp,
            chiefComplaint: "Mild dyspnea upon exertion"
        );

        var madeIndicators = ClinicalIndicators.Make(
            encounterId: madeEncounter.Id,
            ageAtEncounter: 45,
            sex: 0,
            chestPainType: ChestPainType.AtypicalAngina,
            bloodPressure: BloodPressure.Make(128, 82),
            cholesterol: CholesterolReading.Make(210, false),
            ecg: EcgResult.Make(RestingEcgType.Normal, 145, false, 0.8m, StSlope.Upsloping),
            fluoroscopyVessels: 0,
            thalassemia: ThalassemiaType.Normal
        );

        var madeAssessment = RiskAssessmentResult.Make(
            encounterId: madeEncounter.Id,
            modelId: Guid.NewGuid(),
            riskProbability: 0.1850m,
            riskBand: RiskBand.Low,
            confidenceInterval: ConfidenceInterval.Make(0.12m, 0.25m, 0.95m),
            decisionThreshold: 0.2900m,
            baseExpectedValue: 0.50m,
            cdssAdvisoryText: "Low risk profile screening.",
            recommendedAction: "Routine annual cardiology checkup."
        );

        madeAssessment.AddAttribution(ShapAttribution.Make(
            riskAssessmentResultId: madeAssessment.Id,
            featureKey: "chol",
            featureDisplayName: "Serum Cholesterol",
            featureValueDisplay: "210 mg/dL",
            attributionValue: 0.045m,
            impactDirection: ImpactDirection.RiskIncreasing,
            rankOrder: 1
        ));

        madeEncounter.AttachClinicalIndicators(madeIndicators);
        madeEncounter.AddRiskAssessment(madeAssessment);
        madePatient.AddEncounter(madeEncounter);

        return (madePatient, madeEncounter);
    }

    [Fact]
    public async Task Handle_WhenEncounterExistsWithDetails_ReturnsEncounterDetailDto()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var (madePatient, madeEncounter) = MakeSeededEncounter(dbName);

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(madePatient);
            await seedContext.Encounters.AddAsync(madeEncounter);
            await seedContext.SaveChangesAsync();
        }

        // Act
        EncounterDetailDto? madeResult;
        await using (var actContext = MakeInMemoryDbContext(dbName))
        {
            var repo = new ClinicalEncounterRepository(actContext);
            var madeHandler = new GetEncounterByIdQueryHandler(repo);

            var madeQuery = new GetEncounterByIdQuery(madePatient.Id, madeEncounter.Id);
            madeResult = await madeHandler.Handle(madeQuery, CancellationToken.None);
        }

        // Assert
        madeResult.Should().NotBeNull();
        madeResult!.EncounterId.Should().Be(madeEncounter.Id);
        madeResult.PatientId.Should().Be(madePatient.Id);
        madeResult.EncounterNumber.Should().Be("ENC-QUERY-001");
        madeResult.Indicators.Should().NotBeNull();
        madeResult.Indicators!.Age.Should().Be(45);
        madeResult.Indicators.RestingBp.Should().Be(128);
        madeResult.RiskAssessments.Should().ContainSingle();

        var assessment = madeResult.RiskAssessments.First();
        assessment.RiskProbability.Should().Be(0.1850m);
        assessment.RiskBand.Should().Be("Low");
        assessment.Attributions.Should().ContainSingle();
        assessment.Attributions.First().FeatureKey.Should().Be("chol");
    }

    [Fact]
    public async Task Handle_WhenEncounterDoesNotExist_ReturnsNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var actContext = MakeInMemoryDbContext(dbName);
        var repo = new ClinicalEncounterRepository(actContext);
        var madeHandler = new GetEncounterByIdQueryHandler(repo);

        var madeQuery = new GetEncounterByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var madeResult = await madeHandler.Handle(madeQuery, CancellationToken.None);

        // Assert
        madeResult.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPatientIdDoesNotMatch_ReturnsNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var (madePatient, madeEncounter) = MakeSeededEncounter(dbName);

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(madePatient);
            await seedContext.Encounters.AddAsync(madeEncounter);
            await seedContext.SaveChangesAsync();
        }

        await using var actContext = MakeInMemoryDbContext(dbName);
        var repo = new ClinicalEncounterRepository(actContext);
        var madeHandler = new GetEncounterByIdQueryHandler(repo);

        // Query with correct encounter ID but wrong patient ID
        var wrongPatientId = Guid.NewGuid();
        var madeQuery = new GetEncounterByIdQuery(wrongPatientId, madeEncounter.Id);

        // Act
        var madeResult = await madeHandler.Handle(madeQuery, CancellationToken.None);

        // Assert
        madeResult.Should().BeNull();
    }
}
