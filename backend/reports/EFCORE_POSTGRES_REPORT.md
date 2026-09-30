# Technical Report: BACK-302 EF Core PostgreSQL Configuration and Audit Logging

**Project:** PULSE — Clinical Decision Support System (CDSS) for Cardiovascular Risk Stratification & Explainable AI  
**Module:** Phase 3 — Backend Development (`HeartCDSS.Infrastructure`)  
**Task:** BACK-302: EF Core PostgreSQL Configuration and Audit Logging  
**Date:** September 30, 2026  
**Status:** Completed  

---

## 1. Executive Summary

Task **BACK-302** has delivered the complete relational persistence layer for the PULSE CDSS, implemented using **Entity Framework Core 10.0.4** and **Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3**. All domain aggregates and entities specified in BACK-301 are now fully mapped with explicit table configurations, owned value objects, cascading semantics, global soft-delete query filters, and database audit hooks.

Crucially for clinical accountability and regulatory compliance (FDA/MDR requirements for AI-enabled SaMD), the `PredictionAudit` entity persists immutable inference request/response payloads and raw SHAP feature vectors in PostgreSQL native `jsonb` columns, backed by Generalized Inverted Indexes (`GIN`) for low-latency audit trail querying.

The initial database migration has been scaffolded under the specified name **`InitialMake`** (`20260930113039_InitialMake.cs`), producing verified idempotent PostgreSQL DDL. A 91-test test suite confirms complete domain integrity, entity-relational mapping, and repository query behavior.

---

## 2. Architecture & Components

```
HeartCDSS.Infrastructure/
├── Persistence/
│   ├── HeartCDSSDbContext.cs                 # DbContext with ChangeTracker audit hooks & IUnitOfWork
│   ├── Configurations/                       # IEntityTypeConfiguration<T> implementations
│   │   ├── PatientConfiguration.cs           # MRN uniqueness, demographics, soft-delete filter
│   │   ├── ClinicalEncounterConfiguration.cs # 1:N encounters, status conversions, clinician index
│   │   ├── ClinicalIndicatorsConfiguration.cs# 1:1 encounter, owned BloodPressure, Cholesterol, Ecg
│   │   ├── RiskAssessmentResultConfiguration.cs # Owned ConfidenceInterval, 1:N ShapAttributions
│   │   ├── ShapAttributionConfiguration.cs   # Ranked SHAP feature impact vectors
│   │   ├── PredictionAuditConfiguration.cs   # JSONB payloads with GIN indexing, correlation ID
│   │   ├── MlModelConfiguration.cs           # Active model version registry, calibration metadata
│   │   └── UserConfiguration.cs              # User entity and Role configuration + seed data
│   ├── Repositories/                         # Repository implementations
│   │   ├── PatientRepository.cs              # IPatientRepository implementation
│   │   ├── ClinicalEncounterRepository.cs    # IClinicalEncounterRepository implementation
│   │   ├── RiskAssessmentRepository.cs       # IRiskAssessmentRepository implementation
│   │   └── PredictionAuditRepository.cs      # IPredictionAuditRepository implementation
│   └── Migrations/
│       ├── 20260930113039_InitialMake.cs     # Scaffolding migration InitialMake
│       ├── 20260930113039_InitialMake.Designer.cs
│       └── HeartCDSSDbContextModelSnapshot.cs
└── DependencyInjection.cs                   # AddInfrastructure DI registration
```

---

## 3. Entity-Relational Mappings

### 3.1 Aggregates & Owned Value Objects

| Entity / Aggregate | Table Name | Key Mapping Highlights |
| :--- | :--- | :--- |
| `Patient` | `patients` | `mrn` unique index (`WHERE is_deleted = FALSE`), global soft-delete query filter, composite index `(last_name, first_name)`. |
| `ClinicalEncounter` | `clinical_encounters` | Foreign key `patient_id` with `DeleteBehavior.Restrict` to protect patient history, unique `encounter_number`, index on `(patient_id, encounter_date)`. |
| `ClinicalIndicators` | `clinical_indicators` | 1:1 unique foreign key on `encounter_id` with `Cascade` delete. Maps 3 owned value objects: `BloodPressure` (`resting_bp`, `diastolic_bp`), `CholesterolReading` (`serum_cholesterol`, `fasting_blood_sugar`), and `EcgResult` (`resting_ecg`, `max_heart_rate`, `exercise_angina`, `oldpeak_st_depression`, `st_slope`). |
| `RiskAssessmentResult`| `risk_assessments` | Maps owned `ConfidenceInterval` (`ci_lower_bound`, `ci_upper_bound`, `ci_confidence_level`, `ci_margin_percentage`), foreign key to encounter, 1:N attributions. |
| `ShapAttribution` | `shap_attributions` | Foreign key to `risk_assessments` with `Cascade` delete. Indexes on `(risk_assessment_id, rank_order)` and `(risk_assessment_id, impact_direction)`. |
| `PredictionAudit` | `prediction_audits` | Unique index on `correlation_id`, index on `encounter_id`, index on `made_at`. Native `jsonb` columns with `GIN` indexes. |
| `MlModel` | `ml_models` | Unique index on `model_version`, filtered index on `is_active = TRUE`. |
| `User` & `Role` | `users`, `roles`, `user_roles` | Normalized many-to-many relationship via join entity `user_roles`. Seed data for 4 clinical roles (`Physician`, `ClinicalStaff`, `Admin`, `Auditor`). |

