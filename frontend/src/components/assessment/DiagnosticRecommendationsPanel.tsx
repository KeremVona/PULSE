import React, { useMemo } from 'react';
import { RiskAssessmentPayload } from '@/types/clinical';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/common/Card';
import { Button } from '@/components/common/Button';
import { CdssAdvisoryBanner } from '@/components/common/CdssAdvisoryBanner';
import {
  Sparkles,
  Stethoscope,
  FileDown,
  Copy,
  Activity,
  Pill,
  CalendarClock
} from 'lucide-react';
import { useAppDispatch } from '@/store/hooks';
import { addNotification } from '@/store/slices/notificationSlice';
import { makeClassName } from '@/utils/cn';

interface DiagnosticRecommendationsPanelProps {
  assessment: RiskAssessmentPayload;
  onOpenReportExport: () => void;
  className?: string;
}

export interface ClinicalActionPlan {
  primaryGuidance: string;
  orderBadge: string;
  diagnosticWorkup: string[];
  pharmacotherapy: string[];
  monitoringInterval: string;
  urgencyLabel: string;
  urgencyColor: string;
}

export function makeClinicalActionPlan(
  tier: 'Low' | 'Moderate' | 'Elevated' | 'Critical'
): ClinicalActionPlan {
  switch (tier) {
    case 'Low':
      return {
        primaryGuidance:
          'Maintain regular preventive cardiovascular wellness. Current indicators suggest low probability of obstructive coronary disease.',
        orderBadge: 'Routine Primary Care Follow-up',
        diagnosticWorkup: [
          'No invasive testing or cardiac imaging indicated at this juncture.',
          'Annual lipid panel and fasting blood glucose evaluation.',
          'Repeat clinical risk score screening in 12–24 months.'
        ],
        pharmacotherapy: [
          'Continue baseline preventive measures.',
          'Lifestyle and dietary counseling consistent with AHA dietary guidelines.'
        ],
        monitoringInterval: 'Routine annual follow-up (12–24 months)',
        urgencyLabel: 'Elective / Routine',
        urgencyColor: 'text-emerald-700 bg-emerald-50 border-emerald-200'
      };

    case 'Moderate':
      return {
        primaryGuidance:
          'Schedule outpatient cardiovascular evaluation. Consider non-imaging exercise treadmill testing or coronary artery calcium (CAC) scoring.',
        orderBadge: 'Consider Exercise Treadmill Test (ETT)',
        diagnosticWorkup: [
          'Exercise Treadmill Test (ETT) without nuclear imaging to evaluate exertional symptoms.',
          'Consider Coronary Artery Calcium (CAC) CT scan for intermediate risk reclassification.',
          'Comprehensive fasting lipid profile and hs-CRP inflammatory marker.'
        ],
        pharmacotherapy: [
          'Moderate-intensity statin therapy (e.g., Atorvastatin 20 mg daily) to target LDL < 100 mg/dL.',
          'Review anti-hypertensive regimen to maintain blood pressure < 130/80 mmHg.'
        ],
        monitoringInterval: 'Outpatient cardiology visit in 4–6 weeks',
        urgencyLabel: 'Moderate Priority',
        urgencyColor: 'text-amber-700 bg-amber-50 border-amber-200'
      };

    case 'Elevated':
      return {
        primaryGuidance:
          'Recommend Stress Echocardiogram or Myocardial Perfusion SPECT. High likelihood of inducible myocardial ischemia requiring prompt diagnostic clarification.',
        orderBadge: 'Recommend Stress Echocardiogram / SPECT',
        diagnosticWorkup: [
          'Stress Echocardiography or SPECT Myocardial Perfusion Imaging (MPI) within 7–14 days.',
          'Resting transthoracic echocardiogram (TTE) to evaluate left ventricular ejection fraction and regional wall motion abnormalities.',
          'Cardiology consultation within 1–2 weeks.'
        ],
        pharmacotherapy: [
          'Initiate high-intensity statin therapy (Atorvastatin 40–80 mg daily) targeting LDL < 70 mg/dL.',
          'Low-dose Aspirin (81 mg daily) unless specifically contraindicated.',
          'Optimize beta-blocker or calcium channel blocker if anginal symptoms persist.'
        ],
        monitoringInterval: 'Expedited cardiology consult (1–2 weeks)',
        urgencyLabel: 'Elevated Priority',
        urgencyColor: 'text-orange-700 bg-orange-50 border-orange-200'
      };

    case 'Critical':
      return {
        primaryGuidance:
          'Urgent cardiology consultation and inpatient cardiac triage recommended. High probability of critical multi-vessel coronary stenosis or impending ischemic event.',
        orderBadge: 'Urgent Invasive Angiography Consult',
        diagnosticWorkup: [
          'Immediate cardiology consult and priority scheduling for diagnostic coronary angiography (ICA).',
          'Continuous cardiac telemetry monitoring and serial cardiac troponin evaluation.',
          'Bedside echocardiography to assess acute valvular or hemodynamic instability.'
        ],
        pharmacotherapy: [
          'Dual antiplatelet therapy (DAPT) evaluation: Aspirin + P2Y12 inhibitor.',
          'Anticoagulation bridging per institutional acute coronary syndrome (ACS) protocols.',
          'High-intensity statin and intravenous hemodynamic stabilization as clinically indicated.'
        ],
        monitoringInterval: 'Immediate emergency / inpatient triage',
        urgencyLabel: 'Critical / Urgent',
        urgencyColor: 'text-rose-700 bg-rose-50 border-rose-200'
      };
  }
}

