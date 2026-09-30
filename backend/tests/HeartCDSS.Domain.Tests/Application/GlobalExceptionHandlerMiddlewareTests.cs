using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation.Results;
using HeartCDSS.Api.Middleware;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HeartCDSS.Domain.Tests.Application;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static DefaultHttpContext MakeHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/patients/encounters";
        context.TraceIdentifier = "trace-test-12345";
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationExceptionThrown_ShouldReturn400WithValidationProblemDetails()
    {
        // Arrange
        var context = MakeHttpContext();
        var failures = new List<ValidationFailure>
        {
            new("Indicators.RestingBp", "Resting systolic blood pressure must be between 60 and 260 mmHg."),
            new("Indicators.Age", "Age must be between 1 and 110 years.")
        };
        var validationException = new AppValidationException(failures);

        RequestDelegate next = _ => throw validationException;
        var middleware = new GlobalExceptionHandlerMiddleware(next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");

        var responseBody = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problemDetails.Errors.Should().ContainKey("Indicators.RestingBp");
        problemDetails.Errors.Should().ContainKey("Indicators.Age");
        problemDetails.Instance.Should().Be("/api/v1/patients/encounters");
    }

    [Fact]
    public async Task InvokeAsync_WhenDomainExceptionThrown_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        var context = MakeHttpContext();
        var domainException = new DomainException("Encounter number cannot be empty.");

        RequestDelegate next = _ => throw domainException;
        var middleware = new GlobalExceptionHandlerMiddleware(next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().Be("application/problem+json");

        var responseBody = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problemDetails.Title.Should().Be("Clinical Domain Rule Violation");
        problemDetails.Detail.Should().Be("Encounter number cannot be empty.");
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundExceptionThrown_ShouldReturn404WithProblemDetails()
    {
        // Arrange
        var context = MakeHttpContext();
        var notFoundException = new KeyNotFoundException("Patient with ID '123' was not found.");

        RequestDelegate next = _ => throw notFoundException;
        var middleware = new GlobalExceptionHandlerMiddleware(next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        context.Response.ContentType.Should().Be("application/problem+json");

        var responseBody = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.NotFound);
        problemDetails.Title.Should().Be("Resource Not Found");
        problemDetails.Detail.Should().Be("Patient with ID '123' was not found.");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_ShouldReturn500WithoutLeakingDetails()
    {
        // Arrange
        var context = MakeHttpContext();
        var unhandledException = new InvalidOperationException("Fatal database connection crash with secret credentials");

        RequestDelegate next = _ => throw unhandledException;
        var middleware = new GlobalExceptionHandlerMiddleware(next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.ContentType.Should().Be("application/problem+json");

        var responseBody = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        problemDetails.Title.Should().Be("An unexpected error occurred.");
        problemDetails.Detail.Should().NotContain("secret credentials");
    }

    [Fact]
    public async Task InvokeAsync_WhenMlInferenceValidationExceptionThrown_ShouldReturn422WithValidationProblemDetails()
    {
        // Arrange
        var context = MakeHttpContext();
        var errors = new Dictionary<string, string[]>
        {
            ["trestbps"] = ["Resting Blood Pressure (50.0 mmHg) violates physiological limits (60 - 260 mmHg)."],
            ["chol"] = ["Serum Cholesterol (750.0 mg/dL) must be within plausible limits (80 - 600 mg/dL)."]
        };
        var validationException = MlInferenceValidationException.Make(errors, "Patient clinical indicators failed physiological boundary validation.");

        RequestDelegate next = _ => throw validationException;
        var middleware = new GlobalExceptionHandlerMiddleware(next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        context.Response.ContentType.Should().Be("application/problem+json");

        var responseBody = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        problemDetails.Title.Should().Be("ML Inference Validation Error");
        problemDetails.Errors.Should().ContainKey("trestbps");
        problemDetails.Errors.Should().ContainKey("chol");
    }

    [Fact]
    public async Task InvokeAsync_WhenMlInferenceUnavailableExceptionThrown_ShouldReturn503WithProblemDetailsAndReason()
    {
        // Arrange
        var context = MakeHttpContext();
        var unavailableException = MlInferenceUnavailableException.MakeCircuitOpen();

        RequestDelegate next = _ => throw unavailableException;
        var middleware = new GlobalExceptionHandlerMiddleware(next, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        context.Response.ContentType.Should().Be("application/problem+json");

        var responseBody = await ReadResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        problemDetails.Title.Should().Be("ML Inference Service Unavailable");
        problemDetails.Detail.Should().Contain("circuit breaker is open");
        problemDetails.Extensions.Should().ContainKey("downstreamReason");
        problemDetails.Extensions["downstreamReason"]?.ToString().Should().Be("CIRCUIT_BREAKER_OPEN");
    }
}
