using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace HeartCDSS.Infrastructure.Clients;

/// <summary>
/// Resilient HTTP client communicating with the FastAPI ML inference service.
/// Enforces correlation ID forwarding, RFC 7807 error translation, and Polly resilience handling.
/// </summary>
public sealed class FastApiInferenceClient : IMlInferenceClient
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private readonly HttpClient _httpClient;
    private readonly ILogger<FastApiInferenceClient> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;

    public FastApiInferenceClient(
        HttpClient httpClient,
        ILogger<FastApiInferenceClient> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor;
        _jsonOptions = MakeJsonSerializerOptions();
    }

    public async Task<MlInferenceResultDto> MakePredictionAsync(
        MlInferenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var madeCorrelationId = ResolveCorrelationId(request.CorrelationId);
        var madeRequestDto = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? request with { CorrelationId = madeCorrelationId }
            : request;

        _logger.LogInformation(
            "Dispatching ML inference request for Encounter: {EncounterId}, Correlation: {CorrelationId}",
            madeRequestDto.EncounterId,
            madeCorrelationId);

        using var madeHttpRequest = MakeHttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/predict",
            madeRequestDto,
            madeCorrelationId);

        try
        {
            using var madeHttpResponse = await _httpClient.SendAsync(madeHttpRequest, cancellationToken);
            return await MakePredictionResponseAsync(madeHttpResponse, madeCorrelationId, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker open during ML prediction call (Correlation: {CorrelationId})", madeCorrelationId);
            throw MlInferenceUnavailableException.MakeCircuitOpen(ex);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "ML prediction call timed out in Polly pipeline (Correlation: {CorrelationId})", madeCorrelationId);
            throw MlInferenceUnavailableException.MakeTimeout(ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "ML prediction call timed out (Correlation: {CorrelationId})", madeCorrelationId);
            throw MlInferenceUnavailableException.MakeTimeout(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network failure reaching ML inference engine (Correlation: {CorrelationId})", madeCorrelationId);
            throw MlInferenceUnavailableException.MakeUnavailable(
                $"Network communication failure with ML inference service: {ex.Message}",
                ex,
                (int)(ex.StatusCode ?? HttpStatusCode.ServiceUnavailable));
        }
    }

    public async Task<MlHealthCheckResultDto> MakeHealthCheckAsync(
        CancellationToken cancellationToken = default)
    {
        var madeCorrelationId = ResolveCorrelationId(null);

        using var madeHttpRequest = MakeHttpRequestMessage(
            HttpMethod.Get,
            "/health",
            null,
            madeCorrelationId);

        try
        {
            using var madeHttpResponse = await _httpClient.SendAsync(madeHttpRequest, cancellationToken);

            if (!madeHttpResponse.IsSuccessStatusCode)
            {
                var madeErrorBody = await madeHttpResponse.Content.ReadAsStringAsync(cancellationToken);
                throw MlInferenceUnavailableException.MakeUnavailable(
                    $"Health check failed with status {(int)madeHttpResponse.StatusCode}: {madeErrorBody}",
                    statusCode: (int)madeHttpResponse.StatusCode);
            }

            var madeContent = await madeHttpResponse.Content.ReadAsStringAsync(cancellationToken);
            var madeHealth = JsonSerializer.Deserialize<MlHealthCheckResultDto>(madeContent, _jsonOptions);

            return madeHealth ?? throw MlInferenceUnavailableException.MakeUnavailable("Received null health check payload from ML service.");
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker open during ML health check call");
            throw MlInferenceUnavailableException.MakeCircuitOpen(ex);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "ML health check call timed out in Polly pipeline");
            throw MlInferenceUnavailableException.MakeTimeout(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network failure reaching ML service health endpoint");
            throw MlInferenceUnavailableException.MakeUnavailable(
                $"Failed to connect to ML health endpoint: {ex.Message}",
                ex,
                (int)(ex.StatusCode ?? HttpStatusCode.ServiceUnavailable));
        }
    }

    private async Task<MlInferenceResultDto> MakePredictionResponseAsync(
        HttpResponseMessage madeHttpResponse,
        string madeCorrelationId,
        CancellationToken cancellationToken)
    {
        var madeContent = await madeHttpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (madeHttpResponse.IsSuccessStatusCode)
        {
            var madeResult = JsonSerializer.Deserialize<MlInferenceResultDto>(madeContent, _jsonOptions);
            if (madeResult == null)
            {
                throw MlInferenceUnavailableException.MakeUnavailable("Received null prediction payload from ML inference service.");
            }

            return madeResult;
        }

        if (madeHttpResponse.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            _logger.LogWarning(
                "ML service rejected payload with 422 Unprocessable Entity (Correlation: {CorrelationId}): {Content}",
                madeCorrelationId,
                madeContent);

            var madeValidationException = MakeValidationExceptionFromContent(madeContent);
            throw madeValidationException;
        }

        if (madeHttpResponse.StatusCode == HttpStatusCode.ServiceUnavailable ||
            madeHttpResponse.StatusCode == HttpStatusCode.BadGateway ||
            madeHttpResponse.StatusCode == HttpStatusCode.GatewayTimeout)
        {
            _logger.LogError(
                "ML service unavailable with status {StatusCode} (Correlation: {CorrelationId}): {Content}",
                madeHttpResponse.StatusCode,
                madeCorrelationId,
                madeContent);

            throw MlInferenceUnavailableException.MakeUnavailable(
                $"ML inference service returned {(int)madeHttpResponse.StatusCode} ({madeHttpResponse.StatusCode}): {madeContent}",
                statusCode: (int)madeHttpResponse.StatusCode);
        }

        _logger.LogError(
            "ML service returned unexpected HTTP {StatusCode} (Correlation: {CorrelationId}): {Content}",
            madeHttpResponse.StatusCode,
            madeCorrelationId,
            madeContent);

        throw MlInferenceUnavailableException.MakeUnavailable(
            $"ML inference service returned unexpected HTTP {(int)madeHttpResponse.StatusCode}: {madeContent}",
            statusCode: (int)madeHttpResponse.StatusCode);
    }

    private static MlInferenceValidationException MakeValidationExceptionFromContent(string jsonContent)
    {
        var madeErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var madeDetailMessage = "ML inference rejected the request due to clinical validation boundary violations.";

        try
        {
            using var madeDoc = JsonDocument.Parse(jsonContent);
            var madeRoot = madeDoc.RootElement;

            if (madeRoot.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
            {
                madeDetailMessage = detailProp.GetString() ?? madeDetailMessage;
            }

            if (madeRoot.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
            {
                var errorList = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var errElem in errorsProp.EnumerateArray())
                {
                    var msg = errElem.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() ?? "Invalid value" : "Invalid value";
                    var field = "indicators";

                    if (errElem.TryGetProperty("loc", out var locProp) && locProp.ValueKind == JsonValueKind.Array)
                    {
                        var locParts = locProp.EnumerateArray().Select(x => x.ToString()).ToList();
                        field = locParts.LastOrDefault() ?? "indicators";
                    }

                    if (!errorList.TryGetValue(field, out var list))
                    {
                        list = [];
                        errorList[field] = list;
                    }
                    list.Add(msg);
                }

                foreach (var kvp in errorList)
                {
                    madeErrors[kvp.Key] = kvp.Value.ToArray();
                }
            }
        }
        catch
        {
            madeErrors["payload"] = [jsonContent];
        }

        return MlInferenceValidationException.Make(madeErrors, madeDetailMessage);
    }

    private HttpRequestMessage MakeHttpRequestMessage(
        HttpMethod method,
        string uri,
        object? payload,
        string correlationId)
    {
        var madeMessage = new HttpRequestMessage(method, uri);
        madeMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        madeMessage.Headers.Add(CorrelationHeader, correlationId);

        if (payload != null)
        {
            var madeJson = JsonSerializer.Serialize(payload, _jsonOptions);
            madeMessage.Content = new StringContent(madeJson, Encoding.UTF8, "application/json");
        }

        return madeMessage;
    }

    private string ResolveCorrelationId(string? requestCorrelationId)
    {
        if (!string.IsNullOrWhiteSpace(requestCorrelationId))
        {
            return requestCorrelationId;
        }

        if (_httpContextAccessor?.HttpContext?.Request.Headers.TryGetValue(CorrelationHeader, out var headerVal) == true
            && !string.IsNullOrWhiteSpace(headerVal))
        {
            return headerVal.ToString();
        }

        return Guid.NewGuid().ToString();
    }

    private static JsonSerializerOptions MakeJsonSerializerOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true
        };
}
