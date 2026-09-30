using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Application.Common.Models;
using HeartCDSS.Infrastructure.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace HeartCDSS.Domain.Tests.Infrastructure;

public class FastApiInferenceClientTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

        public List<HttpRequestMessage> CapturedRequests { get; } = [];

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
        {
            _handlerFunc = handlerFunc;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequests.Add(request);
            return await _handlerFunc(request, cancellationToken);
        }
    }

    private static MlInferenceRequestDto MakeSampleRequest(string? correlationId = "corr-test-123")
    {
        var madeIndicators = MlIndicatorsPayloadDto.Make(
            age: 60,
            sex: 1,
            cp: 4,
            trestbps: 140,
            chol: 250,
            fbs: 0,
            restecg: 1,
            thalach: 130,
            exang: 1,
            oldpeak: 2.5,
            slope: 2,
            ca: 2,
            thal: 7
        );

        return MlInferenceRequestDto.Make(
            indicators: madeIndicators,
            encounterId: "enc-abc-456",
            correlationId: correlationId,
            patientMrn: "MRN-9988"
        );
    }

    private static HttpResponseMessage MakeSuccessfulPredictionResponse()
    {
        var json = """
        {
            "encounter_id": "enc-abc-456",
            "correlation_id": "corr-test-123",
            "risk_probability": 0.745,
            "risk_band": "High",
            "is_cad_positive": true,
            "decision_threshold": 0.29,
            "confidence_interval": {
                "lower": 0.68,
                "upper": 0.81,
                "confidence_level": 0.95,
                "margin_percentage": 6.5
            },
            "top_risk_drivers": [
                {
                    "feature_key": "oldpeak",
                    "feature_label": "ST Depression",
                    "patient_value": "2.5 mm",
                    "shap_value": 0.165,
                    "impact_direction": "RISK_INCREASING",
                    "rank": 1
                }
            ],
            "top_protective_factors": [],
            "all_attributions": [],
            "explanations": [],
            "clinical_warnings": [
                "Severe ST Depression: oldpeak (2.5 mm) indicates ischemia."
            ],
            "recommended_action": "Refer to cardiology.",
            "clinical_disclaimer": "Clinical Decision Support Aid.",
            "inference_latency_ms": 11.2
        }
        """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static FastApiInferenceClient MakeSut(FakeHttpMessageHandler madeHandler)
    {
        var madeHttpClient = new HttpClient(madeHandler)
        {
            BaseAddress = new Uri("http://localhost:8000")
        };
        return new FastApiInferenceClient(madeHttpClient, NullLogger<FastApiInferenceClient>.Instance);
    }

    [Fact]
    public async Task MakePredictionAsync_WhenFastApiReturns200_DeserializesResponseAndPropagatesHeader()
    {
        // Arrange
        var madeHandler = new FakeHttpMessageHandler((req, ct) => Task.FromResult(MakeSuccessfulPredictionResponse()));
        var madeSut = MakeSut(madeHandler);
        var madeRequest = MakeSampleRequest("corr-test-123");

        // Act
        var madeResult = await madeSut.MakePredictionAsync(madeRequest);

        // Assert
        madeResult.Should().NotBeNull();
        madeResult.RiskProbability.Should().Be(0.745);
        madeResult.RiskBand.Should().Be("High");
        madeResult.IsCadPositive.Should().BeTrue();
        madeResult.TopRiskDrivers.Should().ContainSingle();
        madeResult.ClinicalWarnings.Should().ContainSingle();

        madeHandler.CapturedRequests.Should().ContainSingle();
        var captured = madeHandler.CapturedRequests[0];
        captured.RequestUri!.AbsolutePath.Should().Be("/api/v1/predict");
        captured.Headers.Should().ContainKey("X-Correlation-Id");
        captured.Headers.GetValues("X-Correlation-Id").Should().Contain("corr-test-123");
    }

    [Fact]
    public async Task MakePredictionAsync_WhenNoCorrelationIdProvided_GeneratesAndPropagatesNewCorrelationId()
    {
        // Arrange
        var madeHandler = new FakeHttpMessageHandler((req, ct) => Task.FromResult(MakeSuccessfulPredictionResponse()));
        var madeSut = MakeSut(madeHandler);
        var madeRequest = MakeSampleRequest(correlationId: null);

        // Act
        var madeResult = await madeSut.MakePredictionAsync(madeRequest);

        // Assert
        madeResult.Should().NotBeNull();
        madeHandler.CapturedRequests.Should().ContainSingle();
        var captured = madeHandler.CapturedRequests[0];
        captured.Headers.Should().ContainKey("X-Correlation-Id");
        var headerVal = captured.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        headerVal.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MakePredictionAsync_WhenFastApiReturns422_ThrowsMlInferenceValidationException()
    {
        // Arrange
        var errorJson = """
        {
            "title": "Validation Error",
            "status": 422,
            "detail": "Patient clinical indicators failed physiological boundary validation.",
            "errors": [
                {
                    "loc": ["body", "clinical_indicators", "trestbps"],
                    "msg": "Resting Blood Pressure (45.0 mmHg) violates physiological limits (60 - 260 mmHg).",
                    "type": "value_error"
                }
            ]
        }
        """;

        var madeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var res = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(res);
        });

        var madeSut = MakeSut(madeHandler);
        var madeRequest = MakeSampleRequest();

        // Act
        var madeAction = async () => await madeSut.MakePredictionAsync(madeRequest);

        // Assert
        var madeException = await madeAction.Should().ThrowAsync<MlInferenceValidationException>();
        madeException.Which.Message.Should().Contain("physiological boundary validation");
        madeException.Which.Errors.Should().ContainKey("trestbps");
        madeException.Which.Errors["trestbps"].Should().Contain(msg => msg.Contains("Resting Blood Pressure"));
    }

    [Fact]
    public async Task MakePredictionAsync_WhenFastApiReturns503_ThrowsMlInferenceUnavailableException()
    {
        // Arrange
        var madeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var res = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service degraded during maintenance", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(res);
        });

        var madeSut = MakeSut(madeHandler);
        var madeRequest = MakeSampleRequest();

        // Act
        var madeAction = async () => await madeSut.MakePredictionAsync(madeRequest);

        // Assert
        var madeException = await madeAction.Should().ThrowAsync<MlInferenceUnavailableException>();
        madeException.Which.StatusCode.Should().Be(503);
        madeException.Which.Message.Should().Contain("Service degraded");
    }

    [Fact]
    public async Task MakePredictionAsync_WhenFastApiReturns500_ThrowsMlInferenceUnavailableException()
    {
        // Arrange
        var madeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var res = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Unexpected Python traceback exception", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(res);
        });

        var madeSut = MakeSut(madeHandler);
        var madeRequest = MakeSampleRequest();

        // Act
        var madeAction = async () => await madeSut.MakePredictionAsync(madeRequest);

        // Assert
        var madeException = await madeAction.Should().ThrowAsync<MlInferenceUnavailableException>();
        madeException.Which.StatusCode.Should().Be(500);
        madeException.Which.Message.Should().Contain("unexpected HTTP 500");
    }

    [Fact]
    public async Task MakePredictionAsync_WhenCircuitBreakerThrowsBrokenCircuitException_ThrowsUnavailableExceptionWithCircuitReason()
    {
        // Arrange
        var madeHandler = new FakeHttpMessageHandler((req, ct) =>
            throw new BrokenCircuitException("The circuit is broken and not accepting calls."));

        var madeSut = MakeSut(madeHandler);
        var madeRequest = MakeSampleRequest();

        // Act
        var madeAction = async () => await madeSut.MakePredictionAsync(madeRequest);

        // Assert
        var madeException = await madeAction.Should().ThrowAsync<MlInferenceUnavailableException>();
        madeException.Which.StatusCode.Should().Be(503);
        madeException.Which.DownstreamReason.Should().Be("CIRCUIT_BREAKER_OPEN");
    }

    [Fact]
    public async Task MakePredictionAsync_WhenPollyTimeoutOccurs_ThrowsUnavailableExceptionWithTimeoutReason()
    {
        // Arrange
        var madeHandler = new FakeHttpMessageHandler((req, ct) =>
            throw new TimeoutRejectedException("The execution timed out."));

        var madeSut = MakeSut(madeHandler);
        var madeRequest = MakeSampleRequest();

        // Act
        var madeAction = async () => await madeSut.MakePredictionAsync(madeRequest);

        // Assert
        var madeException = await madeAction.Should().ThrowAsync<MlInferenceUnavailableException>();
        madeException.Which.StatusCode.Should().Be(504);
        madeException.Which.DownstreamReason.Should().Be("TIMEOUT");
    }

    [Fact]
    public async Task MakeHealthCheckAsync_WhenSuccessful200_DeserializesHealthStatusCorrectly()
    {
        // Arrange
        var healthJson = """
        {
            "status": "healthy",
            "model_loaded": true,
            "model_version": "v1.0.0",
            "calibrator_method": "IsotonicRegression",
            "decision_threshold": 0.2900,
            "uptime_seconds": 1542.5,
            "memory_usage_mb": 118.2,
            "timestamp": "2026-09-30T12:00:00Z"
        }
        """;

        var madeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var res = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(healthJson, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(res);
        });

        var madeSut = MakeSut(madeHandler);

        // Act
        var madeHealth = await madeSut.MakeHealthCheckAsync();

        // Assert
        madeHealth.Should().NotBeNull();
        madeHealth.Status.Should().Be("healthy");
        madeHealth.ModelLoaded.Should().BeTrue();
        madeHealth.ModelVersion.Should().Be("v1.0.0");
        madeHealth.CalibratorMethod.Should().Be("IsotonicRegression");
        madeHealth.DecisionThreshold.Should().Be(0.2900);
        madeHealth.UptimeSeconds.Should().Be(1542.5);
        madeHealth.MemoryUsageMb.Should().Be(118.2);

        madeHandler.CapturedRequests.Should().ContainSingle();
        madeHandler.CapturedRequests[0].RequestUri!.AbsolutePath.Should().Be("/health");
    }

    [Fact]
    public async Task MakeHealthCheckAsync_WhenFailsWith500_ThrowsMlInferenceUnavailableException()
    {
        // Arrange
        var madeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var res = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Inference service startup failed", Encoding.UTF8, "text/plain")
            };
            return Task.FromResult(res);
        });

        var madeSut = MakeSut(madeHandler);

        // Act
        var madeAction = async () => await madeSut.MakeHealthCheckAsync();

        // Assert
        var madeException = await madeAction.Should().ThrowAsync<MlInferenceUnavailableException>();
        madeException.Which.StatusCode.Should().Be(500);
        madeException.Which.Message.Should().Contain("Health check failed");
    }
}
