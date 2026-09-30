# Clinical Encounter Controller & Risk Evaluation Endpoints (BACK-305)

## 1. Executive Summary & Objective

Task **BACK-305** establishes the main clinical orchestration layer of the PULSE Clinical Decision Support System (CDSS). It bridges the ASP.NET Core Web API, the MediatR application pipeline, the PostgreSQL persistence infrastructure, and the resilient FastAPI ML inference microservice into a cohesive, secure, and auditable clinical evaluation workflow.

### Key Objectives Delivered:

- **API Endpoints**:
  - `POST /api/v1/patients/{id}/encounters`: Initiates a clinical encounter, dispatches validated indicators to the ML inference service, persists the aggregate graph (`ClinicalEncounter`, `ClinicalIndicators`, `RiskAssessmentResult`, `ShapAttribution`), generates a compliance `PredictionAudit`, and returns HTTP `201` with the risk assessment payload and `Location` header.
  - `GET /api/v1/patients/{id}/encounters/{encounterId}`: Retrieves full encounter details, clinical indicators, calibrated risk probability, risk band, and SHAP feature attributions.
  - `GET /api/v1/patients/{id}/encounters`: Retrieves historical clinical encounters for a patient.
  - `POST /api/v1/auth/token`: Diagnostic and authentication endpoint issuing cryptographically signed JWT tokens for role-based testing and client integration.
- **Security & RBAC**:
  - ASP.NET Core JWT Bearer authentication registered with configurable issuer, audience, and symmetric security key.
  - Role-based authorization policies enforcing clinical role restrictions (`Physician`, `ClinicalStaff`, `Admin`, `Auditor`).
  - OpenAPI / Swagger documentation configured with Bearer security scheme.
- **Architecture & Resilience**:
  - Clean Architecture boundary preserved via `ICorrelationContext` and `IJwtTokenService` application interfaces.
  - Graceful degradation: in-flight encounter making records failure audits (`CircuitBreakerOpen`, `Timeout`, `Error`) without leaving orphaned transactions if the ML microservice degrades.
- **Verification**:
  - Complete test suite expanded to **180 passing tests** (0 warnings, 0 failures), including end-to-end integration tests using `WebApplicationFactory<Program>`.

---

## 2. API Specifications & Routing

| HTTP Method | Route                                            | Authorization Roles                              | Expected Status Codes                                                                  | Description                                                                          |
| :---------- | :----------------------------------------------- | :----------------------------------------------- | :------------------------------------------------------------------------------------- | :----------------------------------------------------------------------------------- |
| `POST`      | `/api/v1/auth/token`                             | AllowAnonymous                                   | `200 OK`, `400 Bad Request`                                                            | Issues signed JWT Bearer tokens with subject, name, and role claims                  |
| `POST`      | `/api/v1/patients/{id}/encounters`               | `Physician`, `ClinicalStaff`, `Admin`            | `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found` | Initiates encounter, executes ML inference, persists domain graph, records audit log |
| `GET`       | `/api/v1/patients/{id}/encounters/{encounterId}` | `Physician`, `ClinicalStaff`, `Admin`, `Auditor` | `200 OK`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`                         | Retrieves complete encounter details with indicators, risk scores, and SHAP drivers  |
| `GET`       | `/api/v1/patients/{id}/encounters`               | `Physician`, `ClinicalStaff`, `Admin`, `Auditor` | `200 OK`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`                         | Lists encounter history for a specified patient                                      |

---

## 3. Orchestration Lifecycle & Pipeline Flow

The encounter workflow orchestrates domain logic, external inference, and persistent audit trails through MediatR:

```
[Client / UI / EHR]
       |
       |  POST /api/v1/patients/{id}/encounters (Bearer JWT)
       v
[EncountersController.MakeEncounter]
       |
       v
[MediatR: MakeEncounterCommand]
       |
       +--> [ValidationBehavior (FluentValidation)]
       |      - Validates physiological bounds (BP: 60-260, Age: 18-120, etc.)
       |      - Fails fast with 400 Bad Request (RFC 7807 ProblemDetails)
       |
       v
[MakeEncounterCommandHandler]
       |
       +--> 1. Patient Verification:
       |       Queries IPatientRepository; throws EntityNotFoundException if missing.
       |
       +--> 2. Domain Aggregate Make:
       |       ClinicalEncounter.Make(patientId, clinicianId, encounterNumber, encounterType)
       |       Registers with IClinicalEncounterRepository.
       |
       +--> 3. Indicator Attachment:
       |       ClinicalIndicators.Make(encounterId, age, restingBp, cholesterol, ...)
       |       encounter.AttachIndicators(indicators)
       |
       +--> 4. Resilient ML Inference Dispatch (IMlInferenceClient):
       |       Builds MlInferenceRequestDto with correlation ID.
       |       Executes through Polly v8 pipeline (Timeout -> Retry backoff -> Circuit Breaker).
       |
       +--> 5. Risk Assessment & Explainability Mapping:
       |       RiskAssessmentResult.Make(encounterId, mlModelId, riskScore, riskBand, ...)
       |       Iterates SHAP drivers -> ShapAttribution.Make(featureName, attributionValue, ...)
       |       encounter.AttachRiskAssessment(riskAssessment)
       |       encounter.MarkAssessmentComplete()
       |
       +--> 6. Audit Trail Recording:
       |       PredictionAudit.Make(...) tracking raw request, response, latency, model version, status.
       |       IPredictionAuditRepository.AddAsync(auditRecord)
       |
       +--> 7. Atomic Transaction Commit:
       |       IUnitOfWork.SaveChangesAsync()
       |
       v
[201 Created Response]
  - Location: /api/v1/patients/{id}/encounters/{encounterId}
  - Payload: MakeEncounterResultDto (Risk probability, band, CI, top risk drivers)
```

