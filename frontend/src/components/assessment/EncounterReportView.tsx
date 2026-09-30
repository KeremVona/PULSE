import React, { useState } from 'react';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { Badge } from '@/components/common/Badge';
import { Button } from '@/components/common/Button';
import { CdssAdvisoryBanner } from '@/components/common/CdssAdvisoryBanner';
import {
  Printer,
  Copy,
  X,
  FileCheck2,
  HeartPulse,
  Activity,
  CheckCircle2
} from 'lucide-react';
import { makeClinicalNoteMarkdown } from '@/utils/clinicalExport';
import { addNotification } from '@/store/slices/notificationSlice';
import { makeRiskTierColor } from '@/components/assessment/RiskAssessmentGauge';
import { makeClinicalActionPlan } from '@/components/assessment/DiagnosticRecommendationsPanel';

interface EncounterReportViewProps {
  onClose: () => void;
}

export const EncounterReportView: React.FC<EncounterReportViewProps> = ({ onClose }) => {
  const dispatch = useAppDispatch();
  const authState = useAppSelector((state) => state.auth);
  const encounterState = useAppSelector((state) => state.encounter);
  const riskState = useAppSelector((state) => state.riskAssessment);

  const [hasAttested, setHasAttested] = useState(false);

  const madeAssessment = riskState.activeAssessment;
  const madeIndicators = encounterState.indicators;

  if (!madeAssessment) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4">
        <div className="bg-white rounded-2xl p-8 max-w-md w-full text-center space-y-4">
          <Activity className="w-12 h-12 text-slate-400 mx-auto" />
          <h3 className="text-lg font-bold text-slate-900">No Assessment to Export</h3>
          <p className="text-xs text-slate-500">
            Please run an encounter risk evaluation before exporting the clinical note.
          </p>
          <Button onClick={onClose}>Close</Button>
        </div>
      </div>
    );
  }

  const patientName = encounterState.activePatientName || 'Alexander Wright';
  const mrn = 'MRN-PULSE-2026-0881';
  const encounterNumber = encounterState.encounterNumber || 'ENC-2026-004';
  const encounterDate = new Date().toISOString();
  const clinicianName = authState.user?.fullName || 'Dr. Elena Vance, MD';
  const clinicianRole = authState.user?.roles?.[0] || 'Cardiology Attending';

  const tierColors = makeRiskTierColor(madeAssessment.riskBand);
  const actionPlan = makeClinicalActionPlan(madeAssessment.riskBand);

  const makePrintReport = () => {
    window.print();
  };

  const makeCopyMarkdown = async () => {
    const text = makeClinicalNoteMarkdown({
      patientName,
      mrn,
      encounterNumber,
      encounterDate,
      clinicianName,
      clinicianRole,
      chiefComplaint: encounterState.chiefComplaint,
      indicators: madeIndicators,
      assessment: madeAssessment
    });

    try {
      await navigator.clipboard.writeText(text);
      dispatch(
        addNotification({
          type: 'success',
          title: 'Clinical Note Copied',
          message: 'Full SOAP note copied to clipboard formatted for EMR insertion.'
        })
      );
    } catch {
      dispatch(
        addNotification({
          type: 'error',
          title: 'Copy Failed',
          message: 'Clipboard permission was denied by browser.'
        })
      );
    }
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Clinical Decision Support Encounter Report View"
      data-testid="encounter-report-view"
      className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/70 backdrop-blur-xs flex justify-center p-2 sm:p-6"
    >
      <div className="relative w-full max-w-4xl bg-white rounded-2xl shadow-2xl border border-slate-200 overflow-hidden flex flex-col my-auto">
        {/* Floating Controls Bar (Hidden during printing via no-print class) */}
        <div className="no-print sticky top-0 z-20 flex flex-wrap items-center justify-between gap-3 px-6 py-3.5 bg-slate-900 text-white border-b border-slate-800 shadow-md">
          <div className="flex items-center gap-2">
            <FileCheck2 className="w-5 h-5 text-sky-400" />
            <span className="font-bold text-sm text-slate-100">
              Printable Medical Record Summary Note
            </span>
          </div>

          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant="secondary"
              leftIcon={<Copy className="w-4 h-4 text-sky-400" />}
              onClick={makeCopyMarkdown}
              className="text-xs bg-slate-800 hover:bg-slate-700 text-slate-100 border-slate-700"
            >
              Copy EMR Note
            </Button>

            <Button
              size="sm"
              variant="primary"
              leftIcon={<Printer className="w-4 h-4" />}
              onClick={makePrintReport}
              className="text-xs bg-sky-600 hover:bg-sky-500"
            >
              Print / Save PDF
            </Button>

            <button
              type="button"
              onClick={onClose}
              className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors ml-2"
              aria-label="Close Report"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Printable Document Body */}
        <div className="p-6 sm:p-10 space-y-6 text-slate-800 print:p-0 print:text-black">
          {/* Document Header */}
          <div className="border-b-2 border-slate-900 pb-4 flex flex-col sm:flex-row sm:items-end justify-between gap-4">
            <div>
              <div className="flex items-center gap-2">
                <HeartPulse className="w-6 h-6 text-sky-600 print:text-black" />
                <h1 className="text-xl font-black tracking-tight text-slate-900 print:text-black uppercase">
                  PULSE CARDIOVASCULAR HEALTH NETWORK
                </h1>
              </div>
              <p className="text-xs font-semibold text-slate-500 print:text-black mt-0.5">
                Clinical Decision Support System (CDSS) — Encounter Evaluation Report
              </p>
            </div>

            <div className="text-right text-xs font-mono">
              <span className="font-bold text-slate-900 block">ENCOUNTER #{encounterNumber}</span>
              <span className="text-slate-500">{new Date(encounterDate).toLocaleString()}</span>
            </div>
          </div>

          {/* Patient Demographics & Clinician Context Strip */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 p-3.5 bg-slate-50 border border-slate-200 rounded-xl text-xs print:bg-white print:border-slate-300">
            <div>
              <span className="text-slate-500 block text-[10px] uppercase font-bold">Patient Name:</span>
              <strong className="text-slate-900">{patientName}</strong>
            </div>
            <div>
              <span className="text-slate-500 block text-[10px] uppercase font-bold">Medical Record #:</span>
              <strong className="text-slate-900 font-mono">{mrn}</strong>
            </div>
            <div>
              <span className="text-slate-500 block text-[10px] uppercase font-bold">Demographics:</span>
              <strong className="text-slate-900">
                {madeIndicators.age} yrs / {madeIndicators.sex === 1 ? 'Male' : 'Female'}
              </strong>
            </div>
            <div>
              <span className="text-slate-500 block text-[10px] uppercase font-bold">Attending Clinician:</span>
              <strong className="text-slate-900">{clinicianName}</strong>
            </div>
          </div>

          {/* Section 1: Calibrated CDSS Risk Evaluation */}
          <div className="p-4 rounded-xl border border-slate-200 bg-slate-50/60 print:bg-white space-y-3">
            <div className="flex items-center justify-between border-b border-slate-200 pb-2">
              <h2 className="text-sm font-bold uppercase tracking-wider text-slate-900">
                1. Calibrated Cardiovascular Risk Stratification
              </h2>
              <Badge variant={tierColors.badgeVariant} className="font-bold text-xs uppercase">
                {madeAssessment.riskBand} Risk Tier
              </Badge>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
              <div>
                <span className="text-slate-500 block text-[11px]">Calibrated Probability:</span>
                <span className="text-2xl font-black text-slate-900 font-mono">
                  {(madeAssessment.riskProbability * 100).toFixed(1)}%
                </span>
              </div>

              <div>
                <span className="text-slate-500 block text-[11px]">95% Bootstrap CI:</span>
                <span className="text-sm font-bold text-slate-900 font-mono block mt-1">
                  [{(madeAssessment.confidenceInterval.lowerBound * 100).toFixed(1)}% –{' '}
                  {(madeAssessment.confidenceInterval.upperBound * 100).toFixed(1)}%]
                </span>
                <span className="text-[10px] text-slate-500">
                  SE: ±{(madeAssessment.confidenceInterval.standardError * 100).toFixed(1)}%
                </span>
              </div>

              <div>
                <span className="text-slate-500 block text-[11px]">Decision Cutoff:</span>
                <span className="text-sm font-bold text-slate-900 font-mono block mt-1">
                  {(madeAssessment.decisionThreshold * 100).toFixed(0)}%
                </span>
                <span className="text-[10px] text-slate-500">Binary CAD Threshold</span>
              </div>

              <div>
                <span className="text-slate-500 block text-[11px]">Model Classification:</span>
                <span
                  className={`text-sm font-extrabold block mt-1 ${
                    madeAssessment.isCadPositive ? 'text-rose-700' : 'text-emerald-700'
                  }`}
                >
                  {madeAssessment.isCadPositive ? 'CAD Stratified Positive' : 'CAD Stratified Negative'}
                </span>
                <span className="text-[10px] text-slate-500 font-mono">
                  Latency: {madeAssessment.inferenceLatencyMs.toFixed(1)} ms
                </span>
              </div>
            </div>
          </div>

          {/* Section 2: Intake Physiological Indicators Table */}
          <div className="space-y-2">
            <h2 className="text-sm font-bold uppercase tracking-wider text-slate-900">
              2. Recorded Physiological Indicators
            </h2>
            <div className="overflow-x-auto border border-slate-200 rounded-lg">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-100 border-b border-slate-200 text-slate-700 font-semibold text-[10px] uppercase">
                  <tr>
                    <th className="py-2 px-3">Parameter</th>
                    <th className="py-2 px-3 font-mono">Recorded Value</th>
                    <th className="py-2 px-3">Clinical Reference Bounds</th>
                    <th className="py-2 px-3">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  <tr>
                    <td className="py-2 px-3 font-medium">Resting Blood Pressure</td>
                    <td className="py-2 px-3 font-mono font-bold">
                      {madeIndicators.restingBp} / {madeIndicators.diastolicBp} mmHg
                    </td>
                    <td className="py-2 px-3 text-slate-500">&lt; 120/80 mmHg (Normal)</td>
                    <td className="py-2 px-3">
                      {madeIndicators.restingBp >= 140 ? (
                        <span className="text-rose-700 font-bold">Stage 2 HTN</span>
                      ) : (
                        <span className="text-emerald-700">Normotensive</span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td className="py-2 px-3 font-medium">Total Serum Cholesterol</td>
                    <td className="py-2 px-3 font-mono font-bold">
                      {madeIndicators.serumCholesterol} mg/dL
                    </td>
                    <td className="py-2 px-3 text-slate-500">&lt; 200 mg/dL (Desirable)</td>
                    <td className="py-2 px-3">
                      {madeIndicators.serumCholesterol >= 240 ? (
                        <span className="text-rose-700 font-bold">Elevated</span>
                      ) : (
                        <span className="text-emerald-700">Acceptable</span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td className="py-2 px-3 font-medium">Fasting Blood Sugar</td>
                    <td className="py-2 px-3 font-mono font-bold">
                      {madeIndicators.fastingBloodSugar === 1 ? '&gt; 120 mg/dL' : '&lt;= 120 mg/dL'}
                    </td>
                    <td className="py-2 px-3 text-slate-500">&lt;= 100 mg/dL (Fasting)</td>
                    <td className="py-2 px-3">
                      {madeIndicators.fastingBloodSugar === 1 ? (
                        <span className="text-amber-700 font-bold">Diabetic Range</span>
                      ) : (
                        <span className="text-emerald-700">Normal</span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td className="py-2 px-3 font-medium">Max Achieved Heart Rate</td>
                    <td className="py-2 px-3 font-mono font-bold">
                      {madeIndicators.maxHeartRate} bpm
                    </td>
                    <td className="py-2 px-3 text-slate-500">
                      Target: {(220 - madeIndicators.age) * 0.85} bpm
                    </td>
                    <td className="py-2 px-3 text-slate-600">Exercise Stress</td>
                  </tr>
                  <tr>
                    <td className="py-2 px-3 font-medium">ST Depression (Oldpeak)</td>
                    <td className="py-2 px-3 font-mono font-bold">
                      {madeIndicators.oldpeakStDepression.toFixed(1)} mm
                    </td>
                    <td className="py-2 px-3 text-slate-500">&lt; 1.0 mm (Non-ischemic)</td>
                    <td className="py-2 px-3">
                      {madeIndicators.oldpeakStDepression >= 2.0 ? (
                        <span className="text-rose-700 font-bold">Severe Ischemia</span>
                      ) : (
                        <span className="text-emerald-700">Baseline</span>
                      )}
                    </td>
                  </tr>
                  <tr>
                    <td className="py-2 px-3 font-medium">Fluoroscopy Major Vessels</td>
                    <td className="py-2 px-3 font-mono font-bold">
                      {madeIndicators.fluoroscopyVessels} of 3 vessels colored
                    </td>
                    <td className="py-2 px-3 text-slate-500">0 vessels (No stenosis)</td>
                    <td className="py-2 px-3">
                      {madeIndicators.fluoroscopyVessels > 0 ? (
                        <span className="text-rose-700 font-bold">Vessel Occlusion</span>
                      ) : (
                        <span className="text-emerald-700">Clear</span>
                      )}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Section 3: TreeSHAP Feature Attributions Breakdown */}
          <div className="space-y-2">
            <h2 className="text-sm font-bold uppercase tracking-wider text-slate-900">
              3. TreeSHAP Local Feature Attributions (Key Contributors)
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
              <div className="p-3 rounded-lg border border-rose-200 bg-rose-50/40 print:bg-white space-y-2">
                <span className="font-bold text-rose-950 block">Top Risk-Increasing Drivers:</span>
                <ol className="space-y-1.5 list-decimal list-inside text-rose-900">
                  {madeAssessment.topRiskDrivers.slice(0, 3).map((driver) => (
                    <li key={driver.featureName} className="font-medium">
                      <strong>{driver.displayName}</strong>: {driver.clinicalValue}{' '}
                      <span className="font-mono text-rose-700">(+{driver.attributionValue.toFixed(3)})</span>
                    </li>
                  ))}
                </ol>
              </div>

              <div className="p-3 rounded-lg border border-emerald-200 bg-emerald-50/40 print:bg-white space-y-2">
                <span className="font-bold text-emerald-950 block">Key Protective Factors:</span>
                <ul className="space-y-1.5 list-disc list-inside text-emerald-900">
                  {madeAssessment.topProtectiveFactors.slice(0, 2).map((protective) => (
                    <li key={protective.featureName} className="font-medium">
                      <strong>{protective.displayName}</strong>: {protective.clinicalValue}{' '}
                      <span className="font-mono text-emerald-700">({protective.attributionValue.toFixed(3)})</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>

          {/* Section 4: Contextual Clinical Action Plan */}
          <div className="p-4 rounded-xl border border-sky-200 bg-sky-50/50 print:bg-white space-y-2 text-xs">
            <h2 className="text-sm font-bold uppercase tracking-wider text-sky-950">
              4. Synthesized Clinical Guidance & Follow-up Directive
            </h2>
            <p className="text-sm font-semibold text-sky-900 leading-relaxed">
              {actionPlan.orderBadge}: {actionPlan.primaryGuidance}
            </p>
            <div className="pt-2 border-t border-sky-200/80 flex flex-wrap items-center justify-between gap-2 text-sky-800">
              <span>Follow-up Interval: <strong>{actionPlan.monitoringInterval}</strong></span>
              <span>Priority: <strong>{actionPlan.urgencyLabel}</strong></span>
            </div>
          </div>

          {/* Section 5: Mandatory Non-Dismissible CDSS Advisory Banner */}
          <CdssAdvisoryBanner />

          {/* Section 6: Clinician Attestation & Electronic Signature */}
          <div className="p-4 rounded-xl border-2 border-slate-900 bg-white space-y-4 text-xs">
            <h2 className="text-sm font-black uppercase tracking-wider text-slate-900">
              5. Attestation & Electronic Verification
            </h2>

            <label className="flex items-start gap-2.5 cursor-pointer">
              <input
                type="checkbox"
                checked={hasAttested}
                onChange={(e) => setHasAttested(e.target.checked)}
                className="mt-0.5 rounded border-slate-300 text-sky-600 focus:ring-sky-500 h-4 w-4"
              />
              <span className="text-slate-700 leading-normal">
                I attest that I am the licensed healthcare provider managing this patient. I have independently reviewed the physiological indicators, evaluated the non-autonomous CDSS recommendations, and verified that this plan aligns with standard cardiovascular clinical practice.
              </span>
            </label>

            <div className="pt-3 border-t border-slate-200 grid grid-cols-1 sm:grid-cols-3 gap-4 font-mono text-[11px]">
              <div>
                <span className="text-slate-400 block text-[10px] uppercase">Attesting Provider:</span>
                <span className="font-bold text-slate-900">{clinicianName}</span>
                <span className="text-slate-500 block">{clinicianRole}</span>
              </div>

              <div>
                <span className="text-slate-400 block text-[10px] uppercase">Verification Status:</span>
                <span className="font-bold text-slate-900 flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                  {hasAttested ? 'Electronically Attested' : 'Attestation Pending'}
                </span>
                <span className="text-slate-500 block">Single Sign-On (JWT Auth)</span>
              </div>

              <div>
                <span className="text-slate-400 block text-[10px] uppercase">Signature Timestamp:</span>
                <span className="font-bold text-slate-900">{new Date().toISOString()}</span>
                <span className="text-slate-500 block">PULSE CDSS v1.0.0</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
