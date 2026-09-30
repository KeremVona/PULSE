using System.Security.Claims;
using FluentAssertions;
using HeartCDSS.Api.Controllers;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Application.Encounters.Queries.GetEncounterById;
using HeartCDSS.Application.Encounters.Queries.GetPatientEncounters;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HeartCDSS.Domain.Tests.Api;

public class EncountersControllerTests
{
    private sealed class FakeMediator : IMediator
    {
        private readonly Func<object, CancellationToken, Task<object?>> _handler;

        public List<object> SentRequests { get; } = [];

        public FakeMediator(Func<object, CancellationToken, Task<object?>> handler)
        {
            _handler = handler;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            var res = await _handler(request, cancellationToken);
            return (TResponse)res!;
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SentRequests.Add(request);
            await _handler(request, cancellationToken);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return _handler(request, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return Task.CompletedTask;
        }
    }

    private static ClinicalIndicatorsInputDto MakeIndicatorsDto()
    {
        return new ClinicalIndicatorsInputDto(
            Age: 55,
            Sex: 1,
            ChestPainType: 2,
            RestingBp: 130,
            DiastolicBp: 85,
            SerumCholesterol: 220,
            FastingBloodSugar: 0,
            RestingEcg: 0,
            MaxHeartRate: 150,
            ExerciseAngina: 0,
            OldpeakStDepression: 1.0m,
            Slope: 1,
            FluoroscopyVessels: 0,
            Thalassemia: 3
        );
    }

    private static MakeEncounterApiRequest MakeApiRequest()
    {
        return new MakeEncounterApiRequest(
            EncounterNumber: "ENC-API-001",
            EncounterType: EncounterType.OutpatientScreening,
            ChiefComplaint: "Routine cardiovascular risk evaluation",
            Indicators: MakeIndicatorsDto()
        );
    }

    private static EncountersController MakeSut(IMediator mediator, Guid? clinicianId = null)
    {
        var madeController = new EncountersController(mediator, NullLogger<EncountersController>.Instance);

        var claims = new List<Claim>
        {
            new("clinician_id", (clinicianId ?? Guid.NewGuid()).ToString()),
            new(ClaimTypes.Role, Role.Physician)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        madeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return madeController;
    }

    [Fact]
    public async Task MakeEncounter_WhenValid_ReturnsCreatedAtActionWithResultDto()
    {
        // Arrange
        var madePatientId = Guid.NewGuid();
        var madeEncounterId = Guid.NewGuid();
        var madeExpectedResult = new MakeEncounterResultDto(
            EncounterId: madeEncounterId,
            PatientId: madePatientId,
            ClinicianId: Guid.NewGuid(),
            EncounterNumber: "ENC-API-001",
            EncounterType: EncounterType.OutpatientScreening,
            Status: EncounterStatus.InProgress,
            ChiefComplaint: "Routine cardiovascular risk evaluation",
            EncounterDate: DateTimeOffset.UtcNow,
            ClinicalAlerts: []
        );

        var madeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(madeExpectedResult));
        var madeSut = MakeSut(madeMediator);
        var madeRequest = MakeApiRequest();

        // Act
        var madeActionResult = await madeSut.MakeEncounter(madePatientId, madeRequest, CancellationToken.None);

        // Assert
        madeActionResult.Should().BeOfType<CreatedAtActionResult>();
        var madeResultAction = madeActionResult as CreatedAtActionResult;
        madeResultAction!.ActionName.Should().Be(nameof(EncountersController.GetEncounterById));
        madeResultAction.Value.Should().Be(madeExpectedResult);

        madeMediator.SentRequests.Should().ContainSingle();
        var sentCommand = madeMediator.SentRequests[0] as MakeEncounterCommand;
        sentCommand.Should().NotBeNull();
        sentCommand!.PatientId.Should().Be(madePatientId);
        sentCommand.EncounterNumber.Should().Be("ENC-API-001");
    }

    [Fact]
    public async Task GetEncounterById_WhenFound_ReturnsOkWithDetailDto()
    {
        // Arrange
        var madePatientId = Guid.NewGuid();
        var madeEncounterId = Guid.NewGuid();
        var madeDetail = new EncounterDetailDto(
            EncounterId: madeEncounterId,
            PatientId: madePatientId,
            ClinicianId: Guid.NewGuid(),
            EncounterNumber: "ENC-API-002",
            EncounterType: "OutpatientScreening",
            Status: "AssessmentComplete",
            ChiefComplaint: null,
            EncounterDate: DateTimeOffset.UtcNow,
            Indicators: null,
            RiskAssessments: [],
            ClinicalAlerts: []
        );

        var madeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(madeDetail));
        var madeSut = MakeSut(madeMediator);

        // Act
        var madeActionResult = await madeSut.GetEncounterById(madePatientId, madeEncounterId, CancellationToken.None);

        // Assert
        madeActionResult.Should().BeOfType<OkObjectResult>();
        var okResult = madeActionResult as OkObjectResult;
        okResult!.Value.Should().Be(madeDetail);
    }

    [Fact]
    public async Task GetEncounterById_WhenNotFound_ReturnsNotFoundProblemDetails()
    {
        // Arrange
        var madeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(null));
        var madeSut = MakeSut(madeMediator);
        var madePatientId = Guid.NewGuid();
        var madeEncounterId = Guid.NewGuid();

        // Act
        var madeActionResult = await madeSut.GetEncounterById(madePatientId, madeEncounterId, CancellationToken.None);

        // Assert
        madeActionResult.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = madeActionResult as NotFoundObjectResult;
        var problemDetails = notFoundResult!.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Title.Should().Be("Encounter Not Found");
    }

    [Fact]
    public async Task GetPatientEncounters_ReturnsOkWithEncountersList()
    {
        // Arrange
        var madePatientId = Guid.NewGuid();
        var madeList = new List<EncounterDetailDto>
        {
            new(
                EncounterId: Guid.NewGuid(),
                PatientId: madePatientId,
                ClinicianId: Guid.NewGuid(),
                EncounterNumber: "ENC-001",
                EncounterType: "OutpatientScreening",
                Status: "InProgress",
                ChiefComplaint: null,
                EncounterDate: DateTimeOffset.UtcNow,
                Indicators: null,
                RiskAssessments: [],
                ClinicalAlerts: []
            )
        };

        var madeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(madeList));
        var madeSut = MakeSut(madeMediator);

        // Act
        var madeActionResult = await madeSut.GetPatientEncounters(madePatientId, CancellationToken.None);

        // Assert
        madeActionResult.Should().BeOfType<OkObjectResult>();
        var okResult = madeActionResult as OkObjectResult;
        okResult!.Value.Should().Be(madeList);
    }
}