---

## 4. Security & Role-Based Authorization Architecture

### 4.1 JWT Authentication Configuration

Registered in `Program.cs` via ASP.NET Core's native JWT Bearer authentication handler:

- **Token Signing**: HMAC-SHA256 with 256-bit symmetric security key.
- **Validation**: Strict issuer (`HeartCDSS.Api`), audience (`HeartCDSS.Client`), lifetime verification, and clock-skew tolerance (30s).
- **Role Mapping**: `ClaimTypes.Role` mapped from token claims into the `ClaimsPrincipal`.

### 4.2 Role Permissions Matrix

- **`Physician`**: Full read/write access. Can initiate encounters, trigger ML evaluations, and review risk assessments.
- **`ClinicalStaff`**: Full operational access for routine outpatient and emergency triage risk assessments.
- **`Admin`**: Administrative oversight and system operations.
- **`Auditor`**: Read-only clinical history and prediction review (`GET` endpoints only); write access is forbidden (`403 Forbidden`).

---

## 5. Test Suite Expansion & Verification Results

All 180 tests across domain, application, infrastructure, and API layers pass with 0 warnings:

| Test Area           | Suite / Class                                          | Test Count |     Status      |
| :------------------ | :----------------------------------------------------- | :--------: | :-------------: |
| **Domain**          | Domain Entities, Value Objects, Domain Events          |     75     |    **PASS**     |
| **Persistence**     | EF Core Postgres, Repositories, Migrations             |     24     |    **PASS**     |
| **Pipeline**        | FluentValidation Pipeline Behaviors & RFC 7807         |     28     |    **PASS**     |
| **Resilience**      | Polly v8 Resilient Inference Client                    |     32     |    **PASS**     |
| **Auth & Security** | JwtTokenService, Token Verification & Expiry           |     4      |    **PASS**     |
| **Orchestration**   | MakeEncounterCommandHandler Orchestration & Mappings   |     4      |    **PASS**     |
| **Query Handlers**  | GetEncounterById & GetPatientEncounters Query Handlers |     4      |    **PASS**     |
| **Controllers**     | EncountersController & AuthController Unit Tests       |     5      |    **PASS**     |
| **Integration**     | WebApplicationFactory End-to-End API Integration Tests |     4      |    **PASS**     |
| **Total**           | **HeartCDSS Solution Test Suite**                      |  **180**   | **PASS (100%)** |

### Verified Integration Test Scenarios:

1. `MakeEncounter_WithoutAuth_ShouldReturn401Unauthorized`: Confirms unauthenticated requests cannot trigger evaluations.
2. `MakeEncounter_WithPhysicianToken_ShouldReturn201CreatedWithRiskPayload`: Tests end-to-end successful encounter making with token, ML inference, entity persistence, and `201 Created` status with location header.
3. `MakeEncounter_WhenPhysiologicalIndicatorsViolateBounds_ShouldReturn400BadRequest`: Asserts physiological validation filters invalid clinical indicators before inference.
4. `GetEncounterById_WhenFound_ShouldReturn200OKWithDetails`: Confirms full encounter retrieval including clinical indicators and risk assessment.
5. `GetEncounterById_WhenNotFound_ShouldReturn404NotFound`: Verifies 404 response for non-existent encounters.
6. `GetPatientEncounters_WhenEncountersExist_ShouldReturn200OKWithList`: Verifies listing all encounters for a given patient.
7. `MakeEncounter_WhenPatientNotFound_ShouldReturn404NotFound`: Verifies 404 when associating an encounter with a non-existent patient.
8. `MakeEncounter_WithAuditorRole_ShouldReturn403Forbidden`: Proves role-based authorization blocks read-only roles from initiating encounters.
