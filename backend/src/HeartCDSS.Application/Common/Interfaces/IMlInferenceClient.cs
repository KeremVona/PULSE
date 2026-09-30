using HeartCDSS.Application.Common.Models;

namespace HeartCDSS.Application.Common.Interfaces;

/// <summary>
/// Interface for communicating with the external FastAPI machine learning inference microservice.
/// Implementations must enforce resilient HTTP communication with exponential backoff retries,
/// timeouts, and circuit breaker policies.
/// </summary>
public interface IMlInferenceClient
{
    /// <summary>
    /// Dispatches clinical indicators to the ML inference engine to evaluate cardiovascular risk,
    /// compute calibrated posterior probabilities with 95% confidence intervals, and extract SHAP attributions.
    /// </summary>
    /// <param name="request">Structured inference request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive CDSS prediction and explainability result.</returns>
    Task<MlInferenceResultDto> MakePredictionAsync(
        MlInferenceRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the health, readiness, and artifact loading status of the ML inference engine.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ML service health details.</returns>
    Task<MlHealthCheckResultDto> MakeHealthCheckAsync(
        CancellationToken cancellationToken = default);
}
