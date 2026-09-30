namespace HeartCDSS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when the ML inference microservice rejects a payload due to physiological
/// validation or schema constraint violations (HTTP 422).
/// </summary>
public sealed class MlInferenceValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public MlInferenceValidationException(
        string message,
        IReadOnlyDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>
    /// Makes a validation exception instance from structured field errors.
    /// </summary>
    public static MlInferenceValidationException Make(
        IReadOnlyDictionary<string, string[]> errors,
        string? customMessage = null) =>
        new(
            customMessage ?? "ML inference service rejected the request due to clinical validation boundary violations.",
            errors);
}
