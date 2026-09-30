# Backend - Enterprise Orchestration Layer

ASP.NET Core Web API service adhering to Clean Architecture principles.

## Scope & Responsibilities

- **Domain & Application**: Encapsulated clinical entities (`Patient`, `ClinicalEncounter`, `PredictionAudit`), MediatR command/query pipeline.
- **Validation**: FluentValidation pipeline enforcing physiological boundary checks.
- **Infrastructure**: Entity Framework Core with PostgreSQL (including JSONB column for audit trails and SHAP vectors).
- **Resilience**: `IMlInferenceClient` powered by `HttpClientFactory` and Polly resilience policies (retries with exponential backoff, circuit breaking).
- **Authentication**: JWT bearer token authentication and role-based access control (RBAC).
