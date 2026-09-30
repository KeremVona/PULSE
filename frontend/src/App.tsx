import React, { useState, useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { AppLayout } from '@/components/layout/AppLayout';
import { NavigationTab } from '@/components/layout/Sidebar';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/common/Card';
import { Badge } from '@/components/common/Badge';
import { Button } from '@/components/common/Button';
import { ClinicalIntakeForm } from '@/components/intake/ClinicalIntakeForm';
import {
  prefillFromPastEncounter,
  fetchPatientEncountersAsync
} from '@/store/slices/encounterSlice';
import { makeTokenAsync } from '@/store/slices/authSlice';
import {
  Activity,
  TrendingUp,
  AlertCircle,
  Clock,
  Sparkles,
  FileText,
  ShieldCheck,
  CheckCircle2,
  KeyRound
} from 'lucide-react';
import { ClinicalRoles } from '@/types/auth';

export const App: React.FC = () => {
  const dispatch = useAppDispatch();
  const [activeTab, setActiveTab] = useState<NavigationTab>('intake');

  const authState = useAppSelector((state) => state.auth);
  const encounterState = useAppSelector((state) => state.encounter);
  const riskState = useAppSelector((state) => state.riskAssessment);

  const madePatientId = encounterState.activePatientId;

  useEffect(() => {
    // Attempt loading historical encounters for the active patient
    dispatch(fetchPatientEncountersAsync({ patientId: madePatientId }));
  }, [dispatch, madePatientId]);

  const makeGenerateToken = () => {
    dispatch(
      makeTokenAsync({
        userId: authState.user?.id || 'd3b07384-d113-40e1-bb69-52e6f4a86b3a',
        username: authState.user?.username || 'dr.elena.vance',
        fullName: authState.user?.fullName || 'Dr. Elena Vance, MD',
        roles: [ClinicalRoles.Physician]
      })
    );
  };

  return (
    <AppLayout activeTab={activeTab} onTabChange={setActiveTab}>
      {/* Clinician Session & Authentication Strip */}
      <div className="flex flex-wrap items-center justify-between gap-3 p-3 bg-white rounded-xl border border-slate-200 text-xs">
        <div className="flex items-center gap-2 text-slate-700">
          <KeyRound className="w-4 h-4 text-sky-600" />
          <span>
            Auth Status: <strong>{authState.isAuthenticated ? 'JWT Bearer Attached' : 'Unauthenticated (Dev Mode)'}</strong>
          </span>
          {authState.token && (
            <span className="font-mono text-[10px] text-slate-400 truncate max-w-xs">
              Token: {authState.token.slice(0, 16)}...
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Button
            size="sm"
            variant="secondary"
            leftIcon={<KeyRound className="w-3.5 h-3.5" />}
            isLoading={authState.isLoading}
            onClick={makeGenerateToken}
          >
            Refresh Physician Token
          </Button>
        </div>
      </div>

      {/* Tab: Clinical Intake Form */}
      {activeTab === 'intake' && (
        <ClinicalIntakeForm onSuccessNavigate={() => setActiveTab('assessment')} />
      )}

      {/* Tab: Risk Stratification */}
      {activeTab === 'assessment' && (
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-slate-900 tracking-tight">
                Calibrated Risk Stratification
              </h2>
              <p className="text-sm text-slate-500">
                Probability calibrated via Isotonic Regression with 95% Bootstrap Confidence Intervals.
              </p>
            </div>
            <Button
              variant="secondary"
              size="sm"
              leftIcon={<FileText className="w-4 h-4" />}
              onClick={() => setActiveTab('intake')}
            >
              Modify Indicators
            </Button>
          </div>

          {riskState.activeAssessment ? (
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              {/* Primary Risk Gauge Card */}
              <Card className="lg:col-span-1 p-6 flex flex-col justify-between items-center text-center">
                <div className="w-full">
                  <span className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                    Cardiovascular Risk Probability
                  </span>

                  <div className="my-6 relative flex flex-col items-center justify-center">
                    <div
                      className={`w-36 h-36 rounded-full border-8 flex flex-col items-center justify-center shadow-inner ${
                        riskState.activeAssessment.riskBand === 'Critical'
                          ? 'border-rose-500 bg-rose-50/50 text-rose-700'
                          : riskState.activeAssessment.riskBand === 'Elevated'
                          ? 'border-orange-500 bg-orange-50/50 text-orange-700'
                          : 'border-emerald-500 bg-emerald-50/50 text-emerald-700'
                      }`}
                    >
                      <span className="text-3xl font-extrabold tracking-tight">
                        {(riskState.activeAssessment.riskProbability * 100).toFixed(1)}%
                      </span>
                      <span className="text-[11px] font-semibold uppercase mt-0.5">
                        {riskState.activeAssessment.riskBand} Risk
                      </span>
                    </div>
                  </div>

                  <div className="space-y-2 text-left bg-slate-50 p-3 rounded-lg border border-slate-100 text-xs">
                    <div className="flex justify-between">
                      <span className="text-slate-500">95% Confidence Interval:</span>
                      <span className="font-semibold text-slate-800 font-mono">
                        [{(riskState.activeAssessment.confidenceInterval.lowerBound * 100).toFixed(1)}% –{' '}
                        {(riskState.activeAssessment.confidenceInterval.upperBound * 100).toFixed(1)}%]
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500">Decision Threshold:</span>
                      <span className="font-semibold text-slate-800 font-mono">
                        {(riskState.activeAssessment.decisionThreshold * 100).toFixed(0)}%
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500">Classification:</span>
                      <span className="font-semibold text-slate-800">
                        {riskState.activeAssessment.isCadPositive ? 'CAD Positive (Stratified)' : 'CAD Negative'}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="w-full mt-4 pt-4 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
                  <span>Latency: {riskState.activeAssessment.inferenceLatencyMs.toFixed(1)} ms</span>
                  <span className="text-emerald-700 font-medium flex items-center gap-1">
                    <CheckCircle2 className="w-3.5 h-3.5" /> Calibrated
                  </span>
                </div>
              </Card>

              {/* Clinical Recommendations & Action Card */}
              <Card className="lg:col-span-2">
                <CardHeader>
                  <CardTitle>Clinical Guidance & Next Steps</CardTitle>
                  <CardDescription>Synthesized decision support recommendations</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="p-4 rounded-xl bg-sky-50 border border-sky-200">
                    <div className="flex items-center gap-2 mb-1">
                      <Sparkles className="w-4 h-4 text-sky-700" />
                      <h4 className="text-sm font-semibold text-sky-900">
                        Recommended Clinical Action
                      </h4>
                    </div>
                    <p className="text-sm text-sky-800 leading-relaxed">
                      {riskState.activeAssessment.recommendedAction}
                    </p>
                  </div>

                  {riskState.activeAssessment.clinicalWarnings.length > 0 && (
                    <div className="p-4 rounded-xl bg-amber-50 border border-amber-200 space-y-2">
                      <div className="flex items-center gap-2">
                        <AlertCircle className="w-4 h-4 text-amber-700" />
                        <h4 className="text-sm font-semibold text-amber-900">
                          Clinical Safety Alerts
                        </h4>
                      </div>
                      <ul className="list-disc list-inside text-xs text-amber-800 space-y-1">
                        {riskState.activeAssessment.clinicalWarnings.map((warn, i) => (
                          <li key={i}>{warn}</li>
                        ))}
                      </ul>
                    </div>
                  )}

                  <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 text-xs text-slate-600">
                    <span className="font-semibold text-slate-800 block mb-1">
                      Regulatory & Model Disclaimer:
                    </span>
                    {riskState.activeAssessment.clinicalDisclaimer}
                  </div>
                </CardContent>
              </Card>
            </div>
          ) : (
            <Card className="p-12 text-center">
              <Activity className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <h3 className="text-base font-semibold text-slate-800">
                No Active Risk Assessment
              </h3>
              <p className="text-sm text-slate-500 max-w-md mx-auto mt-1 mb-4">
                Please submit the clinical indicators on the intake tab to generate an ML-stratified risk evaluation.
              </p>
              <Button onClick={() => setActiveTab('intake')}>
                Go to Clinical Intake
              </Button>
            </Card>
          )}
        </div>
      )}

      {/* Tab: SHAP Explainability */}
      {activeTab === 'explainability' && (
        <div className="space-y-6">
          <div>
            <h2 className="text-xl font-bold text-slate-900 tracking-tight">
              TreeSHAP Feature Attributions
            </h2>
            <p className="text-sm text-slate-500">
              Local feature attributions breaking down risk-increasing and protective drivers.
            </p>
          </div>

          {riskState.activeAssessment ? (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Risk Drivers */}
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <TrendingUp className="w-4 h-4 text-rose-600" />
                    <CardTitle className="text-rose-900">Top Risk Drivers (Increasing Risk)</CardTitle>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  {riskState.activeAssessment.topRiskDrivers.map((driver) => (
                    <div
                      key={driver.featureName}
                      className="p-3 rounded-lg border border-rose-100 bg-rose-50/40 flex items-center justify-between"
                    >
                      <div>
                        <span className="text-sm font-semibold text-slate-900 block">
                          {driver.displayName}
                        </span>
                        <span className="text-xs text-slate-500">
                          Observed Value: <strong>{driver.clinicalValue}</strong>
                        </span>
                      </div>
                      <div className="text-right">
                        <span className="text-sm font-bold text-rose-600 font-mono block">
                          +{driver.attributionValue.toFixed(3)} SHAP
                        </span>
                        <span className="text-[10px] text-slate-400">Rank #{driver.rank}</span>
                      </div>
                    </div>
                  ))}
                </CardContent>
              </Card>

              {/* Protective Factors */}
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <ShieldCheck className="w-4 h-4 text-emerald-600" />
                    <CardTitle className="text-emerald-900">Protective Factors (Decreasing Risk)</CardTitle>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  {riskState.activeAssessment.topProtectiveFactors.length > 0 ? (
                    riskState.activeAssessment.topProtectiveFactors.map((factor) => (
                      <div
                        key={factor.featureName}
                        className="p-3 rounded-lg border border-emerald-100 bg-emerald-50/40 flex items-center justify-between"
                      >
                        <div>
                          <span className="text-sm font-semibold text-slate-900 block">
                            {factor.displayName}
                          </span>
                          <span className="text-xs text-slate-500">
                            Observed Value: <strong>{factor.clinicalValue}</strong>
                          </span>
                        </div>
                        <div className="text-right">
                          <span className="text-sm font-bold text-emerald-600 font-mono block">
                            {factor.attributionValue.toFixed(3)} SHAP
                          </span>
                          <span className="text-[10px] text-slate-400">Rank #{factor.rank}</span>
                        </div>
                      </div>
                    ))
                  ) : (
                    <p className="text-sm text-slate-500 p-4 text-center">
                      No significant protective factors detected for current physiological profile.
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>
          ) : (
            <Card className="p-12 text-center">
              <Sparkles className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <h3 className="text-base font-semibold text-slate-800">
                Awaiting Model Evaluation
              </h3>
              <p className="text-sm text-slate-500 max-w-md mx-auto mt-1 mb-4">
                Execute an intake assessment to visualize SHAP attributions and clinical waterfall drivers.
              </p>
              <Button onClick={() => setActiveTab('intake')}>
                Go to Intake Form
              </Button>
            </Card>
          )}
        </div>
      )}

      {/* Tab: Patient History */}
      {activeTab === 'history' && (
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-slate-900 tracking-tight">
                Historical Encounters for {encounterState.activePatientName}
              </h2>
              <p className="text-sm text-slate-500">
                Audit and longitudinal risk trajectory across outpatient and cardiology encounters.
              </p>
            </div>
            <Button
              variant="secondary"
              size="sm"
              isLoading={encounterState.isLoadingHistory}
              leftIcon={<Clock className="w-4 h-4" />}
              onClick={() => dispatch(fetchPatientEncountersAsync({ patientId: madePatientId }))}
            >
              Refresh History
            </Button>
          </div>

          {encounterState.patientEncounters.length > 0 ? (
            <div className="grid grid-cols-1 gap-4">
              {encounterState.patientEncounters.map((enc) => (
                <Card key={enc.encounterId} className="p-4 hover:border-sky-300 transition-colors">
                  <div className="flex flex-wrap items-center justify-between gap-4">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-bold text-slate-900 font-mono">
                          {enc.encounterNumber}
                        </span>
                        <Badge variant="info">
                          {enc.encounterType === 1
                            ? 'Outpatient'
                            : enc.encounterType === 2
                            ? 'Emergency'
                            : enc.encounterType === 4
                            ? 'Cardiology'
                            : 'Consult'}
                        </Badge>
                      </div>
                      <p className="text-xs text-slate-500 mt-1">
                        {enc.chiefComplaint || 'Routine screening'} • Date: {new Date(enc.encounterDate).toLocaleDateString()}
                      </p>
                    </div>

                    <div className="flex items-center gap-4">
                      {enc.riskAssessment ? (
                        <div className="text-right">
                          <span className="text-sm font-bold text-slate-900 font-mono block">
                            {(enc.riskAssessment.riskProbability * 100).toFixed(1)}%
                          </span>
                          <Badge
                            variant={
                              enc.riskAssessment.riskBand === 'Critical'
                                ? 'critical'
                                : enc.riskAssessment.riskBand === 'Elevated'
                                ? 'elevated'
                                : 'low'
                            }
                          >
                            {enc.riskAssessment.riskBand} Risk
                          </Badge>
                        </div>
                      ) : (
                        <span className="text-xs text-slate-400">Assessment Pending</span>
                      )}

                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => {
                          dispatch(prefillFromPastEncounter(enc));
                          setActiveTab('intake');
                        }}
                      >
                        Load into Intake
                      </Button>
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          ) : (
            <Card className="p-12 text-center">
              <Clock className="w-12 h-12 text-slate-300 mx-auto mb-3" />
              <h3 className="text-base font-semibold text-slate-800">
                No Encounters Recorded Yet
              </h3>
              <p className="text-sm text-slate-500 max-w-md mx-auto mt-1 mb-4">
                Submit an encounter on the intake tab to begin building this patient's clinical longitudinal history.
              </p>
              <Button onClick={() => setActiveTab('intake')}>
                Record New Encounter
              </Button>
            </Card>
          )}
        </div>
      )}

      {/* Tab: Audit & Governance */}
      {activeTab === 'audit' && (
        <div className="space-y-6">
          <div>
            <h2 className="text-xl font-bold text-slate-900 tracking-tight">
              Audit & Compliance Records
            </h2>
            <p className="text-sm text-slate-500">
              GIN-indexed PostgreSQL JSONB audit logs ensuring regulatory accountability.
            </p>
          </div>

          <Card className="p-6 space-y-4">
            <h3 className="text-sm font-semibold text-slate-900">
              System Audit Architecture
            </h3>
            <p className="text-xs text-slate-600 leading-relaxed">
              Every inference call through the ASP.NET Core orchestration pipeline is committed immutably to the <code>prediction_audits</code> table with execution latency, model version hash, exact input parameters, and generated SHAP vectors.
            </p>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-2">
              <div className="p-3 bg-slate-50 rounded-lg border border-slate-200">
                <span className="text-[11px] text-slate-500 block">Model Version</span>
                <span className="text-sm font-semibold text-slate-800 font-mono">v1.0.0</span>
              </div>
              <div className="p-3 bg-slate-50 rounded-lg border border-slate-200">
                <span className="text-[11px] text-slate-500 block">Calibration</span>
                <span className="text-sm font-semibold text-emerald-700">Isotonic (Brier: 0.122)</span>
              </div>
              <div className="p-3 bg-slate-50 rounded-lg border border-slate-200">
                <span className="text-[11px] text-slate-500 block">Target Recall</span>
                <span className="text-sm font-semibold text-sky-700">92.0% Sensitivity</span>
              </div>
              <div className="p-3 bg-slate-50 rounded-lg border border-slate-200">
                <span className="text-[11px] text-slate-500 block">Governance</span>
                <span className="text-sm font-semibold text-slate-800">ISO 13485 / RFC 7807</span>
              </div>
            </div>
          </Card>
        </div>
      )}
    </AppLayout>
  );
};

export default App;
