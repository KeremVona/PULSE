import React from 'react';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { RiskAssessmentGauge } from '@/components/assessment/RiskAssessmentGauge';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/common/Card';
import { Button } from '@/components/common/Button';
import { Badge } from '@/components/common/Badge';
import {
  Sparkles,
  AlertTriangle,
  FileText,
  Activity,
  ArrowRight,
  TrendingUp,
  ShieldCheck,
  Stethoscope
} from 'lucide-react';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';
import { setActiveAssessment } from '@/store/slices/riskAssessmentSlice';

interface RiskAssessmentViewProps {
  onNavigateToIntake: () => void;
  onNavigateToExplainability: () => void;
}

export const RiskAssessmentView: React.FC<RiskAssessmentViewProps> = ({
  onNavigateToIntake,
  onNavigateToExplainability
}) => {
  const dispatch = useAppDispatch();
  const riskState = useAppSelector((state) => state.riskAssessment);
  const encounterState = useAppSelector((state) => state.encounter);

  const madeAssessment = riskState.activeAssessment;

  const makeLoadDemoAssessment = (tier: 'Low' | 'Moderate' | 'Elevated' | 'Critical') => {
    const madeSample = makeSampleRiskAssessment(tier);
    dispatch(setActiveAssessment(madeSample));
  };

  if (!madeAssessment) {
    return (
      <Card className="p-12 text-center max-w-2xl mx-auto my-8 border-dashed border-2">
        <Activity className="w-14 h-14 text-slate-300 mx-auto mb-4" />
        <h3 className="text-lg font-bold text-slate-900">
          No Active Risk Assessment Recorded
        </h3>
        <p className="text-sm text-slate-500 max-w-md mx-auto mt-2 mb-6">
          Submit the patient's physiological indicators in the Clinical Intake form to run the calibrated ML inference pipeline.
        </p>

        <div className="flex flex-wrap items-center justify-center gap-3">
          <Button onClick={onNavigateToIntake} leftIcon={<FileText className="w-4 h-4" />}>
            Go to Clinical Intake
          </Button>

          <Button
            variant="secondary"
            onClick={() => makeLoadDemoAssessment('Elevated')}
            leftIcon={<Sparkles className="w-4 h-4 text-sky-600" />}
          >
            Load Demo Assessment (Elevated)
          </Button>
        </div>

        <div className="mt-8 pt-6 border-t border-slate-100 flex flex-wrap items-center justify-center gap-2 text-xs text-slate-400">
          <span>Quick Demo Presets:</span>
          {(['Low', 'Moderate', 'Elevated', 'Critical'] as const).map((tier) => (
            <button
              key={tier}
              type="button"
              onClick={() => makeLoadDemoAssessment(tier)}
              className="px-2.5 py-1 rounded-md bg-slate-100 hover:bg-slate-200 text-slate-700 font-medium transition-colors"
            >
              {tier} Risk
            </button>
          ))}
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6" data-testid="risk-assessment-view">
      {/* Top Context & Action Strip */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-xl font-bold text-slate-900 tracking-tight">
              Calibrated Cardiovascular Risk Stratification
            </h2>
            <Badge variant="neutral" className="font-mono text-xs">
              Encounter #{encounterState.encounterNumber || 'ENC-ACTIVE'}
            </Badge>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            Isotonically calibrated probability with 95% bootstrap confidence intervals and guideline-aligned triage.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="secondary"
            size="sm"
            leftIcon={<FileText className="w-4 h-4" />}
            onClick={onNavigateToIntake}
          >
            Modify Indicators
          </Button>

          <Button
            size="sm"
            rightIcon={<ArrowRight className="w-4 h-4" />}
            onClick={onNavigateToExplainability}
          >
            View SHAP Attributions
          </Button>
        </div>
      </div>

      {/* Main Grid: Gauge Left (1 col) + Clinical Guidance Right (2 cols) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column: Calibrated Risk Gauge */}
        <div className="lg:col-span-1">
          <RiskAssessmentGauge assessment={madeAssessment} />
        </div>

        {/* Right Column: Guidance, Safety Warnings & Top Drivers */}
        <div className="lg:col-span-2 space-y-6">
          {/* Clinical Action Card */}
          <Card className="border-sky-200 bg-sky-50/40">
            <CardHeader className="pb-2">
              <div className="flex items-center gap-2">
                <Sparkles className="w-5 h-5 text-sky-700" />
                <CardTitle className="text-sky-950 text-base">
                  Recommended Clinical Action
                </CardTitle>
              </div>
              <CardDescription className="text-sky-800 text-xs">
                Contextual CDSS guidance aligned with AHA/ACC guidelines
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-sm font-medium text-sky-900 leading-relaxed">
                {madeAssessment.recommendedAction}
              </p>
            </CardContent>
          </Card>

          {/* Clinical Safety Alerts Banner (if present) */}
          {madeAssessment.clinicalWarnings.length > 0 && (
            <Card className="border-amber-300 bg-amber-50/50">
              <CardHeader className="pb-2">
                <div className="flex items-center gap-2">
                  <AlertTriangle className="w-5 h-5 text-amber-700" />
                  <CardTitle className="text-amber-950 text-base">
                    Active Clinical Safety Alerts
                  </CardTitle>
                </div>
              </CardHeader>
              <CardContent>
                <ul className="list-disc list-inside text-xs text-amber-900 space-y-1.5 font-medium">
                  {madeAssessment.clinicalWarnings.map((warning, index) => (
                    <li key={index}>{warning}</li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}

          {/* Quick Summary of Top Drivers */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Top Risk Drivers */}
            <Card>
              <CardHeader className="pb-2">
                <div className="flex items-center gap-2">
                  <TrendingUp className="w-4 h-4 text-rose-600" />
                  <CardTitle className="text-sm text-rose-900">
                    Key Risk Drivers
                  </CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-2">
                {madeAssessment.topRiskDrivers.slice(0, 3).map((driver) => (
                  <div
                    key={driver.featureName}
                    className="flex items-center justify-between p-2 rounded-lg bg-rose-50/60 border border-rose-100 text-xs"
                  >
                    <div>
                      <span className="font-semibold text-rose-950 block">
                        {driver.displayName}
                      </span>
                      <span className="text-[11px] text-rose-700 font-mono">
                        {driver.clinicalValue}
                      </span>
                    </div>
                    <span className="font-bold font-mono text-rose-600">
                      +{driver.attributionValue.toFixed(3)}
                    </span>
                  </div>
                ))}
              </CardContent>
            </Card>

            {/* Top Protective Factors */}
            <Card>
              <CardHeader className="pb-2">
                <div className="flex items-center gap-2">
                  <ShieldCheck className="w-4 h-4 text-emerald-600" />
                  <CardTitle className="text-sm text-emerald-900">
                    Key Protective Factors
                  </CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-2">
                {madeAssessment.topProtectiveFactors.slice(0, 3).map((protective) => (
                  <div
                    key={protective.featureName}
                    className="flex items-center justify-between p-2 rounded-lg bg-emerald-50/60 border border-emerald-100 text-xs"
                  >
                    <div>
                      <span className="font-semibold text-emerald-950 block">
                        {protective.displayName}
                      </span>
                      <span className="text-[11px] text-emerald-700 font-mono">
                        {protective.clinicalValue}
                      </span>
                    </div>
                    <span className="font-bold font-mono text-emerald-600">
                      {protective.attributionValue.toFixed(3)}
                    </span>
                  </div>
                ))}
              </CardContent>
            </Card>
          </div>

          {/* Regulatory Disclaimer Card */}
          <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 text-xs text-slate-500 flex items-start gap-2.5">
            <Stethoscope className="w-4 h-4 text-slate-400 mt-0.5 shrink-0" />
            <div>
              <span className="font-semibold text-slate-700 block mb-0.5">
                Physician Advisory:
              </span>
              <p>{madeAssessment.clinicalDisclaimer}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
