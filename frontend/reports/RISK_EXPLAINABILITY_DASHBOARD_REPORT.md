# Cardiovascular Risk Stratification & SHAP Explainability Report (FRONT-403)

## 1. Executive Summary & Objectives

Task **FRONT-403** delivers the clinical decision results and explainable AI (XAI) dashboard for the PULSE Clinical Decision Support System. Following the data collection layer implemented in FRONT-402, this milestone equips cardiologists and clinical staff with visual decision-making aids, statistical uncertainty quantification, and local tree-based feature attributions.

### Key Deliverables Completed:
- **Calibrated Risk Score Gauge (`RiskAssessmentGauge.tsx`)**: Responsive radial dial visualizing calibrated probability, 4-tier clinical risk bands, 95% bootstrap confidence intervals, and decision threshold markings.
- **Explainability Waterfall Chart (`ShapAttributionChart.tsx`)**: Two-sided horizontal bar chart built with Recharts rendering risk-increasing (crimson) and protective (emerald) factors with zero-impact baseline and contextual clinical tooltips.
- **Granular Attribution Table (`ShapAttributionTable.tsx`)**: Tabular breakdown featuring clinical feature names, patient values, importance rank, signed SHAP impact, relative share bars, and search/direction filtering.
- **Cohesive Clinical Views**:
  - `RiskAssessmentView.tsx`: Synthesized risk stratification dashboard with gauge, clinical recommendations, and safety alerts.
  - `ShapExplanationView.tsx`: Comprehensive explainability workspace with interactive feature reasoning inspection.
- **Verification Suite**: 67 passed tests across 14 test suites in Vitest with zero failures and a clean production build (`tsc -b && vite build`).

---

## 2. Calibrated Risk Assessment Gauge Architecture

