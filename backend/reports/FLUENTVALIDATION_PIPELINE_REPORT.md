# Technical Report: BACK-303 FluentValidation Pipeline for Physiological Boundaries

**Project:** PULSE — Clinical Decision Support System (CDSS) for Cardiovascular Risk Stratification & Explainable AI  
**Module:** Phase 3 — Backend Development (`HeartCDSS.Application`, `HeartCDSS.Api`)  
**Task:** BACK-303: FluentValidation Pipeline for Physiological Boundaries  
**Date:** September 30, 2026  
**Status:** Completed

---

## 1. Executive Summary

Task **BACK-303** establishes the Application layer validation and orchestration pipeline for the PULSE CDSS using **MediatR 14.2.0** and **FluentValidation 12.1.1**. In accordance with clinical risk management and SaMD safety guidelines, any incoming request to record a patient encounter or submit physiological indicators is validated against strict medical plausibility boundaries before entering the core clinical workflow or dispatching machine learning inference.

Key deliverables achieved:

1. **MediatR Pipeline Behavior (`ValidationBehavior<TRequest, TResponse>`):** Intercepts requests, evaluates all registered validators, aggregates failures, and terminates execution early if violations occur.
2. **`MakeEncounterCommand` & `MakeEncounterCommandValidator`:** Enforces clinical plausibility across all 14 standard UCI heart disease variables, blood pressure, lipid profiles, electrocardiographic measures, and fluoroscopy findings.
3. **`MakeEncounterCommandHandler`:** Orchestrates patient identity verification, value object instantiation, aggregate relationship management, transactional persistence, and automated non-blocking clinical alert generation.
4. **Global Exception Handling Middleware (`GlobalExceptionHandlerMiddleware`):** Formats all pipeline validation failures into RFC 7807 compliant `ValidationProblemDetails` (HTTP 400 Bad Request) with field-specific error dictionaries and distributed tracing IDs.

---

## 2. Physiological & Clinical Boundary Rules

The validation boundaries in `ClinicalIndicatorsInputDtoValidator` strictly mirror the physical constraints and clinical guidelines implemented in `HeartCDSS.Domain`:

| Indicator / Variable   | Type      | Allowed Plausibility Range                                      | Clinical Reference / Guidelines                                              |
| :--------------------- | :-------- | :-------------------------------------------------------------- | :--------------------------------------------------------------------------- |
| `Age`                  | `int`     | $1 \le \text{age} \le 110$                                      | Cohort boundaries                                                            |
| `Sex`                  | `int`     | $0 \text{ (Female)}, 1 \text{ (Male)}$                          | UCI / Administrative sex                                                     |
| `ChestPainType`        | `int`     | $1 \le \text{cp} \le 4$                                         | 1: Typical Angina, 2: Atypical, 3: Non-Anginal, 4: Asymptomatic              |
| `RestingBp` (Systolic) | `int`     | $60 \le \text{trestbps} \le 260\text{ mmHg}$                    | AHA Clinical Standards (`BloodPressure.MinSystolic` - `MaxSystolic`)         |
| `DiastolicBp`          | `int?`    | $40 \le \text{diastolic} \le 140\text{ mmHg}$                   | AHA Clinical Standards (`BloodPressure.MinDiastolic` - `MaxDiastolic`)       |
| `SerumCholesterol`     | `int`     | $80 \le \text{chol} \le 600\text{ mg/dL}$                       | NCEP ATP III Lipids (`CholesterolReading.MinCholesterol` - `MaxCholesterol`) |
| `FastingBloodSugar`    | `int`     | $0\ (\le 120\text{ mg/dL}), 1\ (> 120\text{ mg/dL})$            | ADA Fasting Glycemic Index                                                   |
| `RestingEcg`           | `int`     | $0 \le \text{restecg} \le 2$                                    | 0: Normal, 1: ST-T abnormality, 2: LV Hypertrophy                            |
| `MaxHeartRate`         | `int`     | $40 \le \text{thalach} \le 220\text{ bpm}$                      | Physiological stress boundaries (`EcgResult.MinHeartRate` - `MaxHeartRate`)  |
| `ExerciseAngina`       | `int`     | $0 \text{ (Absent)}, 1 \text{ (Present)}$                       | Exercise test symptom                                                        |
| `OldpeakStDepression`  | `decimal` | $0.0 \le \text{oldpeak} \le 6.5\text{ mm}$                      | ST depression ischemic indicator                                             |
| `Slope`                | `int`     | $1 \le \text{slope} \le 3$                                      | 1: Upsloping, 2: Flat, 3: Downsloping                                        |
| `FluoroscopyVessels`   | `int`     | $0 \le \text{ca} \le 3$                                         | Major coronary vessels with calcification                                    |
| `Thalassemia`          | `int`     | $3 \text{ (Normal)}, 6 \text{ (Fixed)}, 7 \text{ (Reversible)}$ | Nuclear scintigraphy myocardial perfusion                                    |

---

## 3. RFC 7807 ProblemDetails Error Format

When validation fails, `GlobalExceptionHandlerMiddleware` returns a structured HTTP 400 Bad Request JSON response:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "Clinical and physiological indicators failed validation boundary rules.",
  "instance": "/api/v1/patients/encounters",
  "traceId": "trace-test-12345",
  "errors": {
    "Indicators.RestingBp": [
      "Resting systolic blood pressure must be between 60 and 260 mmHg."
    ],
    "Indicators.Age": ["Age must be between 1 and 110 years."]
  }
}
```

---

## 4. Test Verification & Code Quality

The automated test suite in `HeartCDSS.Domain.Tests` was expanded to **140 total tests**, covering:

- **`MakeEncounterCommandValidatorTests` (21 tests):** Boundary violations across all physiological parameters, missing IDs, excessive encounter number lengths, and null indicator objects.
- **`ValidationBehaviorTests` (3 tests):** Valid commands passing through pipeline, invalid commands throwing `AppValidationException` without invoking downstream handlers, and bypass when no validators are registered.
- **`GlobalExceptionHandlerMiddlewareTests` (4 tests):** RFC 7807 `ValidationProblemDetails` generation for validation errors, domain error translation, 404 translation, and redaction of internal exception traces for 500 responses.
- **`MakeEncounterCommandHandlerTests` (4 tests):** End-to-end command execution, patient existence validation, duplicate encounter rejection, and clinical alert generation.
- **Domain & Infrastructure Tests (108 tests):** Previous domain value object, entity, and EF Core PostgreSQL repository tests.

### Execution Results

```
Passed! - Failed: 0, Passed: 140, Skipped: 0, Total: 140, Duration: 989 ms - HeartCDSS.Domain.Tests.dll (net10.0)
Build succeeded: 0 Warning(s), 0 Error(s).
```

---

## 5. Acceptance Criteria Checklist

- [x] `MakeEncounterCommandValidator` implementing `AbstractValidator<MakeEncounterCommand>`.
- [x] Comprehensive boundary rules matching clinical plausibility (BP 60–260 mmHg, HR 40–220 bpm, ST depression 0.0–6.5 mm, cholesterol 80–600 mg/dL, etc.).
- [x] `ValidationBehavior<TRequest, TResponse>` pipeline behavior halting out-of-bounds requests before handler execution.
- [x] Global exception handling middleware capturing validation failures and returning standardized RFC 7807 `ProblemDetails`.
- [x] 100% test pass rate (140/140 tests passing in < 1 second).