export const DiagnosticRecommendationsPanel: React.FC<DiagnosticRecommendationsPanelProps> = ({
  assessment,
  onOpenReportExport,
  className
}) => {
  const dispatch = useAppDispatch();
  const madePlan = useMemo(() => makeClinicalActionPlan(assessment.riskBand), [assessment.riskBand]);

  const makeCopyActionPlan = async () => {
    const text = `PULSE CDSS - CLINICAL ACTION PLAN (${assessment.riskBand.toUpperCase()} RISK)\n` +
      `Primary Directive: ${madePlan.primaryGuidance}\n` +
      `Recommended Order: ${madePlan.orderBadge}\n` +
      `Diagnostic Workup:\n${madePlan.diagnosticWorkup.map((d) => `- ${d}`).join('\n')}\n` +
      `Pharmacotherapy Guidance:\n${madePlan.pharmacotherapy.map((p) => `- ${p}`).join('\n')}\n` +
      `Follow-up Horizon: ${madePlan.monitoringInterval}\n` +
      `Advisory: Non-autonomous decision support. Verify clinically.`;

    try {
      await navigator.clipboard.writeText(text);
      dispatch(
        addNotification({
          type: 'success',
          title: 'Action Plan Copied',
          message: 'Clinical recommendations copied to clipboard in standardized format.'
        })
      );
    } catch {
      dispatch(
        addNotification({
          type: 'info',
          title: 'Clipboard Action',
          message: 'Clinical guidance ready for manual review.'
        })
      );
    }
  };

  return (
    <div
      role="region"
      aria-label="Diagnostic Recommendations and Clinical Action Plan"
      data-testid="diagnostic-recommendations-panel"
      className={makeClassName('space-y-4', className)}
    >
      {/* Non-Dismissible CDSS Advisory Banner Anchored Directly Above Controls */}
      <CdssAdvisoryBanner />

      <Card className="border-slate-200 shadow-xs">
        <CardHeader className="pb-3 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-lg bg-sky-50 text-sky-700 border border-sky-200">
              <Stethoscope className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-base text-slate-900">
                Diagnostic Recommendations & Clinical Action Plan
              </CardTitle>
              <CardDescription className="text-xs text-slate-500">
                Contextual clinical directives synthesized from calibrated risk and patient vital profile
              </CardDescription>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <span
              className={makeClassName(
                'px-2.5 py-1 rounded-md text-xs font-bold border font-mono',
                madePlan.urgencyColor
              )}
            >
              {madePlan.urgencyLabel}
            </span>
          </div>
        </CardHeader>

        <CardContent className="space-y-5 pt-4">
          {/* Primary Recommended Order Highlight Box */}
          <div className="p-4 rounded-xl bg-sky-50/70 border border-sky-200/80 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div className="space-y-1">
              <span className="text-[11px] font-bold text-sky-900 tracking-wider uppercase flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5 text-sky-600" /> Key Clinical Order Recommendation
              </span>
              <h4 className="text-base font-extrabold text-sky-950">
                {madePlan.orderBadge}
              </h4>
              <p className="text-xs text-sky-800 leading-relaxed max-w-2xl">
                {madePlan.primaryGuidance}
              </p>
            </div>

            <Button
              size="sm"
              variant="primary"
              leftIcon={<FileDown className="w-4 h-4" />}
              onClick={onOpenReportExport}
              className="shrink-0"
            >
              Export Clinical Note
            </Button>
          </div>

          {/* Two-Column Grid: Diagnostic Workup vs Pharmacotherapy */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
            {/* Diagnostic Workup */}
            <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 space-y-2.5">
              <div className="flex items-center gap-1.5 font-bold text-slate-900">
                <Activity className="w-4 h-4 text-sky-600" />
                <span>Recommended Diagnostic Workup</span>
              </div>
              <ul className="space-y-2 text-slate-700">
                {madePlan.diagnosticWorkup.map((item, i) => (
                  <li key={i} className="flex items-start gap-2">
                    <span className="w-1.5 h-1.5 rounded-full bg-sky-500 mt-1.5 shrink-0" />
                    <span>{item}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* Pharmacotherapy Guidance */}
            <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 space-y-2.5">
              <div className="flex items-center gap-1.5 font-bold text-slate-900">
                <Pill className="w-4 h-4 text-amber-600" />
                <span>Pharmacotherapy & Secondary Prevention</span>
              </div>
              <ul className="space-y-2 text-slate-700">
                {madePlan.pharmacotherapy.map((item, i) => (
                  <li key={i} className="flex items-start gap-2">
                    <span className="w-1.5 h-1.5 rounded-full bg-amber-500 mt-1.5 shrink-0" />
                    <span>{item}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Follow-up Horizon Strip */}
          <div className="flex items-center justify-between p-3 rounded-lg bg-slate-100/70 border border-slate-200 text-xs text-slate-700">
            <div className="flex items-center gap-2">
              <CalendarClock className="w-4 h-4 text-slate-500" />
              <span>Recommended Follow-up Horizon:</span>
              <strong className="text-slate-900">{madePlan.monitoringInterval}</strong>
            </div>

            <button
              type="button"
              onClick={makeCopyActionPlan}
              className="text-sky-700 hover:text-sky-900 font-medium inline-flex items-center gap-1 transition-colors cursor-pointer"
            >
              <Copy className="w-3.5 h-3.5" /> Copy Action Plan
            </button>
          </div>
        </CardContent>

        <CardFooter className="bg-slate-50/50 border-t border-slate-100 flex flex-wrap items-center justify-between gap-3 text-xs">
          <span className="text-slate-500">
            Guidance aligned with ACC/AHA Prevention Guidelines and Isotonic CDSS stratification.
          </span>

          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant="secondary"
              leftIcon={<Copy className="w-3.5 h-3.5" />}
              onClick={makeCopyActionPlan}
            >
              Copy Guidance
            </Button>
            <Button
              size="sm"
              variant="primary"
              leftIcon={<FileDown className="w-3.5 h-3.5" />}
              onClick={onOpenReportExport}
            >
              Export Clinical Note
            </Button>
          </div>
        </CardFooter>
      </Card>
    </div>
  );
};
