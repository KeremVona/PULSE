import React from 'react';
import { useAppSelector } from '@/store/hooks';
import {
  ClipboardList,
  Activity,
  BarChart3,
  History,
  FileCheck2,
  ChevronRight
} from 'lucide-react';
import { makeClassName } from '@/utils/cn';

export type NavigationTab = 'intake' | 'assessment' | 'explainability' | 'history' | 'audit';

interface SidebarProps {
  activeTab: NavigationTab;
  onTabChange: (tab: NavigationTab) => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ activeTab, onTabChange }) => {
  const encounterState = useAppSelector((state) => state.encounter);
  const riskState = useAppSelector((state) => state.riskAssessment);

  const navigationItems = [
    {
      id: 'intake' as NavigationTab,
      label: 'Clinical Intake',
      description: 'Physiological indicators entry',
      icon: ClipboardList,
      badge: encounterState.isDirty ? 'Unsaved' : undefined,
      badgeColor: 'bg-amber-100 text-amber-800'
    },
    {
      id: 'assessment' as NavigationTab,
      label: 'Risk Stratification',
      description: 'Calibrated probability & band',
      icon: Activity,
      badge: riskState.activeAssessment ? `${(riskState.activeAssessment.riskProbability * 100).toFixed(0)}%` : undefined,
      badgeColor:
        riskState.activeAssessment?.riskBand === 'Critical'
          ? 'bg-rose-100 text-rose-800'
          : riskState.activeAssessment?.riskBand === 'Elevated'
          ? 'bg-orange-100 text-orange-800'
          : 'bg-emerald-100 text-emerald-800'
    },
    {
      id: 'explainability' as NavigationTab,
      label: 'SHAP Explainability',
      description: 'Feature attributions & drivers',
      icon: BarChart3,
      badge: riskState.activeAssessment?.topRiskDrivers.length ? `${riskState.activeAssessment.topRiskDrivers.length} Drivers` : undefined,
      badgeColor: 'bg-sky-100 text-sky-800'
    },
    {
      id: 'history' as NavigationTab,
      label: 'Patient History',
      description: 'Past encounter records',
      icon: History,
      badge: encounterState.patientEncounters.length > 0 ? `${encounterState.patientEncounters.length}` : undefined,
      badgeColor: 'bg-slate-100 text-slate-800'
    },
    {
      id: 'audit' as NavigationTab,
      label: 'Audit & Governance',
      description: 'GIN-indexed prediction logs',
      icon: FileCheck2
    }
  ];

  return (
    <aside className="w-72 bg-white border-r border-slate-200 flex flex-col justify-between select-none">
      <div className="p-4 space-y-1">
        <div className="px-3 py-2 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
          Clinical Navigation
        </div>

        <nav className="space-y-1" aria-label="Clinical Portal Navigation">
          {navigationItems.map((item) => {
            const Icon = item.icon;
            const isCurrent = activeTab === item.id;

            return (
              <button
                key={item.id}
                type="button"
                onClick={() => onTabChange(item.id)}
                className={makeClassName(
                  'w-full flex items-center justify-between p-3 rounded-xl text-left transition-all group cursor-pointer',
                  isCurrent
                    ? 'bg-sky-50 text-sky-900 border border-sky-200/80 shadow-xs'
                    : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                )}
              >
                <div className="flex items-center gap-3 min-w-0">
                  <div
                    className={makeClassName(
                      'w-8 h-8 rounded-lg flex items-center justify-center transition-colors',
                      isCurrent
                        ? 'bg-sky-600 text-white'
                        : 'bg-slate-100 text-slate-500 group-hover:bg-slate-200 group-hover:text-slate-700'
                    )}
                  >
                    <Icon className="w-4 h-4" />
                  </div>
                  <div className="truncate">
                    <span className="block text-sm font-semibold truncate">
                      {item.label}
                    </span>
                    <span className="block text-[11px] text-slate-500 truncate">
                      {item.description}
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-1.5 ml-2 shrink-0">
                  {item.badge && (
                    <span
                      className={makeClassName(
                        'text-[10px] font-semibold px-2 py-0.5 rounded-full',
                        item.badgeColor
                      )}
                    >
                      {item.badge}
                    </span>
                  )}
                  <ChevronRight
                    className={makeClassName(
                      'w-4 h-4 transition-transform text-slate-400',
                      isCurrent ? 'text-sky-700 translate-x-0.5' : 'group-hover:translate-x-0.5'
                    )}
                  />
                </div>
              </button>
            );
          })}
        </nav>
      </div>

      {/* Governance & Architecture Footprint */}
      <div className="p-4 border-t border-slate-100 bg-slate-50/50">
        <div className="rounded-xl bg-white border border-slate-200 p-3 shadow-2xs">
          <div className="flex items-center gap-2 mb-1.5">
            <span className="w-2 h-2 rounded-full bg-emerald-500 animate-ping" />
            <span className="text-xs font-semibold text-slate-800">
              PULSE CDSS Core
            </span>
          </div>
          <p className="text-[11px] text-slate-500 leading-tight">
            Calibrated Isotonic Classifier with 95% Confidence Bounds & TreeSHAP Attribution.
          </p>
          <div className="mt-2 pt-2 border-t border-slate-100 flex items-center justify-between text-[10px] text-slate-400">
            <span>Version 1.0.0</span>
            <span>FHIR R4 Aligned</span>
          </div>
        </div>
      </div>
    </aside>
  );
};
