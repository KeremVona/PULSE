using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HeartCDSS.Api.Controllers;
using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Application.Common.Models;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Application.Encounters.Queries.GetEncounterById;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HeartCDSS.Domain.Tests.Api;

public class EncountersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public EncountersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    private sealed class StubMlInferenceClient : IMlInferenceClient
    {
        public Task<MlInferenceResultDto> MakePredictionAsync(MlInferenceRequestDto request, CancellationToken cancellationToken = default)
        {
            var madeResult = new MlInferenceResultDto
            {
                EncounterId = request.EncounterId,
                CorrelationId = request.CorrelationId,
                RiskProbability = 0.654,
                RiskBand = "Elevated",
                IsCadPositive = true,
                DecisionThreshold = 0.29,
                ConfidenceInterval = MlConfidenceIntervalDto.Make(0.58, 0.72, 0.95, 7.0),
                TopRiskDrivers =
                [
                    MlFeatureAttributionDto.Make("oldpeak", "ST Depression", "2.2 mm", 0.14, "RISK_INCREASING", 1)
                ],
                TopProtectiveFactors = [],
                AllAttributions = [],
                Explanations =
                [
                    MlFeatureAttributionDto.Make("oldpeak", "ST Depression", "2.2 mm", 0.14, "RISK_INCREASING", 1)
                ],
                ClinicalWarnings = [],
                RecommendedAction = "Cardiology evaluation.",
                ClinicalDisclaimer = "Screening aid.",
                InferenceLatencyMs = 12.0
            };

            return Task.FromResult(madeResult);
        }

        public Task<MlHealthCheckResultDto> MakeHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MlHealthCheckResultDto.Make("healthy", true, "v1.0.0", "IsotonicRegression", 0.29, 50, 100, "2026-09-30T12:00:00Z"));
        }
    }

    private WebApplicationFactory<Program> MakeCustomFactory(string dbName)
    {
        return _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("UseInMemoryDatabase", "true");
            builder.UseSetting("InMemoryDbName", dbName);
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = dbName
                });
            });
            builder.ConfigureServices(services =>
            {
                // Replace ML inference client with stub
                var mlDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMlInferenceClient));
                if (mlDescriptor != null)
                {
                    services.Remove(mlDescriptor);
                }
                services.AddSingleton<IMlInferenceClient, StubMlInferenceClient>();
            });
        });
    }

    private static ClinicalIndicatorsInputDto MakeValidIndicators()
    {
        return new ClinicalIndicatorsInputDto(
            Age: 58,
            Sex: 1,
            ChestPainType: 2,
            RestingBp: 135,
            DiastolicBp: 85,
            SerumCholesterol: 230,
            FastingBloodSugar: 0,
            RestingEcg: 1,
            MaxHeartRate: 155,
            ExerciseAngina: 0,
            OldpeakStDepression: 1.5m,
            Slope: 2,
            FluoroscopyVessels: 1,
            Thalassemia: 3
        );
    }

    private static string MakeToken(WebApplicationFactory<Program> factory, string role = Role.Physician)
    {
        using var scope = factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtService.MakeToken(Guid.NewGuid(), "test.doctor", "Dr. Test Doctor", [role]);
    }

    [Fact]
    public async Task MakeEncounter_WithoutAuth_ShouldReturn401Unauthorized()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();
        var madePatientId = Guid.NewGuid();

        var madeRequest = new MakeEncounterApiRequest(
            EncounterNumber: "ENC-NOAUTH-01",
            EncounterType: EncounterType.OutpatientScreening,
            ChiefComplaint: "Routine test",
            Indicators: MakeValidIndicators()
        );

        var content = new StringContent(JsonSerializer.Serialize(madeRequest), Encoding.UTF8, "application/json");

        // Act
        var madeResponse = await madeClient.PostAsync($"/api/v1/patients/{madePatientId}/encounters", content);

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MakeEncounter_WithPhysicianToken_ShouldReturn201CreatedWithRiskPayload()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();

        var madePatientId = Guid.NewGuid();
        var madePatient = Patient.Make(
            mrn: "MRN-INTEG-01",
            firstName: "James",
            lastName: "Holden",
            dateOfBirth: new DateOnly(1980, 5, 20),
            biologicalSex: BiologicalSex.Male,
            id: madePatientId
        );

        using (var scope = madeFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HeartCDSSDbContext>();
            await db.Patients.AddAsync(madePatient);
            await db.SaveChangesAsync();
        }

        var madeToken = MakeToken(madeFactory, Role.Physician);
        madeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", madeToken);

        var madeRequest = new MakeEncounterApiRequest(
            EncounterNumber: "ENC-INTEG-01",
            EncounterType: EncounterType.CardiologyFollowUp,
            ChiefComplaint: "Follow-up post stress test",
            Indicators: MakeValidIndicators()
        );

        var content = new StringContent(JsonSerializer.Serialize(madeRequest), Encoding.UTF8, "application/json");

        // Act
        var madeResponse = await madeClient.PostAsync($"/api/v1/patients/{madePatientId}/encounters", content);

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        madeResponse.Headers.Location.Should().NotBeNull();

        var responseBody = await madeResponse.Content.ReadAsStringAsync();
        var madeResult = JsonSerializer.Deserialize<MakeEncounterResultDto>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        madeResult.Should().NotBeNull();
        madeResult!.EncounterNumber.Should().Be("ENC-INTEG-01");
        madeResult.Status.Should().Be(EncounterStatus.AssessmentComplete);
        madeResult.RiskAssessment.Should().NotBeNull();
        madeResult.RiskAssessment!.RiskProbability.Should().Be(0.654);
        madeResult.RiskAssessment.RiskBand.Should().Be("Elevated");
    }

    [Fact]
    public async Task MakeEncounter_WhenPhysiologicalIndicatorsViolateBounds_ShouldReturn400BadRequest()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();

        var madeToken = MakeToken(madeFactory, Role.Physician);
        madeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", madeToken);

        // Blood pressure 300 mmHg violates 60-260 boundary
        var invalidIndicators = new ClinicalIndicatorsInputDto(
            Age: 50,
            Sex: 1,
            ChestPainType: 1,
            RestingBp: 300,
            DiastolicBp: 150,
            SerumCholesterol: 200,
            FastingBloodSugar: 0,
            RestingEcg: 0,
            MaxHeartRate: 150,
            ExerciseAngina: 0,
            OldpeakStDepression: 1.0m,
            Slope: 1,
            FluoroscopyVessels: 0,
            Thalassemia: 3
        );

        var madeRequest = new MakeEncounterApiRequest(
            EncounterNumber: "ENC-INVALID-01",
            EncounterType: EncounterType.EmergencyTriage,
            ChiefComplaint: "Hypertensive crisis test",
            Indicators: invalidIndicators
        );

        var content = new StringContent(JsonSerializer.Serialize(madeRequest), Encoding.UTF8, "application/json");

        // Act
        var madeResponse = await madeClient.PostAsync($"/api/v1/patients/{Guid.NewGuid()}/encounters", content);

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseBody = await madeResponse.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Indicators.RestingBp");
    }

    [Fact]
    public async Task GetEncounterById_WhenFound_ShouldReturn200OKWithDetails()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();

        var madePatientId = Guid.NewGuid();
        var madePatient = Patient.Make(
            mrn: "MRN-INTEG-02",
            firstName: "Amos",
            lastName: "Burton",
            dateOfBirth: new DateOnly(1975, 3, 12),
            biologicalSex: BiologicalSex.Male,
            id: madePatientId
        );

        var madeEncounterId = Guid.NewGuid();
        var madeEncounter = ClinicalEncounter.Make(
            patientId: madePatientId,
            clinicianId: Guid.NewGuid(),
            encounterNumber: "ENC-GET-01",
            encounterType: EncounterType.OutpatientScreening,
            id: madeEncounterId
        );

        using (var scope = madeFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HeartCDSSDbContext>();
            await db.Patients.AddAsync(madePatient);
            await db.Encounters.AddAsync(madeEncounter);
            await db.SaveChangesAsync();
        }

        var madeToken = MakeToken(madeFactory, Role.Physician);
        madeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", madeToken);

        // Act
        var madeResponse = await madeClient.GetAsync($"/api/v1/patients/{madePatientId}/encounters/{madeEncounterId}");

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await madeResponse.Content.ReadAsStringAsync();
        var madeDetail = JsonSerializer.Deserialize<EncounterDetailDto>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        madeDetail.Should().NotBeNull();
        madeDetail!.EncounterId.Should().Be(madeEncounterId);
        madeDetail.EncounterNumber.Should().Be("ENC-GET-01");
    }

    [Fact]
    public async Task GetEncounterById_WhenNotFound_ShouldReturn404NotFound()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();

        var madeToken = MakeToken(madeFactory, Role.Physician);
        madeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", madeToken);

        // Act
        var madeResponse = await madeClient.GetAsync($"/api/v1/patients/{Guid.NewGuid()}/encounters/{Guid.NewGuid()}");

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPatientEncounters_WhenEncountersExist_ShouldReturn200OKWithList()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();

        var madePatientId = Guid.NewGuid();
        var madePatient = Patient.Make(
            mrn: "MRN-INTEG-LIST",
            firstName: "Alex",
            lastName: "Kamal",
            dateOfBirth: new DateOnly(1975, 3, 15),
            biologicalSex: BiologicalSex.Male,
            id: madePatientId
        );

        var madeClinicianId = Guid.NewGuid();
        var madeEncounter1 = ClinicalEncounter.Make(
            patientId: madePatientId,
            clinicianId: madeClinicianId,
            encounterNumber: "ENC-LIST-01",
            encounterType: EncounterType.OutpatientScreening,
            chiefComplaint: "Checkup 1"
        );
        var madeEncounter2 = ClinicalEncounter.Make(
            patientId: madePatientId,
            clinicianId: madeClinicianId,
            encounterNumber: "ENC-LIST-02",
            encounterType: EncounterType.CardiologyFollowUp,
            chiefComplaint: "Checkup 2"
        );

        using (var scope = madeFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HeartCDSSDbContext>();
            await db.Patients.AddAsync(madePatient);
            await db.Encounters.AddRangeAsync(madeEncounter1, madeEncounter2);
            await db.SaveChangesAsync();
        }

        var madeToken = MakeToken(madeFactory, Role.Physician);
        madeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", madeToken);

        // Act
        var madeResponse = await madeClient.GetAsync($"/api/v1/patients/{madePatientId}/encounters");

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await madeResponse.Content.ReadAsStringAsync();
        var madeList = JsonSerializer.Deserialize<List<EncounterDetailDto>>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        madeList.Should().NotBeNull();
        madeList!.Should().HaveCount(2);
    }

    [Fact]
    public async Task MakeEncounter_WhenPatientNotFound_ShouldReturn404NotFound()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();

        var madeToken = MakeToken(madeFactory, Role.Physician);
        madeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", madeToken);

        var madeRequest = new MakeEncounterApiRequest(
            EncounterNumber: "ENC-NONEXIST-01",
            EncounterType: EncounterType.OutpatientScreening,
            ChiefComplaint: "Patient does not exist",
            Indicators: MakeValidIndicators()
        );

        var content = new StringContent(JsonSerializer.Serialize(madeRequest), Encoding.UTF8, "application/json");

        // Act
        var madeResponse = await madeClient.PostAsync($"/api/v1/patients/{Guid.NewGuid()}/encounters", content);

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MakeEncounter_WithAuditorRole_ShouldReturn403Forbidden()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madeFactory = MakeCustomFactory(dbName);
        var madeClient = madeFactory.CreateClient();

        // Auditor is allowed read access but forbidden from making encounters
        var madeToken = MakeToken(madeFactory, Role.Auditor);
        madeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", madeToken);

        var madeRequest = new MakeEncounterApiRequest(
            EncounterNumber: "ENC-FORBIDDEN-01",
            EncounterType: EncounterType.OutpatientScreening,
            ChiefComplaint: "Auditor trying to make encounter",
            Indicators: MakeValidIndicators()
        );

        var content = new StringContent(JsonSerializer.Serialize(madeRequest), Encoding.UTF8, "application/json");

        // Act
        var madeResponse = await madeClient.PostAsync($"/api/v1/patients/{Guid.NewGuid()}/encounters", content);

        // Assert
        madeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