---

## 4. PostgreSQL JSONB & GIN Index Verification

To ensure fast search across raw prediction inputs, outputs, and feature explanations, `PredictionAuditConfiguration` configures PostgreSQL GIN indexes:

```csharp
builder.Property(a => a.RequestPayload)
    .HasColumnName("request_payload")
    .HasColumnType("jsonb")
    .IsRequired();

builder.Property(a => a.ResponsePayload)
    .HasColumnName("response_payload")
    .HasColumnType("jsonb");

builder.Property(a => a.ExplainingFeatures)
    .HasColumnName("explaining_features")
    .HasColumnType("jsonb");

builder.HasIndex(a => a.RequestPayload)
    .HasDatabaseName("idx_prediction_audits_req_json")
    .HasMethod("gin");

builder.HasIndex(a => a.ResponsePayload)
    .HasDatabaseName("idx_prediction_audits_res_json")
    .HasMethod("gin");

builder.HasIndex(a => a.ExplainingFeatures)
    .HasDatabaseName("idx_prediction_audits_explaining_json")
    .HasMethod("gin");
```

### Verified Generated SQL DDL

From `dotnet ef migrations script --idempotent`:
```sql
CREATE INDEX idx_prediction_audits_explaining_json ON prediction_audits USING gin (explaining_features);
CREATE INDEX idx_prediction_audits_req_json ON prediction_audits USING gin (request_payload);
CREATE INDEX idx_prediction_audits_res_json ON prediction_audits USING gin (response_payload);
```

---

## 5. Repository Implementations

The four repository implementations adhere to Clean Architecture principles:
- **`PatientRepository` (`IPatientRepository`):** Provides MRN lookup, name prefix search, MRN collision detection (`ExistsWithMrnAsync`), and active patient querying respecting soft deletion.
- **`ClinicalEncounterRepository` (`IClinicalEncounterRepository`):** Implements `GetWithDetailsAsync` eagerly loading `Indicators`, `RiskAssessments`, and `Attributions` via `AsSplitQuery()` for performance.
- **`RiskAssessmentRepository` (`IRiskAssessmentRepository`):** Implements `GetLatestByEncounterIdAsync` ordering by `AssessedAt DESC` with eager loading of `Attributions`.
- **`PredictionAuditRepository` (`IPredictionAuditRepository`):** Provides query access by `CorrelationId` and `EncounterId` with append-only guarantees.

---

## 6. Migration Scaffolding (`InitialMake`)

As per user instruction, the migration was explicitly generated with the name **`InitialMake`**:
- Target migration file: `src/HeartCDSS.Infrastructure/Persistence/Migrations/20260930113039_InitialMake.cs`
- Model snapshot: `HeartCDSSDbContextModelSnapshot.cs`
- All tables, constraints, foreign keys, and indexes compile with **0 Warnings** and **0 Errors**.

---

## 7. Automated Test Verification

Testing was executed using **xUnit** and **FluentAssertions** with `Microsoft.EntityFrameworkCore.InMemory`:
- **Domain Entity & Value Object Tests:** 83 tests passing
- **EF Core Persistence & Value Object Mapping Tests:** 4 tests passing
  - `CanInsertAndRetrievePatient_WithSoftDeleteFilter`
  - `CanPersistEncounter_WithClinicalIndicatorsAndOwnedValueObjects`
  - `CanPersistRiskAssessment_WithConfidenceIntervalAndShapAttributions`
  - `CanPersistPredictionAudit_WithRawJsonPayloadsAndExplainingFeatures`
- **Repository Pattern Tests:** 4 tests passing
  - `PatientRepository_AddAndQuery_Succeeds`
  - `ClinicalEncounterRepository_GetWithDetails_LoadsGraph`
  - `RiskAssessmentRepository_AddAndQuery_Succeeds`
  - `PredictionAuditRepository_AddAndQuery_Succeeds`
- **Total Suite:** **91 passed, 0 failed, 0 skipped** (2.0s duration).

---

## 8. Acceptance Criteria Checklist

- [x] `HeartCDSSDbContext` implementing `IUnitOfWork` and entity configurations via `IEntityTypeConfiguration<T>`.
- [x] Initial PostgreSQL migration named **`InitialMake`** generated and verified.
- [x] `jsonb` column mappings for `request_payload`, `response_payload`, and `explaining_features` with PostgreSQL `GIN` indexing.
- [x] Owned value object mappings for `BloodPressure`, `CholesterolReading`, `EcgResult`, and `ConfidenceInterval`.
- [x] Soft delete global query filters (`IsDeleted == false`) and change-tracker timestamp auditing (`UpdatedAt = UtcNow`).
- [x] Referential integrity: `Restrict` on Patient -> Encounters to guard historical records; `Cascade` on Encounters -> Indicators and Assessments -> Attributions.
- [x] Repositories implemented: `IPatientRepository`, `IClinicalEncounterRepository`, `IRiskAssessmentRepository`, `IPredictionAuditRepository`.
- [x] Dependency injection wired in `HeartCDSS.Infrastructure/DependencyInjection.cs` and registered in `HeartCDSS.Api/Program.cs`.
- [x] Zero compilation warnings and zero compilation errors.
- [x] 100% passing test suite across all 91 tests.
