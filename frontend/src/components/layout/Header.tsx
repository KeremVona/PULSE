import React from 'react';
import { useAppSelector } from '@/store/hooks';
import { HeartPulse, Activity, User, ShieldCheck } from 'lucide-react';
import { Badge } from '@/components/common/Badge';

export const Header: React.FC = () => {
  const authState = useAppSelector((state) => state.auth);
  const encounterState = useAppSelector((state) => state.encounter);

  const madeClinicianName = authState.user?.fullName || 'Dr. Elena Vance, MD';
  const madeRoles = authState.user?.roles || ['Physician'];

  return (
    <header className="bg-white border-b border-slate-200 sticky top-0 z-40">
      <div className="px-6 py-3 flex items-center justify-between gap-4">
        {/* Brand & System Title */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-sky-600 to-indigo-700 flex items-center justify-center text-white shadow-sm shadow-sky-600/20">
            <HeartPulse className="w-6 h-6 animate-pulse" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-base font-bold tracking-tight text-slate-900">
                PULSE CDSS
              </h1>
              <span className="text-[11px] font-semibold uppercase px-1.5 py-0.5 rounded bg-sky-100 text-sky-800 tracking-wider">
                Enterprise
              </span>
            </div>
            <p className="text-xs text-slate-500 font-medium">
              Cardiovascular Risk Stratification & Explainable AI
            </p>
          </div>
        </div>

        {/* Active Patient Context Strip */}
        <div className="hidden lg:flex items-center gap-3 px-4 py-1.5 rounded-lg bg-slate-50 border border-slate-200/80">
          <div className="text-right">
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold text-slate-800">
                {encounterState.activePatientName}
              </span>
              <span className="text-[11px] text-slate-400 font-mono">
                {encounterState.activePatientMrn}
              </span>
            </div>
            <p className="text-[11px] text-slate-500">
              Age: {encounterState.indicators.age} | Sex: {encounterState.indicators.sex === 1 ? 'Male' : 'Female'} | Enc: #{encounterState.encounterNumber}
            </p>
          </div>
          <Badge variant="info">Active Patient</Badge>
        </div>

        {/* System & Clinician Context */}
        <div className="flex items-center gap-4">
          {/* ML Service Health Badge */}
          <div className="hidden sm:flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs font-medium">
            <Activity className="w-3.5 h-3.5 text-emerald-600" />
            <span>ML Engine: Online (95% CI)</span>
          </div>

          {/* Logged Clinician Profile */}
          <div className="flex items-center gap-2.5 pl-3 border-l border-slate-200">
            <div className="w-8 h-8 rounded-full bg-slate-100 border border-slate-200 flex items-center justify-center text-slate-600">
              <User className="w-4 h-4" />
            </div>
            <div className="text-left hidden md:block">
              <div className="flex items-center gap-1.5">
                <span className="text-xs font-semibold text-slate-800">
                  {madeClinicianName}
                </span>
                <ShieldCheck className="w-3.5 h-3.5 text-sky-600" />
              </div>
              <div className="flex items-center gap-1 mt-0.5">
                {madeRoles.map((role) => (
                  <span
                    key={role}
                    className="text-[10px] font-medium text-slate-500 uppercase tracking-wider"
                  >
                    {role}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
};
