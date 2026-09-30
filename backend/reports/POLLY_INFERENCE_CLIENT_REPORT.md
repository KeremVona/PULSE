# PULSE CDSS — BACK-304 Technical Report: Resilient ML Inference HTTP Client with Polly

## 1. Executive Summary

As part of **BACK-304 (Phase 3: Core Backend Orchestration & Domain Modeling)**, a resilient, enterprise-grade HTTP client was developed in ASP.NET Core 10 to interface seamlessly with the FastAPI Machine Learning Inference Microservice.

The implementation establishes a clean architectural boundary between the Application and Infrastructure layers, implements modern Polly v8 resilience pipelines via `Microsoft.Extensions.Http.Resilience`, enforces distributed tracing correlation forwarding via `X-Correlation-Id`, and maps structured RFC 7807 problem details across all validation and downstream failure states.

---

## 2. Architecture & Layer Separation

```
┌────────────────────────────────────────────────────────────────────────┐
│ HeartCDSS.Application                                                  │
│                                                                        │
│   ├── IMlInferenceClient (Interface)                                   │
│   │     ├── MakePredictionAsync(request, ct)                           │
│   │     └── MakeHealthCheckAsync(ct)                                   │
│   │                                                                    │
│   ├── Common/Models/MlInferenceDtos.cs                                 │
│   │     ├── MlInferenceRequestDto                                      │
│   │     ├── MlIndicatorsPayloadDto                                     │
│   │     ├── MlInferenceResultDto                                       │
│   │     ├── MlConfidenceIntervalDto                                    │
│   │     ├── MlFeatureAttributionDto                                    │
│   │     └── MlHealthCheckResultDto                                     │
│   │                                                                    │
│   └── Common/Exceptions/                                               │
│         ├── MlInferenceUnavailableException (HTTP 503 / Circuit Open)   │
│         └── MlInferenceValidationException  (HTTP 422 / Bound Viol.)    │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Implements
┌───────────────────────────────────▼────────────────────────────────────┐
│ HeartCDSS.Infrastructure                                               │
│                                                                        │
│   ├── Clients/FastApiInferenceClient.cs                                │
│   │     ├── Dispatches POST /api/v1/predict                            │
│   │     ├── Dispatches GET /health                                     │
│   │     ├── Injects X-Correlation-Id distributed trace header          │
│   │     └── Translates Polly/HTTP exceptions to Application contracts │
│   │                                                                    │
│   └── DependencyInjection.cs                                           │
│         └── AddStandardResilienceHandler (Polly v8 Pipeline)           │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ HTTP over Wire
┌───────────────────────────────────▼────────────────────────────────────┐
│ FastAPI Inference Engine (Port 8000)                                   │
│   POST /api/v1/predict  <--- Model Calibrator + TreeExplainer          │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Core Deliverables & Technical Details

### 3.1 Interface & Contracts (`HeartCDSS.Application`)

- **Interface:** [`IMlInferenceClient`](file:///F:/Code/Projects/PULSE/backend/src/HeartCDSS.Application/Common/Interfaces/IMlInferenceClient.cs)

  ```csharp
  public interface IMlInferenceClient
  {
      Task<MlInferenceResultDto> MakePredictionAsync(
          MlInferenceRequestDto request,
          CancellationToken cancellationToken = default);

      Task<MlHealthCheckResultDto> MakeHealthCheckAsync(
          CancellationToken cancellationToken = default);
  }
  ```

- **DTO Mapping Models:** [`MlInferenceDtos.cs`](file:///F:/Code/Projects/PULSE/backend/src/HeartCDSS.Application/Common/Models/MlInferenceDtos.cs)
  - `MlIndicatorsPayloadDto`: Enforces the 14 standard physiological features matching the FastAPI Pydantic schema (`age`, `sex`, `cp`, `trestbps`, `chol`, `fbs`, `restecg`, `thalach`, `exang`, `oldpeak`, `slope`, `ca`, `thal`) using explicit `[JsonPropertyName(...)]` snake_case attributes.
  - Domain Conversion Methods:
    - `MakeFromInputDto(ClinicalIndicatorsInputDto input)`
    - `MakeFromDomain(ClinicalIndicators indicators)`
    - `MakeRiskAssessmentResult(Guid encounterId, Guid modelId)`
    - `MakeDomainConfidenceInterval()`
    - `MakeDomainShapAttribution(Guid riskAssessmentResultId)`

### 3.2 Concrete Client Implementation (`HeartCDSS.Infrastructure`)

- **Client:** [`FastApiInferenceClient`](file:///F:/Code/Projects/PULSE/backend/src/HeartCDSS.Infrastructure/Clients/FastApiInferenceClient.cs)
  - Transmits JSON payloads over HTTP with `application/json` Accept and Content-Type headers.
  - Automatically resolves and injects `X-Correlation-Id` header from request payload, active `IHttpContextAccessor`, or generates an immutable UUID.
  - Implements defensive error translation:
    - `BrokenCircuitException` → `MlInferenceUnavailableException.MakeCircuitOpen()`
    - `TimeoutRejectedException` / canceled requests → `MlInferenceUnavailableException.MakeTimeout()`
    - HTTP 422 Unprocessable Entity → `MlInferenceValidationException.Make(errors, detail)`
    - HTTP 503/502/504 / Network failures → `MlInferenceUnavailableException.MakeUnavailable(...)`

### 3.3 Polly Resilience Pipeline Configuration

Configured in [`DependencyInjection.cs`](file:///F:/Code/Projects/PULSE/backend/src/HeartCDSS.Infrastructure/DependencyInjection.cs) using Microsoft's standard resilience pipeline:

| Policy Component          | Setting / Parameter                                                                        | Clinical Rationale                                                                        |
| :------------------------ | :----------------------------------------------------------------------------------------- | :---------------------------------------------------------------------------------------- |
| **Total Request Timeout** | `5000 ms`                                                                                  | Prevents blocking HTTP requests during catastrophic downstream hang.                      |
| **Attempt Timeout**       | `5000 ms`                                                                                  | Bounds individual invocation attempts.                                                    |
| **Retry Strategy**        | `3 max attempts`, Exponential backoff, jitter enabled, `200 ms` initial delay              | Absorbs transient FastAPI restart/container migration blips without human intervention.   |
| **Retry Condition**       | Transient 5xx, 408 Request Timeout, network exceptions                                     | **422 Unprocessable Entity is NOT retried** (fails fast).                                 |
| **Circuit Breaker**       | `SamplingDuration: 10s`, `FailureRatio: 0.5`, `MinimumThroughput: 5`, `BreakDuration: 15s` | Protects ML container under severe saturation, failing fast to prevent thread starvation. |

### 3.4 API Middleware RFC 7807 ProblemDetails Integration

[`GlobalExceptionHandlerMiddleware.cs`](file:///F:/Code/Projects/PULSE/backend/src/HeartCDSS.Api/Middleware/GlobalExceptionHandlerMiddleware.cs) catches and formats ML inference exceptions:

- **`MlInferenceUnavailableException`:** Yields **HTTP 503 Service Unavailable** `ProblemDetails` with `title: "ML Inference Service Unavailable"`, `traceId`, and downstream reason (`CIRCUIT_BREAKER_OPEN`, `TIMEOUT`).
- **`MlInferenceValidationException`:** Yields **HTTP 422 Unprocessable Entity** `ValidationProblemDetails` with `title: "ML Inference Validation Error"` and structured dictionary of physiological field errors.

---

## 4. Test Verification & Quality Gates

The test suite was expanded with 19 comprehensive tests across DTOs, the client, Polly resilience, and middleware:

1. **`MlInferenceDtosTests` (5 tests):**
   - Verified mapping from domain `ClinicalIndicators` to `MlIndicatorsPayloadDto`.
   - Verified mapping from `ClinicalIndicatorsInputDto` to `MlIndicatorsPayloadDto`.
   - Verified serialization to snake_case JSON matching FastAPI Pydantic schema.
   - Verified deserialization of full FastAPI prediction payload (probability, band, CI, drivers, warnings).
   - Verified conversion from DTO to domain `RiskAssessmentResult` and `ShapAttribution` entities.

2. **`FastApiInferenceClientTests` (9 tests):**
   - Verified HTTP 200 OK handling and response deserialization.
   - Verified `X-Correlation-Id` header propagation and payload mapping.
   - Verified automatic correlation ID generation when omitted.
   - Verified HTTP 422 parsing into `MlInferenceValidationException`.
   - Verified HTTP 503 and 500 error translation into `MlInferenceUnavailableException`.
   - Verified `BrokenCircuitException` translation to HTTP 503 `CIRCUIT_BREAKER_OPEN`.
   - Verified `TimeoutRejectedException` translation to HTTP 504 `TIMEOUT`.
   - Verified GET `/health` success and error handling.

3. **`PollyResiliencePipelineTests` (3 tests):**
   - Verified transient HTTP 503 retry recovery (1 initial failure + 1 successful retry = 2 attempts).
   - Verified exhaustion of retries on persistent 503 (1 initial + 3 retries = 4 total attempts).
   - Verified fast-fail without retries on HTTP 422 validation errors (1 attempt only).

4. **`GlobalExceptionHandlerMiddlewareTests` (2 tests updated):**
   - Verified HTTP 422 `ValidationProblemDetails` payload structure.
   - Verified HTTP 503 `ProblemDetails` payload structure with `downstreamReason`.

### Test Execution Summary

```
Total Test Files: 1
Passed:   159
Failed:   0
Skipped:  0
Duration: 1.0 s
Compiler Warnings: 0
Compiler Errors:   0
```
