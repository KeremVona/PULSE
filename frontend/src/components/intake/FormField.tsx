import React from 'react';
import { makeClassName } from '@/utils/cn';
import { AlertCircle, AlertTriangle, Info } from 'lucide-react';
import { PhysiologicalWarning } from '@/validation/encounterSchema';

interface FormFieldProps {
  label: string;
  name: string;
  error?: string;
  warning?: PhysiologicalWarning;
  unit?: string;
  hint?: string;
  required?: boolean;
  children: React.ReactNode;
  className?: string;
}

export const FormField: React.FC<FormFieldProps> = ({
  label,
  name,
  error,
  warning,
  unit,
  hint,
  required = false,
  children,
  className
}) => {
  const madeErrorId = `${name}-error`;
  const madeWarningId = `${name}-warning`;
  const madeHintId = `${name}-hint`;

  return (
    <div className={makeClassName('space-y-1.5', className)}>
      <div className="flex items-center justify-between gap-2">
        <label
          htmlFor={name}
          className="block text-xs font-semibold text-slate-800 tracking-tight"
        >
          {label}
          {required && <span className="text-rose-500 ml-0.5">*</span>}
        </label>
        {unit && (
          <span className="text-[11px] font-mono font-medium text-slate-400 bg-slate-100 px-1.5 py-0.5 rounded">
            {unit}
          </span>
        )}
      </div>

      <div className="relative rounded-lg">
        {children}
      </div>

      {hint && !error && !warning && (
        <p id={madeHintId} className="text-[11px] text-slate-400 flex items-center gap-1 mt-0.5">
          <Info className="w-3 h-3 text-slate-400 shrink-0" />
          <span>{hint}</span>
        </p>
      )}

      {/* Real-time Field Error (Red) */}
      {error && (
        <p
          id={madeErrorId}
          role="alert"
          className="text-xs text-rose-600 font-medium flex items-center gap-1 animate-in fade-in duration-150"
        >
          <AlertCircle className="w-3.5 h-3.5 shrink-0" />
          <span>{error}</span>
        </p>
      )}

      {/* Real-time Physiological Clinical Warning (Amber/Rose) */}
      {warning && !error && (
        <div
          id={madeWarningId}
          className={makeClassName(
            'p-2 rounded-md border text-xs flex items-start gap-1.5 animate-in fade-in duration-150',
            warning.severity === 'critical'
              ? 'bg-rose-50 border-rose-200 text-rose-800'
              : 'bg-amber-50 border-amber-200 text-amber-800'
          )}
        >
          <AlertTriangle
            className={makeClassName(
              'w-3.5 h-3.5 shrink-0 mt-0.5',
              warning.severity === 'critical' ? 'text-rose-600' : 'text-amber-600'
            )}
          />
          <div className="leading-tight">
            <span className="font-semibold block">{warning.title}:</span>
            <span>{warning.message}</span>
          </div>
        </div>
      )}
    </div>
  );
};
