using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.ValueObjects;
using HeartCDSS.Infrastructure.Persistence;
using HeartCDSS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HeartCDSS.Domain.Tests.Infrastructure;

public class RepositoryTests
{
    private static HeartCDSSDbContext MakeInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HeartCDSSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new HeartCDSSDbContext(options);
    }

    [Fact]
    public async Task PatientRepository_AddAndQuery_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var patient = Patient.Make(
            mrn: "MRN-1001",
            firstName: "Robert",
            lastName: "Smith",
            dateOfBirth: new DateOnly(1975, 3, 20),
            biologicalSex: BiologicalSex.Male
        );

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new PatientRepository(context);
            await repo.AddAsync(patient);
            await context.SaveChangesAsync();
        }

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new PatientRepository(context);

            var byId = await repo.GetByIdAsync(patient.Id);
            byId.Should().NotBeNull();
            byId!.Mrn.Should().Be("MRN-1001");

            var byMrn = await repo.GetByMrnAsync("MRN-1001");
            byMrn.Should().NotBeNull();
            byMrn!.FirstName.Should().Be("Robert");

            var exists = await repo.ExistsWithMrnAsync("MRN-1001");
            exists.Should().BeTrue();

            var notExists = await repo.ExistsWithMrnAsync("NON-EXISTENT");
            notExists.Should().BeFalse();

            var searchResults = await repo.SearchByNameAsync("Smi", 10);
            searchResults.Should().HaveCount(1);
            searchResults[0].LastName.Should().Be("Smith");
        }
    }

    [Fact]
    public async Task ClinicalEncounterRepository_GetWithDetails_LoadsGraph()
    {
        var dbName = Guid.NewGuid().ToString();
        var patientId = Guid.NewGuid();
        var clinicianId = Guid.NewGuid();

        var encounter = ClinicalEncounter.Make(
            patientId: patientId,
            clinicianId: clinicianId,
            encounterNumber: "ENC-DETAIL-001",
            encounterType: EncounterType.EmergencyTriage,
            chiefComplaint: "Acute chest pain radiating to left arm"
        );

        var indicators = ClinicalIndicators.Make(
            encounterId: encounter.Id,
            ageAtEncounter: 64,
            sex: 1,
            chestPainType: ChestPainType.TypicalAngina,
            bloodPressure: BloodPressure.Make(160, 95),
            cholesterol: CholesterolReading.Make(280, 0),
            ecg: EcgResult.Make(RestingEcgType.Normal, 140, true, 3.2m, StSlope.Downsloping),
            fluoroscopyVessels: 3,
            thalassemia: ThalassemiaType.ReversibleDefect
        );

        encounter.AttachClinicalIndicators(indicators);

        var assessment = RiskAssessmentResult.Make(
            encounterId: encounter.Id,
            modelId: Guid.NewGuid(),
            riskProbability: 0.9120m,
            riskBand: RiskBand.High,
            confidenceInterval: ConfidenceInterval.Make(0.8650m, 0.9590m),
            decisionThreshold: 0.420m,
            baseExpectedValue: -0.150m,
            cdssAdvisoryText: "Critical risk driven by typical angina and ST depression."
        );

        var attr = ShapAttribution.Make(
            riskAssessmentResultId: assessment.Id,
            featureKey: "oldpeak",
            featureDisplayName: "ST Depression",
            featureValueDisplay: "3.2 mm",
            attributionValue: 0.450m,
            impactDirection: ImpactDirection.RiskIncreasing
        );

        assessment.AddAttribution(attr);
        encounter.AddRiskAssessment(assessment);

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new ClinicalEncounterRepository(context);
            await repo.AddAsync(encounter);
            await context.ClinicalIndicators.AddAsync(indicators);
            await context.RiskAssessments.AddAsync(assessment);
            await context.SaveChangesAsync();
        }

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new ClinicalEncounterRepository(context);

            var detailed = await repo.GetWithDetailsAsync(encounter.Id);
            detailed.Should().NotBeNull();
            detailed!.EncounterNumber.Should().Be("ENC-DETAIL-001");
            detailed.Indicators.Should().NotBeNull();
            detailed.Indicators!.BloodPressure.Systolic.Should().Be(160);
            detailed.RiskAssessments.Should().HaveCount(1);
            detailed.RiskAssessments.First().Attributions.Should().HaveCount(1);
            detailed.RiskAssessments.First().Attributions.First().FeatureKey.Should().Be("oldpeak");

            var byNumber = await repo.GetByEncounterNumberAsync("ENC-DETAIL-001");
            byNumber.Should().NotBeNull();

            var byPatient = await repo.GetByPatientIdAsync(patientId);
            byPatient.Should().HaveCount(1);

            var byClinician = await repo.GetRecentByClinicianAsync(clinicianId, 10);
            byClinician.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task RiskAssessmentRepository_AddAndQuery_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var encounterId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var assessment = RiskAssessmentResult.Make(
            encounterId: encounterId,
            modelId: modelId,
            riskProbability: 0.4500m,
            riskBand: RiskBand.Moderate,
            confidenceInterval: ConfidenceInterval.Make(0.3800m, 0.5200m),
            decisionThreshold: 0.420m,
            baseExpectedValue: -0.150m,
            cdssAdvisoryText: "Moderate risk patient profile."
        );

        var attr = ShapAttribution.Make(
            riskAssessmentResultId: assessment.Id,
            featureKey: "thalach",
            featureDisplayName: "Max Heart Rate",
            featureValueDisplay: "165 bpm",
            attributionValue: -0.150m,
            impactDirection: ImpactDirection.Protective
        );

        assessment.AddAttribution(attr);

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new RiskAssessmentRepository(context);
            await repo.AddAsync(assessment);
            await context.SaveChangesAsync();
        }

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new RiskAssessmentRepository(context);

            var byId = await repo.GetByIdAsync(assessment.Id);
            byId.Should().NotBeNull();
            byId!.RiskBand.Should().Be(RiskBand.Moderate);
            byId.Attributions.Should().HaveCount(1);
            byId.Attributions.First().ImpactDirection.Should().Be(ImpactDirection.Protective);

            var latest = await repo.GetLatestByEncounterIdAsync(encounterId);
            latest.Should().NotBeNull();
            latest!.RiskProbability.Should().Be(0.4500m);

            var history = await repo.GetHistoryByEncounterIdAsync(encounterId);
            history.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task PredictionAuditRepository_AddAndQuery_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var encounterId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var audit = PredictionAudit.RecordSuccess(
            encounterId: encounterId,
            userId: userId,
            correlationId: correlationId,
            requestPayload: "{\"test\":\"request\"}",
            responsePayload: "{\"test\":\"response\"}",
            executionTimeMs: 25,
            explainingFeatures: "{\"shap\":[]}"
        );

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new PredictionAuditRepository(context);
            await repo.AddAsync(audit);
            await context.SaveChangesAsync();
        }

        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var repo = new PredictionAuditRepository(context);

            var byId = await repo.GetByIdAsync(audit.Id);
            byId.Should().NotBeNull();
            byId!.CorrelationId.Should().Be(correlationId);

            var byCorrelation = await repo.GetByCorrelationIdAsync(correlationId);
            byCorrelation.Should().NotBeNull();
            byCorrelation!.ExecutionTimeMs.Should().Be(25);
            byCorrelation.InferenceStatus.Should().Be(InferenceExecutionStatus.Success);

            var byEncounter = await repo.GetByEncounterIdAsync(encounterId);
            byEncounter.Should().HaveCount(1);
        }
    }
}
