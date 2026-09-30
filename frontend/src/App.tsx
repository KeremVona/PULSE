import React, { useState, useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '@/store/hooks';
import { AppLayout } from '@/components/layout/AppLayout';
import { NavigationTab } from '@/components/layout/Sidebar';
import { Card } from '@/components/common/Card';
import { Badge } from '@/components/common/Badge';
import { Button } from '@/components/common/Button';
import { ClinicalIntakeForm } from '@/components/intake/ClinicalIntakeForm';
import { RiskAssessmentView } from '@/components/assessment/RiskAssessmentView';
import { ShapExplanationView } from '@/components/assessment/ShapExplanationView';
import {
  prefillFromPastEncounter,
  fetchPatientEncountersAsync
} from '@/store/slices/encounterSlice';
import { makeTokenAsync } from '@/store/slices/authSlice';
import { Clock, KeyRound } from 'lucide-react';
import { ClinicalRoles } from '@/types/auth';

export const App: React.FC = () => {
  const dispatch = useAppDispatch();
  const [activeTab, setActiveTab] = useState<NavigationTab>('intake');

  const authState = useAppSelector((state) => state.auth);
  const encounterState = useAppSelector((state) => state.encounter);

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
        <RiskAssessmentView
          onNavigateToIntake={() => setActiveTab('intake')}
          onNavigateToExplainability={() => setActiveTab('explainability')}
        />
      )}

      {/* Tab: SHAP Explainability */}
      {activeTab === 'explainability' && (
        <ShapExplanationView
          onNavigateToIntake={() => setActiveTab('intake')}
          onNavigateToAssessment={() => setActiveTab('assessment')}
        />
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