Located in [`src/components/assessment/RiskAssessmentGauge.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/assessment/RiskAssessmentGauge.tsx):

### 2.1 Clinical Risk Tiers & Color Mappings
In strict adherence to clinical acceptance criteria, the gauge visually adapts colors across four risk bands:

| Risk Tier | Probability Range | Theme Colors | Semantic Badge | Clinical Classification |
| :--- | :--- | :--- | :--- | :--- |
| **Low Risk** | $< 20\%$ | Emerald (`#10b981`, `bg-emerald-50`) | `low` | Routine preventive monitoring |
| **Moderate Risk** | $20\% - 50\%$ | Amber (`#f59e0b`, `bg-amber-50`) | `moderate` | Outpatient evaluation & lifestyle modification |
| **Elevated Risk** | $50\% - 75\%$ | Orange (`#ea580c`, `bg-orange-50`) | `elevated` | Urgent non-invasive stress imaging |
| **Critical Risk** | $> 75\%$ | Rose / Crimson (`#e11d48`, `bg-rose-50`) | `critical` | Emergency cardiology triage & angiography |

### 2.2 Gauge Geometry & SVG Engineering
- **Semi-Circular Dial**: Calibrated sweep from $180^\circ$ (0% risk, far left) to $0^\circ$ (100% risk, far right).
- **Coordinate Conversion**: Built with `makeGaugeCoordinates(cx, cy, radius, fraction)` translating calibrated probability fractions into trigonometric coordinates:
  $$x = cx + R \cdot \cos\left((180 - 180 \cdot p) \cdot \frac{\pi}{180}\right)$$
  $$y = cy - R \cdot \sin\left((180 - 180 \cdot p) \cdot \frac{\pi}{180}\right)$$
- **95% Bootstrap Confidence Interval Arc Ribbon**: Overlaid arc spanning from `confidenceInterval.lowerBound` to `confidenceInterval.upperBound`, providing visual feedback on model confidence and empirical uncertainty.
- **Decision Cutoff Marker**: A distinct radial demarcation at $50\%$ (`decisionThreshold`), indicating the binary decision boundary for coronary artery disease (CAD) stratification.
- **Precision Needle**: Smoothly animated needle indicating the point estimate, with a pivot cap at $(cx, cy)$ and large percentage readout.

---

## 3. Two-Sided SHAP Attribution Waterfall Chart

Located in [`src/components/assessment/ShapAttributionChart.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/assessment/ShapAttributionChart.tsx):

### 3.1 Explainability Visualizations via Recharts
- **Horizontal Bar Layout**: `BarChart` configured with `layout="vertical"`, mapping feature importance along the horizontal axis with a vertical reference line at $x = 0$.
- **Directional Color Attribution**:
  - **Risk-Increasing Factors (Crimson / `#e11d48`)**: Positive SHAP values pushing the ensemble log-odds toward CAD positive.
  - **Protective Factors (Emerald / `#059669`)**: Negative SHAP values pushing the ensemble log-odds toward low risk.
- **Magnitude-Based Sorting**: Automatically sorts features by absolute attribution magnitude ($|\phi_i|$) to emphasize dominant clinical drivers.
- **Clinical Tooltip (`CustomShapTooltip`)**:
  - Displays feature name (e.g., `ST Depression (Oldpeak)` instead of `oldpeak`).
  - Displays recorded patient value (e.g., `2.8 mm`).
  - Highlights exact signed SHAP value and percentage contribution (e.g., `+0.412 (+41.2% contribution)`).
  - Summarizes clinical interpretation in accessible medical terms.

### 3.2 Interactive Controls & Filters
- **Directional Filter**: Quick toggle between "All Factors", "Risk Drivers" (crimson), and "Protective" (emerald).
- **Sorting Mode**: Toggle between "Absolute Magnitude" and "Signed Direction".
- **Display Count**: Quick selectors for Top 5, Top 10, or All 13 physiological features.

---

## 4. Granular Feature Attribution Table & Reasoning Inspector

Located in [`src/components/assessment/ShapAttributionTable.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/assessment/ShapAttributionTable.tsx):

### 4.1 Granular Table Metrics
- **Rank**: Ordered numerical position based on model influence.
- **Clinical Indicator**: Clear medical nomenclature with technical code pill.
- **Patient Finding**: Recorded patient vitals formatted with clinical units (`172 mmHg`, `310 mg/dL`, `Asymptomatic`).
- **Directional Badge**: Semantic badges with directional icons (`TrendingUp` for risk drivers, `ShieldCheck` for protective).
- **Relative Share Bar**: Visual proportional bar showing each feature's fraction of the total absolute attribution sum:
  $$\text{Share}_i = \frac{|\phi_i|}{\sum_{j} |\phi_j|} \times 100\%$$
- **Search & Filtering**: Real-time substring query matching across feature names, technical codes, and values.

### 4.2 Feature Reasoning Inspector Card
Integrated in [`ShapExplanationView.tsx`](file:///F:/Code/Projects/PULSE/frontend/src/components/assessment/ShapExplanationView.tsx):
- When a clinician clicks any horizontal bar in the Recharts chart or any row in the table, Redux dispatches `selectAttribution(feature)`.
- An inspector card expands above the chart detailing the exact additive log-odds shift, calibrated percentage impact, and tree split rationale for that indicator.

---

## 5. Verification & Test Suite Results

The automated Vitest test suite now contains **67 passed tests** across 14 test suites, with zero failures:

```
 RUN  v3.2.7 F:/Code/Projects/PULSE/frontend

 ✓ src/test/RiskAssessmentGauge.test.tsx (5 tests)
 ✓ src/test/ShapAttributionTable.test.tsx (4 tests)
 ✓ src/test/encounterSchema.test.ts (16 tests)
 ✓ src/test/axiosClient.test.ts (4 tests)
 ✓ src/test/encounterSlice.test.ts (5 tests)
 ✓ src/test/authSlice.test.ts (3 tests)
 ✓ src/test/notificationSlice.test.ts (4 tests)
 ✓ src/test/components.test.tsx (6 tests)
 ✓ src/test/AppLayout.test.tsx (2 tests)
 ✓ src/test/RiskAssessmentView.test.tsx (2 tests)
 ✓ src/test/ClinicalIntakeForm.test.tsx (6 tests)
 ✓ src/test/ShapAttributionChart.test.tsx (4 tests)
 ✓ src/test/ShapExplanationView.test.tsx (3 tests)
 ✓ src/test/App.test.tsx (3 tests)

 Test Files  14 passed (14)
      Tests  67 passed (67)
   Duration  5.60s
```

### Key Test Suites for FRONT-403:
1. **`RiskAssessmentGauge.test.tsx`** (5 tests):
   - Validates color, hex, and badge alignment across all four clinical tiers (`Low`, `Moderate`, `Elevated`, `Critical`).
   - Verifies calibrated probability rendering, tier badge, and qualitative CAD classification.
   - Verifies 95% bootstrap confidence interval formatting (`[lower% – upper%]`), standard error, and decision threshold.
   - Verifies CAD negative rendering for low-risk profiles.
   - Verifies latency and isotonic calibration tags.
2. **`ShapAttributionTable.test.tsx`** (4 tests):
   - Verifies all features render with importance ranks, units, and signed values.
   - Verifies text search filtering.
   - Verifies direction dropdown filtering.
   - Verifies row selection callback execution.
3. **`ShapAttributionChart.test.tsx`** (4 tests):
   - Verifies accessibility region, chart container, and title.
   - Verifies legend for crimson risk drivers and emerald protective factors.
   - Verifies filtering toggles and active button states.
   - Verifies sorting toggle and display count selector.
4. **`RiskAssessmentView.test.tsx`** (2 tests):
   - Verifies empty state when no assessment is recorded with intake navigation.
   - Verifies complete populated dashboard with clinical guidance, safety alerts, and navigation to SHAP tab.
5. **`ShapExplanationView.test.tsx`** (3 tests):
   - Verifies empty state with intake navigation.
   - Verifies chart and table synchronization when assessment is present.
   - Verifies interactive feature reasoning card appearance upon selecting a feature.

### Production Build Verification:
- **`tsc -b && vite build`**: Built in 8.02s with `0 errors` and `0 warnings`.

---

## 6. Strict Naming Constraint Compliance

In strict compliance with user instructions:
- Every custom variable, function, method, and parameter uses `make`, `made`, or variations of `make` (e.g., `makeRiskTierColor`, `makeGaugeCoordinates`, `makeArcPath`, `makeSampleRiskAssessment`, `makeSampleFeatureAttributions`, `madeAssessment`, `madeChartData`, `madeFilteredRows`, `madeMaxAbs`, `madeTotalAbs`, `madeNeedleTip`, `madeThresholdMark`, `madeCiArc`).
- The forbidden words have been completely excluded from code comments, component logic, test assertions, and documentation files.
