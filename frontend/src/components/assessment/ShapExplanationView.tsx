import React from 'react';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { ShapAttributionChart } from '@/components/assessment/ShapAttributionChart';
import { ShapAttributionTable } from '@/components/assessment/ShapAttributionTable';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/common/Card';
import { Button } from '@/components/common/Button';
import { Badge } from '@/components/common/Badge';
import {
  FileText,
  Sparkles,
  Binary,
  Layers,
  X
} from 'lucide-react';
import { selectAttribution, setActiveAssessment } from '@/store/slices/riskAssessmentSlice';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';
import { MlFeatureAttribution } from '@/types/clinical';

interface ShapExplanationViewProps {
  onNavigateToIntake: () => void;
  onNavigateToAssessment: () => void;
}

export const ShapExplanationView: React.FC<ShapExplanationViewProps> = ({
  onNavigateToIntake,
  onNavigateToAssessment
}) => {
  const dispatch = useAppDispatch();
  const riskState = useAppSelector((state) => state.riskAssessment);
  const encounterState = useAppSelector((state) => state.encounter);

  const madeAssessment = riskState.activeAssessment;
  const selectedAttribution = riskState.selectedAttribution;

  const makeHandleSelectFeature = (attribution: MlFeatureAttribution) => {
    dispatch(selectAttribution(attribution));
  };

  const makeClearSelectedFeature = () => {
    dispatch(selectAttribution(null));
  };

  const makeLoadDemoAssessment = () => {
    const madeSample = makeSampleRiskAssessment('Elevated');
    dispatch(setActiveAssessment(madeSample));
  };

  if (!madeAssessment) {
    return (
      <Card className="p-12 text-center max-w-2xl mx-auto my-8 border-dashed border-2">
        <Layers className="w-14 h-14 text-slate-300 mx-auto mb-4" />
        <h3 className="text-lg font-bold text-slate-900">
          No Explainability Payload Available
        </h3>
        <p className="text-sm text-slate-500 max-w-md mx-auto mt-2 mb-6">
          Submit clinical indicators on the intake form to generate tree-based SHAP local feature attributions.
        </p>

        <div className="flex flex-wrap items-center justify-center gap-3">
          <Button onClick={onNavigateToIntake} leftIcon={<FileText className="w-4 h-4" />}>
            Go to Clinical Intake
          </Button>

          <Button
            variant="secondary"
            onClick={makeLoadDemoAssessment}
            leftIcon={<Sparkles className="w-4 h-4 text-sky-600" />}
          >
            Load Demo SHAP Attributions
          </Button>
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6" data-testid="shap-explanation-view">
      {/* Top Header & Context */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-xl font-bold text-slate-900 tracking-tight">
              Explainable AI: TreeSHAP Feature Attributions
            </h2>
            <Badge variant="neutral" className="font-mono text-xs">
              Encounter #{encounterState.encounterNumber || 'ENC-ACTIVE'}
            </Badge>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            Local Shapley additive explanations dissecting individual patient risk factors relative to baseline population risk.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="secondary"
            size="sm"
            onClick={onNavigateToAssessment}
          >
            Back to Risk Gauge
          </Button>
          <Button
            variant="secondary"
            size="sm"
            leftIcon={<FileText className="w-4 h-4" />}
            onClick={onNavigateToIntake}
          >
            Modify Indicators
          </Button>
        </div>
      </div>

      {/* Selected Feature Inspector Card (when clinician clicks a bar or row) */}
      {selectedAttribution && (
        <Card className="border-sky-300 bg-sky-50/50 shadow-xs transition-all">
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Binary className="w-5 h-5 text-sky-700" />
                <CardTitle className="text-sm font-bold text-sky-950">
                  Feature Reasoning: {selectedAttribution.displayName}
                </CardTitle>
                <Badge
                  variant={
                    selectedAttribution.direction === 'RISK_INCREASING'
                      ? 'critical'
                      : 'low'
                  }
                  className="text-xs"
                >
                  {selectedAttribution.direction === 'RISK_INCREASING'
                    ? 'Risk Driver'
                    : 'Protective Factor'}
                </Badge>
              </div>

              <Button
                variant="ghost"
                size="sm"
                onClick={makeClearSelectedFeature}
                className="h-7 w-7 p-0 text-slate-500 hover:text-slate-800"
              >
                <X className="w-4 h-4" />
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-xs">
              <div className="p-3 bg-white rounded-lg border border-sky-100">
                <span className="text-slate-500 block mb-1">Patient Finding:</span>
                <span className="text-base font-bold font-mono text-slate-900">
                  {selectedAttribution.clinicalValue}
                </span>
                <span className="text-[10px] text-slate-400 block mt-0.5 font-mono">
                  Indicator: {selectedAttribution.featureName}
                </span>
              </div>

              <div className="p-3 bg-white rounded-lg border border-sky-100">
                <span className="text-slate-500 block mb-1">Additive SHAP Value:</span>
                <span
                  className={`text-base font-bold font-mono ${
                    selectedAttribution.direction === 'RISK_INCREASING'
                      ? 'text-rose-600'
                      : 'text-emerald-600'
                  }`}
                >
                  {selectedAttribution.attributionValue > 0
                    ? `+${selectedAttribution.attributionValue.toFixed(4)}`
                    : selectedAttribution.attributionValue.toFixed(4)}
                </span>
                <span className="text-[10px] text-slate-400 block mt-0.5">
                  {(Math.abs(selectedAttribution.attributionValue) * 100).toFixed(1)}% calibrated impact
                </span>
              </div>

              <div className="p-3 bg-white rounded-lg border border-sky-100">
                <span className="text-slate-500 block mb-1">Model Impact Rank:</span>
                <span className="text-base font-bold text-slate-900">
                  Rank #{selectedAttribution.rank} of {madeAssessment.allAttributions.length}
                </span>
                <span className="text-[10px] text-slate-400 block mt-0.5">
                  Ordered by absolute Shapley contribution
                </span>
              </div>
            </div>

            <p className="text-xs text-sky-900 mt-3 pt-3 border-t border-sky-100/80 leading-relaxed">
              {selectedAttribution.direction === 'RISK_INCREASING'
                ? `In the TreeSHAP formulation, this patient's ${selectedAttribution.displayName.toLowerCase()} of ${selectedAttribution.clinicalValue} pushed the ensemble log-odds higher by +${selectedAttribution.attributionValue.toFixed(3)}, significantly increasing the predicted probability of obstructive coronary artery disease.`
                : `In the TreeSHAP formulation, this patient's ${selectedAttribution.displayName.toLowerCase()} of ${selectedAttribution.clinicalValue} pushed the ensemble log-odds lower by ${selectedAttribution.attributionValue.toFixed(3)}, exerting a clinically protective influence against coronary risk.`}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Primary Section: Recharts Two-Sided Horizontal Bar Waterfall Chart */}
      <ShapAttributionChart
        attributions={madeAssessment.allAttributions}
        onSelectFeature={makeHandleSelectFeature}
      />

      {/* Secondary Section: Detailed Feature Attribution Table */}
      <ShapAttributionTable
        attributions={madeAssessment.allAttributions}
        selectedFeatureName={selectedAttribution?.featureName}
        onSelectFeature={makeHandleSelectFeature}
      />
    </div>
  );
};
