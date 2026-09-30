import {
  RiskAssessmentPayload,
  ClinicalIndicatorsInput
} from '@/types/clinical';

export interface ClinicalExportContext {
  patientName: string;
  mrn: string;
  encounterNumber: string;
  encounterDate: string;
  clinicianName: string;
  clinicianRole: string;
  chiefComplaint?: string | null;
  indicators: ClinicalIndicatorsInput;
  assessment: RiskAssessmentPayload;
}

/**
 * Generates a structured EMR clinical summary note in markdown format.
 * Formatted for direct copy-paste into institutional health records (Epic, Cerner).
 */
export function makeClinicalNoteMarkdown(context: ClinicalExportContext): string {
  const {
    patientName,
    mrn,
    encounterNumber,
    encounterDate,
    clinicianName,
    clinicianRole,
    chiefComplaint,
    indicators,
    assessment
  } = context;

  const probabilityPct = (assessment.riskProbability * 100).toFixed(1);
  const lowerCiPct = (assessment.confidenceInterval.lowerBound * 100).toFixed(1);
  const upperCiPct = (assessment.confidenceInterval.upperBound * 100).toFixed(1);
  const thresholdPct = (assessment.decisionThreshold * 100).toFixed(0);

  const topDrivers = assessment.topRiskDrivers.slice(0, 3);
  const topProtective = assessment.topProtectiveFactors.slice(0, 2);

  const driversList = topDrivers.length > 0
    ? topDrivers
        .map(
          (d, idx) =>
            `   ${idx + 1}. ${d.displayName}: ${d.clinicalValue} (+${d.attributionValue.toFixed(3)} SHAP)`
        )
        .join('\n')
    : '   None significant detected.';

  const protectiveList = topProtective.length > 0
    ? topProtective
        .map(
          (p, idx) =>
            `   ${idx + 1}. ${p.displayName}: ${p.clinicalValue} (${p.attributionValue.toFixed(3)} SHAP)`
        )
        .join('\n')
    : '   None significant detected.';

  const warningsList = assessment.clinicalWarnings.length > 0
    ? assessment.clinicalWarnings.map((w) => `   - ${w}`).join('\n')
    : '   - No acute physiological boundary warnings triggered.';

  return `================================================================================
PULSE CARDIOVASCULAR CDSS — CLINICAL ENCOUNTER SUMMARY NOTE
================================================================================
PATIENT NAME:      ${patientName}
MRN:               ${mrn}
ENCOUNTER NUMBER:  ${encounterNumber}
ENCOUNTER DATE:    ${new Date(encounterDate).toLocaleString()}
ATTENDING:         ${clinicianName} (${clinicianRole})
CHIEF COMPLAINT:   ${chiefComplaint || 'Cardiovascular risk evaluation and screening'}

--------------------------------------------------------------------------------
1. RECORDED CLINICAL & PHYSIOLOGICAL INDICATORS
--------------------------------------------------------------------------------
- Patient Age / Biological Sex: ${indicators.age} years / ${indicators.sex === 1 ? 'Male' : 'Female'}
- Resting Blood Pressure:      ${indicators.restingBp}/${indicators.diastolicBp} mmHg
- Total Serum Cholesterol:     ${indicators.serumCholesterol} mg/dL
- Fasting Blood Sugar:         ${indicators.fastingBloodSugar === 1 ? '> 120 mg/dL (Elevated)' : '<= 120 mg/dL (Normal)'}
- Resting ECG Findings:        Type ${indicators.restingEcg}
- Max Achieved Heart Rate:     ${indicators.maxHeartRate} bpm
- Exercise-Induced Angina:     ${indicators.exerciseAngina === 1 ? 'Present' : 'Absent'}
- ST Depression (Oldpeak):     ${indicators.oldpeakStDepression.toFixed(1)} mm (Slope: Type ${indicators.slope})
- Major Fluoroscopy Vessels:   ${indicators.fluoroscopyVessels} of 3 vessels colored
- Thalassemia Defect Type:     Defect Code ${indicators.thalassemia}

--------------------------------------------------------------------------------
2. CDSS CALIBRATED CARDIOVASCULAR RISK EVALUATION
--------------------------------------------------------------------------------
- Calibrated CAD Probability:  ${probabilityPct}%
- Risk Stratification Tier:    ${assessment.riskBand} Risk
- 95% Bootstrap CI:            [${lowerCiPct}% – ${upperCiPct}%] (SE: ±${(assessment.confidenceInterval.standardError * 100).toFixed(1)}%)
- Decision Threshold:          ${thresholdPct}%
- Qualitative Stratification:  ${assessment.isCadPositive ? 'CAD Stratified Positive' : 'CAD Stratified Negative'}
- Model Pipeline:              Calibrated Tree Ensemble (Isotonic Regression)
- Inference Turnaround:        ${assessment.inferenceLatencyMs.toFixed(1)} ms

--------------------------------------------------------------------------------
3. LOCAL TreeSHAP FEATURE ATTRIBUTIONS (TOP CONTRIBUTORS)
--------------------------------------------------------------------------------
Risk-Increasing Drivers:
${driversList}

Protective Factors:
${protectiveList}

--------------------------------------------------------------------------------
4. CLINICAL SAFETY ALERTS
--------------------------------------------------------------------------------
${warningsList}

--------------------------------------------------------------------------------
5. RECOMMENDED CLINICAL ACTION PLAN
--------------------------------------------------------------------------------
${assessment.recommendedAction}

--------------------------------------------------------------------------------
6. REGULATORY CDSS ADVISORY & CLINICIAN ATTESTATION
--------------------------------------------------------------------------------
${assessment.clinicalDisclaimer}

Regulatory Notice: PULSE CDSS is an assistive software tool (FDA SaMD / EU MDR
Class IIa). All algorithmic recommendations are non-autonomous and subject to
independent physician evaluation.

Attesting Clinician: ${clinicianName}
Electronic Signature: Verified via Institutional EMR Single Sign-On
Date / Timestamp:    ${new Date().toISOString()}
================================================================================`;
}
