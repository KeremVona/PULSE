# Frontend - Clinician Decision Support Portal

React + TypeScript single-page application styled with Tailwind CSS.

## Scope & Responsibilities

- **State Management**: Redux Toolkit & RTK Query for cached and optimistic clinical encounter workflows.
- **Intake Form**: Two-column clinical intake (`react-hook-form` + `zod`) with real-time physiological boundary checks and warning alerts.
- **Explainability Visualizations**:
  - Calibrated cardiovascular risk score gauge with 95% confidence intervals.
  - Recharts horizontal bar chart visualizing local SHAP feature attributions (risk drivers vs. protective factors).
- **Clinical Governance**: Persistent, non-dismissible CDSS advisory banner and exportable clinical notes.
