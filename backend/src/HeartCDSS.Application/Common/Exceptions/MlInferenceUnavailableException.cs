namespace HeartCDSS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when the ML inference microservice is unavailable, the circuit breaker is open,
/// or downstream transient failures exceed maximum retry policies.
/// </summary>
public sealed class MlInferenceUnavailableException : Exception
{
    public int StatusCode { get; }
    public string? DownstreamReason { get; }

    public MlInferenceUnavailableException(
        string message,
        Exception? innerException = null,
        int statusCode = 503,
        string? downstreamReason = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        DownstreamReason = downstreamReason;
    }

    /// <summary>
    /// Makes an exception representing an open circuit breaker.
    /// </summary>
    public static MlInferenceUnavailableException MakeCircuitOpen(Exception? inner = null) =>
        new(
            "ML inference service circuit breaker is open. Downstream microservice is temporarily unavailable.",
            inner,
            503,
            "CIRCUIT_BREAKER_OPEN");

    /// <summary>
    /// Makes an exception representing a downstream network or request timeout.
    /// </summary>
    public static MlInferenceUnavailableException MakeTimeout(Exception? inner = null) =>
        new(
            "ML inference service request timed out after resilient retry attempts.",
            inner,
            504,
            "TIMEOUT");

    /// <summary>
    /// Makes an exception representing an unavailable or degraded downstream service.
    /// </summary>
    public static MlInferenceUnavailableException MakeUnavailable(
        string reason,
        Exception? inner = null,
        int statusCode = 503) =>
        new(
            $"ML inference service is currently unavailable: {reason}",
            inner,
            statusCode,
            reason);
}
