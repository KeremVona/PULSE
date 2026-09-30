using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.ValueObjects;
using HeartCDSS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HeartCDSS.Domain.Tests.Infrastructure;

public class HeartCDSSDbContextTests
{
    private static HeartCDSSDbContext MakeInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HeartCDSSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new HeartCDSSDbContext(options);
    }

    [Fact]
    public async Task CanInsertAndRetrievePatient_WithSoftDeleteFilter()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var patient = Patient.Make(
            mrn: "MRN-TEST-001",
            firstName: "Jane",
            lastName: "Doe",
            dateOfBirth: new DateOnly(1980, 5, 12),
            biologicalSex: BiologicalSex.Female,
            phoneNumber: "+15551234567"
        );

        // Act - Save
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            await context.Patients.AddAsync(patient);
            await context.SaveChangesAsync();
        }

        // Assert - Retrieve
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var retrieved = await context.Patients.FirstOrDefaultAsync(p => p.Id == patient.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Mrn.Should().Be("MRN-TEST-001");
            retrieved.FirstName.Should().Be("Jane");
            retrieved.LastName.Should().Be("Doe");
            retrieved.BiologicalSex.Should().Be(BiologicalSex.Female);
            retrieved.Status.Should().Be(PatientStatus.Active);
            retrieved.IsDeleted.Should().BeFalse();

            // Soft-delete
            retrieved.MarkDeleted();
            await context.SaveChangesAsync();
        }

        // Assert - Soft-deleted should be filtered out by default
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var filtered = await context.Patients.FirstOrDefaultAsync(p => p.Id == patient.Id);
            filtered.Should().BeNull();

            var unfiltered = await context.Patients.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == patient.Id);
            unfiltered.Should().NotBeNull();
            unfiltered!.IsDeleted.Should().BeTrue();
            unfiltered.DeletedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task CanPersistEncounter_WithClinicalIndicatorsAndOwnedValueObjects()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var patientId = Guid.NewGuid();
        var clinicianId = Guid.NewGuid();

        var encounter = ClinicalEncounter.Make(
            patientId: patientId,
            clinicianId: clinicianId,
            encounterNumber: "ENC-2026-TEST",
            encounterType: EncounterType.CardiologyFollowUp,
            chiefComplaint: "Substernal chest tightness on exertion"
        );

        var indicators = ClinicalIndicators.Make(
            encounterId: encounter.Id,
            ageAtEncounter: 58,
            sex: 1,
            chestPainType: ChestPainType.AtypicalAngina,
            bloodPressure: BloodPressure.Make(145, 92),
            cholesterol: CholesterolReading.Make(245, 1),
            ecg: EcgResult.Make(RestingEcgType.StTAbnormality, 152, true, 2.3m, StSlope.Flat),
            fluoroscopyVessels: 2,
            thalassemia: ThalassemiaType.ReversibleDefect
        );

        encounter.AttachClinicalIndicators(indicators);

        // Act
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            await context.Encounters.AddAsync(encounter);
            await context.ClinicalIndicators.AddAsync(indicators);
            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var retrieved = await context.Encounters
                .Include(e => e.Indicators)
                .FirstOrDefaultAsync(e => e.Id == encounter.Id);

            retrieved.Should().NotBeNull();
            retrieved!.EncounterNumber.Should().Be("ENC-2026-TEST");
            retrieved.Indicators.Should().NotBeNull();
            retrieved.Indicators!.AgeAtEncounter.Should().Be(58);
            retrieved.Indicators.Sex.Should().Be(1);
            retrieved.Indicators.ChestPainType.Should().Be(ChestPainType.AtypicalAngina);

            // Verify owned BloodPressure
            retrieved.Indicators.BloodPressure.Should().NotBeNull();
            retrieved.Indicators.BloodPressure.Systolic.Should().Be(145);
            retrieved.Indicators.BloodPressure.Diastolic.Should().Be(92);
            retrieved.Indicators.BloodPressure.IsHypertensive.Should().BeTrue();

            // Verify owned CholesterolReading
            retrieved.Indicators.Cholesterol.Should().NotBeNull();
            retrieved.Indicators.Cholesterol.SerumCholesterolMgDl.Should().Be(245);
            retrieved.Indicators.Cholesterol.FastingBloodSugarAbove120.Should().BeTrue();
            retrieved.Indicators.Cholesterol.IsHigh.Should().BeTrue();

            // Verify owned EcgResult
            retrieved.Indicators.Ecg.Should().NotBeNull();
            retrieved.Indicators.Ecg.RestingEcg.Should().Be(RestingEcgType.StTAbnormality);
            retrieved.Indicators.Ecg.MaxHeartRateBpm.Should().Be(152);
            retrieved.Indicators.Ecg.HasExerciseInducedAngina.Should().BeTrue();
            retrieved.Indicators.Ecg.OldpeakStDepression.Should().Be(2.3m);
            retrieved.Indicators.Ecg.Slope.Should().Be(StSlope.Flat);
        }
    }

    [Fact]
    public async Task CanPersistRiskAssessment_WithConfidenceIntervalAndShapAttributions()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var encounterId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var riskAssessment = RiskAssessmentResult.Make(
            encounterId: encounterId,
            modelId: modelId,
            riskProbability: 0.8420m,
            riskBand: RiskBand.High,
            confidenceInterval: ConfidenceInterval.Make(0.7850m, 0.8990m, 0.95m),
            decisionThreshold: 0.420m,
            baseExpectedValue: -0.150m,
            cdssAdvisoryText: "High risk profile driven by chest pain and ST depression.",
            recommendedAction: "Immediate cardiology referral and coronary angiography"
        );

        var attr1 = ShapAttribution.Make(
            riskAssessmentResultId: riskAssessment.Id,
            featureKey: "cp",
            featureDisplayName: "Chest Pain Type",
            featureValueDisplay: "Atypical Angina",
            attributionValue: 0.3450m,
            impactDirection: ImpactDirection.RiskIncreasing,
            percentageImpact: 35.5m,
            rankOrder: 1
        );

        var attr2 = ShapAttribution.Make(
            riskAssessmentResultId: riskAssessment.Id,
            featureKey: "oldpeak",
            featureDisplayName: "ST Depression",
            featureValueDisplay: "2.3 mm",
            attributionValue: 0.2850m,
            impactDirection: ImpactDirection.RiskIncreasing,
            percentageImpact: 29.3m,
            rankOrder: 2
        );

        riskAssessment.AddAttribution(attr1);
        riskAssessment.AddAttribution(attr2);

        // Act
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            await context.RiskAssessments.AddAsync(riskAssessment);
            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var retrieved = await context.RiskAssessments
                .Include(r => r.Attributions)
                .FirstOrDefaultAsync(r => r.Id == riskAssessment.Id);

            retrieved.Should().NotBeNull();
            retrieved!.RiskProbability.Should().Be(0.8420m);
            retrieved.RiskBand.Should().Be(RiskBand.High);
            retrieved.IsCadPositive.Should().BeTrue();

            // Verify owned ConfidenceInterval
            retrieved.ConfidenceInterval.Should().NotBeNull();
            retrieved.ConfidenceInterval.LowerBound.Should().Be(0.7850m);
            retrieved.ConfidenceInterval.UpperBound.Should().Be(0.8990m);
            retrieved.ConfidenceInterval.ConfidenceLevel.Should().Be(0.95m);

            // Verify Attributions
            retrieved.Attributions.Should().HaveCount(2);
            var sorted = retrieved.Attributions.OrderBy(a => a.RankOrder).ToList();
            sorted[0].FeatureKey.Should().Be("cp");
            sorted[0].ImpactDirection.Should().Be(ImpactDirection.RiskIncreasing);
            sorted[1].FeatureKey.Should().Be("oldpeak");
        }
    }

    [Fact]
    public async Task CanPersistPredictionAudit_WithRawJsonPayloadsAndExplainingFeatures()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var encounterId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var requestJson = "{\"age\":62,\"sex\":1,\"cp\":3,\"trestbps\":140,\"chol\":260}";
        var responseJson = "{\"cad_probability\":0.82,\"risk_band\":\"High\",\"is_cad_positive\":true}";
        var shapJson = "[{\"feature\":\"cp\",\"shap_value\":0.32},{\"feature\":\"chol\",\"shap_value\":0.18}]";

        var audit = PredictionAudit.RecordSuccess(
            encounterId: encounterId,
            userId: userId,
            correlationId: correlationId,
            requestPayload: requestJson,
            responsePayload: responseJson,
            executionTimeMs: 42,
            explainingFeatures: shapJson,
            clientIp: "127.0.0.1",
            userAgent: "HeartCDSS-Client/1.0"
        );

        // Act
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            await context.PredictionAudits.AddAsync(audit);
            await context.SaveChangesAsync();
        }

        // Assert
        await using (var context = MakeInMemoryDbContext(dbName))
        {
            var retrieved = await context.PredictionAudits
                .FirstOrDefaultAsync(a => a.CorrelationId == correlationId);

            retrieved.Should().NotBeNull();
            retrieved!.RequestPayload.Should().Be(requestJson);
            retrieved.ResponsePayload.Should().Be(responseJson);
            retrieved.ExplainingFeatures.Should().Be(shapJson);
            retrieved.ExecutionTimeMs.Should().Be(42);
            retrieved.InferenceStatus.Should().Be(InferenceExecutionStatus.Success);
            retrieved.ClientIp.Should().Be("127.0.0.1");
            retrieved.UserAgent.Should().Be("HeartCDSS-Client/1.0");
        }
    }
}
