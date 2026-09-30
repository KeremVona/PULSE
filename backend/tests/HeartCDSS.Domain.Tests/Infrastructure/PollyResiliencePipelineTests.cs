using System.Net;
using System.Text;
using FluentAssertions;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Application.Common.Models;
using HeartCDSS.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Xunit;

namespace HeartCDSS.Domain.Tests.Infrastructure;

public class PollyResiliencePipelineTests
{
    private sealed class CountingFakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _responseProvider;

        public int AttemptCount { get; private set; }

        public CountingFakeHttpMessageHandler(Func<int, HttpResponseMessage> responseProvider)
        {
            _responseProvider = responseProvider;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AttemptCount++;
            return Task.FromResult(_responseProvider(AttemptCount));
        }
    }

    private static MlInferenceRequestDto MakeSampleRequest()
    {
        var madeIndicators = MlIndicatorsPayloadDto.Make(
            age: 50,
            sex: 1,
            cp: 1,
            trestbps: 120,
            chol: 200,
            fbs: 0,
            restecg: 0,
            thalach: 150,
            exang: 0,
            oldpeak: 0.0,
            slope: 1,
            ca: 0,
            thal: 3
        );

        return MlInferenceRequestDto.Make(
            indicators: madeIndicators,
            encounterId: "enc-polly-test",
            correlationId: "trace-polly-test",
            patientMrn: "MRN-1234"
        );
    }

    private static HttpResponseMessage MakeSuccessResponse()
    {
        var json = """
        {
            "encounter_id": "enc-polly-test",
            "correlation_id": "trace-polly-test",
            "risk_probability": 0.15,
            "risk_band": "Low",
            "is_cad_positive": false,
            "decision_threshold": 0.29,
            "confidence_interval": {
                "lower": 0.10,
                "upper": 0.22,
                "confidence_level": 0.95,
                "margin_percentage": 6.0
            },
            "top_risk_drivers": [],
            "top_protective_factors": [],
            "all_attributions": [],
            "explanations": [],
            "clinical_warnings": [],
            "recommended_action": "Standard clinical follow-up.",
            "clinical_disclaimer": "Screening aid only.",
            "inference_latency_ms": 9.5
        }
        """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static IServiceProvider MakeServiceProviderWithResilience(
        CountingFakeHttpMessageHandler madeHandler,
        int retryCount = 3,
        int delayMs = 10)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        services.AddHttpClient<IMlInferenceClient, FastApiInferenceClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:8000");
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
        .ConfigurePrimaryHttpMessageHandler(() => madeHandler)
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);

            options.Retry.MaxRetryAttempts = retryCount;
            options.Retry.BackoffType = DelayBackoffType.Constant;
            options.Retry.Delay = TimeSpan.FromMilliseconds(delayMs);
            options.Retry.UseJitter = false;

            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task MakeResiliencePipeline_WhenInitialCallFails503ThenSucceeds_RetriesAndReturnsSuccessfulResult()
    {
        // Arrange: 1st attempt returns 503, 2nd attempt returns 200 OK
        var madeHandler = new CountingFakeHttpMessageHandler(attempt =>
        {
            if (attempt == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("Temporarily unavailable", Encoding.UTF8, "text/plain")
                };
            }

            return MakeSuccessResponse();
        });

        var madeProvider = MakeServiceProviderWithResilience(madeHandler, retryCount: 3, delayMs: 5);
        var madeClient = madeProvider.GetRequiredService<IMlInferenceClient>();
        var madeRequest = MakeSampleRequest();

        // Act
        var madeResult = await madeClient.MakePredictionAsync(madeRequest);

        // Assert
        madeResult.Should().NotBeNull();
        madeResult.RiskProbability.Should().Be(0.15);
        madeResult.RiskBand.Should().Be("Low");
        madeHandler.AttemptCount.Should().Be(2); // 1 initial failure + 1 successful retry
    }

    [Fact]
    public async Task MakeResiliencePipeline_WhenAllAttemptsFail503_RetriesUpToConfiguredRetryCountAndThrows()
    {
        // Arrange: Always returns 503 Service Unavailable
        var madeHandler = new CountingFakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Downstream down", Encoding.UTF8, "text/plain")
            });

        const int retryCount = 3;
        var madeProvider = MakeServiceProviderWithResilience(madeHandler, retryCount: retryCount, delayMs: 5);
        var madeClient = madeProvider.GetRequiredService<IMlInferenceClient>();
        var madeRequest = MakeSampleRequest();

        // Act
        var madeAction = async () => await madeClient.MakePredictionAsync(madeRequest);

        // Assert: 1 initial attempt + 3 retries = 4 total attempts
        var madeException = await madeAction.Should().ThrowAsync<MlInferenceUnavailableException>();
        madeException.Which.StatusCode.Should().Be(503);
        madeHandler.AttemptCount.Should().Be(retryCount + 1);
    }

    [Fact]
    public async Task MakeResiliencePipeline_WhenValidation422Occurs_DoesNotRetry()
    {
        // Arrange: Validation failure (422) is non-transient, should NOT be retried
        var errorJson = """
        {
            "detail": "Validation error",
            "errors": [
                {
                    "loc": ["body", "age"],
                    "msg": "Invalid age",
                    "type": "value_error"
                }
            ]
        }
        """;

        var madeHandler = new CountingFakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
            });

        var madeProvider = MakeServiceProviderWithResilience(madeHandler, retryCount: 3, delayMs: 5);
        var madeClient = madeProvider.GetRequiredService<IMlInferenceClient>();
        var madeRequest = MakeSampleRequest();

        // Act
        var madeAction = async () => await madeClient.MakePredictionAsync(madeRequest);

        // Assert: Should fail immediately without retrying (AttemptCount == 1)
        await madeAction.Should().ThrowAsync<MlInferenceValidationException>();
        madeHandler.AttemptCount.Should().Be(1);
    }
}
