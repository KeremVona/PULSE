using System.Net;
using System.Text.Json;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HeartCDSS.Api.Middleware;

/// <summary>
/// Global exception handling middleware capturing validation, domain, and unhandled errors,
/// returning standardized RFC 7807 ProblemDetails and ValidationProblemDetails payloads.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failure at {Path}: {Count} fields failed.", context.Request.Path, ex.Errors.Count);
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (MlInferenceValidationException ex)
        {
            _logger.LogWarning(ex, "ML inference validation failure at {Path}: {Count} fields rejected.", context.Request.Path, ex.Errors.Count);
            await HandleMlInferenceValidationExceptionAsync(context, ex);
        }
        catch (MlInferenceUnavailableException ex)
        {
            _logger.LogError(ex, "ML inference service unavailable at {Path}: {Reason}", context.Request.Path, ex.DownstreamReason ?? ex.Message);
            await HandleMlInferenceUnavailableExceptionAsync(context, ex);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation at {Path}: {Message}", context.Request.Path, ex.Message);
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogInformation(ex, "Resource not found at {Path}: {Message}", context.Request.Path, ex.Message);
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled server exception at {Path}", context.Request.Path);
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, AppValidationException exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var problemDetails = new ValidationProblemDetails(exception.Errors.ToDictionary(k => k.Key, v => v.Value))
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "Clinical and physiological indicators failed validation boundary rules.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleDomainExceptionAsync(HttpContext context, DomainException exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Clinical Domain Rule Violation",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Resource Not Found",
            Status = (int)HttpStatusCode.NotFound,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleMlInferenceValidationExceptionAsync(HttpContext context, MlInferenceValidationException exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;

        var madeProblemDetails = new ValidationProblemDetails(exception.Errors.ToDictionary(k => k.Key, v => v.Value))
        {
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
            Title = "ML Inference Validation Error",
            Status = (int)HttpStatusCode.UnprocessableEntity,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        madeProblemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var madeJson = JsonSerializer.Serialize(madeProblemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(madeJson);
    }

    private static async Task HandleMlInferenceUnavailableExceptionAsync(HttpContext context, MlInferenceUnavailableException exception)
    {
        context.Response.ContentType = "application/problem+json";
        var statusCode = exception.StatusCode > 0 ? exception.StatusCode : (int)HttpStatusCode.ServiceUnavailable;
        context.Response.StatusCode = statusCode;

        var madeProblemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            Title = "ML Inference Service Unavailable",
            Status = statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        madeProblemDetails.Extensions["traceId"] = context.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(exception.DownstreamReason))
        {
            madeProblemDetails.Extensions["downstreamReason"] = exception.DownstreamReason;
        }

        var madeJson = JsonSerializer.Serialize(madeProblemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(madeJson);
    }

    private static async Task HandleUnhandledExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An unexpected error occurred.",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "An unexpected server error occurred while processing the clinical request.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
