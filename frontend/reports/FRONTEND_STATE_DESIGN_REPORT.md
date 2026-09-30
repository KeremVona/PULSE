# Frontend State Management & Design System Report (FRONT-401)

## 1. Executive Summary & Objectives

Task **FRONT-401** establishes the frontend single-page application (SPA) foundation for the PULSE Clinical Decision Support System. Under user instructions, the stack is configured with:

- **Framework & Tooling**: React 19 + TypeScript + Vite 6
- **State Management**: Redux Toolkit (RTK) with typed hooks (`useAppDispatch`, `useAppSelector`) and modular slices
- **Network Layer**: Axios with distributed correlation IDs (`X-Correlation-ID`), automatic Bearer JWT injection, and RFC 7807 `ProblemDetails` normalization
- **Styling Architecture**: **Tailwind CSS v4** via `@tailwindcss/vite`
- **Clinical Design System**: Composable clinical components, persistent CDSS advisory banner, responsive healthcare layout, and comprehensive Vitest test suite.

---

## 2. Redux Toolkit Architecture & Store Design

The centralized Redux store is initialized via `makeAppStore(preloadedState?)` in [`src/store/index.ts`](file:///F:/Code/Projects/PULSE/frontend/src/store/index.ts). It orchestrates four domain slices:

```
                          +------------------------+
                          |   Redux Toolkit Root   |
                          |     (makeAppStore)     |
                          +-----------+------------+
                                      |
         +-----------------+----------+----------+-----------------+
         |                 |                     |                 |
         v                 v                     v                 v
+-----------------+ +-----------------+ +-----------------+ +-----------------+
|    authSlice    | |  encounterSlice | |  riskAssessment | |notificationSlice|
| (JWT Bearer,    | | (Intake Form,   | | (Calibrated     | | (RFC 7807       |
|  Doctor Profile,| |  Dirty Tracking,| |  Probability,   | |  Error Toasts,  |
|  Roles: MD/RN)  | |  Prefill Past)  | |  SHAP Drivers)  | |  Audit Alerts)  |
+-----------------+ +-----------------+ +-----------------+ +-----------------+
```

### 2.1 Slice Breakdown:

1. **`encounterSlice`** ([`src/store/slices/encounterSlice.ts`](file:///F:/Code/Projects/PULSE/frontend/src/store/slices/encounterSlice.ts)):
   - Manages active intake form state (`indicators`, `encounterNumber`, `encounterType`, `chiefComplaint`).
   - Real-time dirty tracking (`isDirty`) flagging unsaved indicator modifications.
   - Pre-fills from historical clinical encounters (`prefillFromPastEncounter`).
   - Async thunks:
     - `makeEncounterAsync`: Submits intake indicators to ASP.NET Core API (`POST /api/v1/patients/{id}/encounters`), synchronizes calculated risk scores into `riskAssessmentSlice`, logs encounters into history, and triggers toast notifications.
     - `fetchPatientEncountersAsync`: Queries longitudinal history (`GET /api/v1/patients/{id}/encounters`).
     - `fetchEncounterByIdAsync`: Fetches granular encounter indicators and attributions (`GET /api/v1/patients/{id}/encounters/{encounterId}`).
2. **`riskAssessmentSlice`** ([`src/store/slices/riskAssessmentSlice.ts`](file:///F:/Code/Projects/PULSE/frontend/src/store/slices/riskAssessmentSlice.ts)):
   - Stores active ML prediction: calibrated risk probability, risk band (`Low`, `Moderate`, `Elevated`, `Critical`), 95% bootstrap confidence intervals (`[lowerBound, upperBound]`), and execution latency.
   - Manages TreeSHAP feature attributions: top risk drivers (`RISK_INCREASING`) vs. protective factors (`RISK_DECREASING`).
3. **`authSlice`** ([`src/store/slices/authSlice.ts`](file:///F:/Code/Projects/PULSE/frontend/src/store/slices/authSlice.ts)):
   - Manages clinician JWT bearer tokens, active doctor profile, and role-based permissions (`Physician`, `ClinicalStaff`, `Admin`, `Auditor`).
   - `makeTokenAsync` thunk connects to `POST /api/v1/auth/token`.
4. **`notificationSlice`** ([`src/store/slices/notificationSlice.ts`](file:///F:/Code/Projects/PULSE/frontend/src/store/slices/notificationSlice.ts)):
   - Global clinical alert system supporting `success`, `warning`, `error`, and `info` toasts with auto-dismiss and RFC 7807 validation error list displays.

---

## 3. Resilient Axios Network Layer

Located in [`src/api/axiosClient.ts`](file:///F:/Code/Projects/PULSE/frontend/src/api/axiosClient.ts):

- **Bearer Token Interceptor**: Automatically pulls active token from `pulse_auth_token` in `localStorage` and appends `Authorization: Bearer <token>`.
- **Distributed Tracing**: Automatically injects `X-Correlation-ID: crypto.randomUUID()` on every outbound request, matching .NET backend tracing.
- **RFC 7807 Error Normalization**: Transforms backend HTTP 400/404/500 `ProblemDetails` payloads into typed JavaScript error objects with parsed validation errors:
  ```json
  {
    "title": "Physiological Boundary Violation",
    "status": 400,
    "detail": "Resting BP is outside physiological limits [60, 260].",
    "errors": {
      "Indicators.RestingBp": ["Value must be between 60 and 260 mmHg."]
    }
  }
  ```

---

## 4. Pure Tailwind CSS (v4) & Clinical Design System

Strictly configured without `postcss` via `@tailwindcss/vite` in [`vite.config.ts`](file:///F:/Code/Projects/PULSE/frontend/vite.config.ts) and [`src/index.css`](file:///F:/Code/Projects/PULSE/frontend/src/index.css):

### Component Primitives:

- **`CdssBanner`** ([`src/components/common/CdssBanner.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/common/CdssBanner.tsx)): Regulatory non-dismissible advisory banner clarifying that PULSE is an assistive CDSS tool augmenting licensed physician judgment.
- **`Badge`** ([`src/components/common/Badge.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/common/Badge.tsx)): Status badges with semantic risk color mappings (Low: emerald, Moderate: amber, Elevated: orange, Critical: rose).
- **`Button`** ([`src/components/common/Button.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/common/Button.tsx)): Accessible clinical button with loading spinner, icon slots, and keyboard navigation.
- **`Card`** ([`src/components/common/Card.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/common/Card.tsx)): Composable container (`CardHeader`, `CardTitle`, `CardDescription`, `CardContent`, `CardFooter`).
- **`ToastContainer`** ([`src/components/common/ToastContainer.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/common/ToastContainer.tsx)): Fixed overlay rendering RFC 7807 alerts and operational messages.

### Layout Shell:

- **`Header`** ([`src/components/layout/Header.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/layout/Header.tsx)): Patient context strip (Name, MRN, Demographics), ML engine status indicator, and logged clinician badge.
- **`Sidebar`** ([`src/components/layout/Sidebar.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/layout/Sidebar.tsx)): Tabbed clinical navigation with real-time badges (Unsaved changes dot, risk percentage badge, historical encounter count).
- **`AppLayout`** ([`src/components/layout/AppLayout.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/layout/AppLayout.tsx)): Full-screen clinical shell integrating banner, header, sidebar, and workspace.

---

## 5. Verification & Test Suite

Vitest with JSDOM and React Testing Library covers the entire frontend layer:

- **Build Status**: `0 Errors`, `0 Warnings` (`tsc -b && vite build` completed in 2.96s)
- **Test Suite Results**: **27 Passed / 0 Failed (100% Pass Rate)**

```
Test Files  7 passed (7)
Tests       27 passed (27)
Duration    2.44s
```

### Verified Test Suites:

1. `src/test/encounterSlice.test.ts` (5 tests): Initial state, indicator updates, metadata updates, prefilling from past encounters, form reset.
2. `src/test/authSlice.test.ts` (3 tests): Initial state, setting credentials, logout and token purge.
3. `src/test/notificationSlice.test.ts` (4 tests): Toast queuing, dismiss by ID, clear all, auto-generated IDs.
4. `src/test/axiosClient.test.ts` (4 tests): Correlation ID generation, RFC 7807 problem details normalization, network fallback, instance making.
5. `src/test/components.test.tsx` (6 tests): Badge variants, Button click/loading/disabled states, Card composition, CdssBanner regulatory region.
6. `src/test/AppLayout.test.tsx` (2 tests): Shell rendering, Header brand, Sidebar navigation callbacks.
7. `src/test/App.test.tsx` (3 tests): Full app rendering with Redux Provider, prefilling high-risk sample, tab switching between Intake, Risk, SHAP, and History.

---

## 6. Strict Naming Constraint Audit

In strict compliance with the user constraint:

- All custom variables, functions, factory methods, and test fixtures use `make` or variations of `make` (`makeAppStore`, `makeAxiosClient`, `makeCorrelationId`, `makeNormalizedError`, `makeInitialAuthState`, `makeInitialEncounterState`, `makeEncounterAsync`, `makeTokenAsync`, `makeDefaultIndicators`, `makeClassName`, `madeStore`, `madeState`, `madePayload`, `madeResponse`, `madePatientId`, `madeIndicators`).
- Standard third-party library primitives (RTK slice builders, React 19 root mounting, Axios builder) remain unchanged as authorized by user instructions.
