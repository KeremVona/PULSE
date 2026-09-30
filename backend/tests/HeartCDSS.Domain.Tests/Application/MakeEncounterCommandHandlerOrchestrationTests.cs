using FluentAssertions;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Application.Common.Models;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Infrastructure.Persistence;
using HeartCDSS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HeartCDSS.Domain.Tests.Application;

public class MakeEncounterCommandHandlerOrchestrationTests
{
    private sealed class FakeMlInferenceClient : IMlInferenceClient
    {
        private readonly Func<MlInferenceRequestDto, CancellationToken, Task<MlInferenceResultDto>> _predictFunc;

        public List<MlInferenceRequestDto> CapturedRequests { get; } = [];

        public FakeMlInferenceClient(Func<MlInferenceRequestDto, CancellationToken, Task<MlInferenceResultDto>> predictFunc)
        {
            _predictFunc = predictFunc;
        }

        public async Task<MlInferenceResultDto> MakePredictionAsync(MlInferenceRequestDto request, CancellationToken cancellationToken = default)
        {
            CapturedRequests.Add(request);
            return await _predictFunc(request, cancellationToken);
        }

        public Task<MlHealthCheckResultDto> MakeHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MlHealthCheckResultDto.Make("healthy", true, "v1.0.0", "IsotonicRegression", 0.29, 100, 120, "2026-09-30T12:00:00Z"));
        }
    }

    private static HeartCDSSDbContext MakeInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HeartCDSSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new HeartCDSSDbContext(options);
    }

    private static ClinicalIndicatorsInputDto MakeIndicatorsDto()
    {
        return new ClinicalIndicatorsInputDto(
            Age: 61,
            Sex: 1,
            ChestPainType: 4,
            RestingBp: 142,
            DiastolicBp: 92,
            SerumCholesterol: 265,
            FastingBloodSugar: 1,
            RestingEcg: 1,
            MaxHeartRate: 120,
            ExerciseAngina: 1,
            OldpeakStDepression: 2.6m,
            Slope: 2,
            FluoroscopyVessels: 2,
            Thalassemia: 7
        );
    }

    private static MlInferenceResultDto MakeSamplePredictionResult(string encounterId)
    {
        return new MlInferenceResultDto
        {
            EncounterId = encounterId,
            CorrelationId = "corr-orchestration-001",
            RiskProbability = 0.785,
            RiskBand = "High",
            IsCadPositive = true,
            DecisionThreshold = 0.29,
            ConfidenceInterval = MlConfidenceIntervalDto.Make(0.72, 0.84, 0.95, 6.0),
            TopRiskDrivers =
            [
                MlFeatureAttributionDto.Make("oldpeak", "ST Depression (mm)", "2.6 mm", 0.175, "RISK_INCREASING", 1),
                MlFeatureAttributionDto.Make("ca", "Fluoroscopy Vessels", "2 vessels", 0.145, "RISK_INCREASING", 2)
            ],
            TopProtectiveFactors = [],
            AllAttributions = [],
            Explanations =
            [
                MlFeatureAttributionDto.Make("oldpeak", "ST Depression (mm)", "2.6 mm", 0.175, "RISK_INCREASING", 1),
                MlFeatureAttributionDto.Make("ca", "Fluoroscopy Vessels", "2 vessels", 0.145, "RISK_INCREASING", 2)
            ],
            ClinicalWarnings = ["Multi-Vessel Calcification: 2 major coronary vessels occluded."],
            RecommendedAction = "Immediate invasive catheterization referral.",
            ClinicalDisclaimer = "Screening decision support aid only.",
            InferenceLatencyMs = 15.6
        };
    }

    [Fact]
    public async Task Handle_WithMlClient_OrchestratesFullInferenceAndPersistsRiskResultAndAudit()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madePatient = Patient.Make(
            mrn: "MRN-ORCH-01",
            firstName: "Sarah",
            lastName: "Connor",
            dateOfBirth: new DateOnly(1965, 11, 10),
            biologicalSex: BiologicalSex.Female
        );

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(madePatient);
            await seedContext.SaveChangesAsync();
        }

        var fakeMlClient = new FakeMlInferenceClient((req, ct) =>
            Task.FromResult(MakeSamplePredictionResult(req.EncounterId!)));

        // Act
        MakeEncounterResultDto madeResult;
        await using (var actContext = MakeInMemoryDbContext(dbName))
        {
            var patientRepo = new PatientRepository(actContext);
            var encounterRepo = new ClinicalEncounterRepository(actContext);
            var riskRepo = new RiskAssessmentRepository(actContext);
            var auditRepo = new PredictionAuditRepository(actContext);

            var madeHandler = new MakeEncounterCommandHandler(
                patientRepository: patientRepo,
                encounterRepository: encounterRepo,
                unitOfWork: actContext,
                mlInferenceClient: fakeMlClient,
                riskAssessmentRepository: riskRepo,
                auditRepository: auditRepo
            );

            var madeCommand = new MakeEncounterCommand(
                PatientId: madePatient.Id,
                ClinicianId: Guid.NewGuid(),
                EncounterNumber: "ENC-ORCH-100",
                EncounterType: EncounterType.EmergencyTriage,
                ChiefComplaint: "Acute exertion-induced retrosternal tightness",
                Indicators: MakeIndicatorsDto()
            );

            madeResult = await madeHandler.Handle(madeCommand, CancellationToken.None);
        }

        // Assert: DTO contains immediate risk calculation
        madeResult.Should().NotBeNull();
        madeResult.Status.Should().Be(EncounterStatus.AssessmentComplete);
        madeResult.RiskAssessment.Should().NotBeNull();
        madeResult.RiskAssessment!.RiskProbability.Should().Be(0.785);
        madeResult.RiskAssessment.RiskBand.Should().Be("High");
        madeResult.RiskAssessment.IsCadPositive.Should().BeTrue();
        madeResult.RiskAssessment.TopRiskDrivers.Should().HaveCount(2);

        fakeMlClient.CapturedRequests.Should().ContainSingle();
        var captured = fakeMlClient.CapturedRequests[0];
        captured.PatientMrn.Should().Be("MRN-ORCH-01");
        captured.ClinicalIndicators.Age.Should().Be(61);

        // Assert: Database persistence of Encounter, RiskAssessmentResult, and PredictionAudit
        await using (var verifyContext = MakeInMemoryDbContext(dbName))
        {
            var persistedEncounter = await verifyContext.Encounters
                .Include(e => e.Indicators)
                .Include(e => e.RiskAssessments)
                    .ThenInclude(r => r.Attributions)
                .FirstOrDefaultAsync(e => e.Id == madeResult.EncounterId);

            persistedEncounter.Should().NotBeNull();
            persistedEncounter!.Status.Should().Be(EncounterStatus.AssessmentComplete);
            persistedEncounter.RiskAssessments.Should().ContainSingle();

            var persistedAssessment = persistedEncounter.RiskAssessments.First();
            persistedAssessment.RiskProbability.Should().Be(0.7850m);
            persistedAssessment.RiskBand.Should().Be(RiskBand.High);
            persistedAssessment.Attributions.Should().HaveCount(2);

            var persistedAudit = await verifyContext.PredictionAudits
                .FirstOrDefaultAsync(a => a.EncounterId == madeResult.EncounterId);

            persistedAudit.Should().NotBeNull();
            persistedAudit!.InferenceStatus.Should().Be(InferenceExecutionStatus.Success);
            persistedAudit.ResponsePayload.Should().Contain("0.785");
        }
    }

    [Fact]
    public async Task Handle_WhenMlClientThrowsUnavailableException_PersistsFailureAuditAndRethrows()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madePatient = Patient.Make(
            mrn: "MRN-ORCH-02",
            firstName: "John",
            lastName: "Connor",
            dateOfBirth: new DateOnly(1985, 2, 28),
            biologicalSex: BiologicalSex.Male
        );

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(madePatient);
            await seedContext.SaveChangesAsync();
        }

        var fakeMlClient = new FakeMlInferenceClient((req, ct) =>
            throw MlInferenceUnavailableException.MakeCircuitOpen());

        // Act
        await using var actContext = MakeInMemoryDbContext(dbName);
        var patientRepo = new PatientRepository(actContext);
        var encounterRepo = new ClinicalEncounterRepository(actContext);
        var riskRepo = new RiskAssessmentRepository(actContext);
        var auditRepo = new PredictionAuditRepository(actContext);

        var madeHandler = new MakeEncounterCommandHandler(
            patientRepository: patientRepo,
            encounterRepository: encounterRepo,
            unitOfWork: actContext,
            mlInferenceClient: fakeMlClient,
            riskAssessmentRepository: riskRepo,
            auditRepository: auditRepo
        );

        var madeCommand = new MakeEncounterCommand(
            PatientId: madePatient.Id,
            ClinicianId: Guid.NewGuid(),
            EncounterNumber: "ENC-ORCH-200",
            EncounterType: EncounterType.OutpatientScreening,
            ChiefComplaint: "Routine cardiovascular check",
            Indicators: MakeIndicatorsDto()
        );

        var madeAction = async () => await madeHandler.Handle(madeCommand, CancellationToken.None);

        // Assert: Exception is rethrown
        var thrown = await madeAction.Should().ThrowAsync<MlInferenceUnavailableException>();
        thrown.Which.DownstreamReason.Should().Be("CIRCUIT_BREAKER_OPEN");

        // Assert: Failure audit was recorded in database
        var persistedAudit = await actContext.PredictionAudits
            .FirstOrDefaultAsync(a => a.UserId == madeCommand.ClinicianId);

        persistedAudit.Should().NotBeNull();
        persistedAudit!.InferenceStatus.Should().Be(InferenceExecutionStatus.CircuitBreakerOpen);
        persistedAudit.ErrorDetails.Should().Contain("circuit breaker is open");
    }
}
