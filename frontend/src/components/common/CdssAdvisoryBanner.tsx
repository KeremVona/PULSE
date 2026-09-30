import React from 'react';
import { ShieldAlert, FileCheck } from 'lucide-react';
import { makeClassName } from '@/utils/cn';

interface CdssAdvisoryBannerProps {
  className?: string;
  compact?: boolean;
}

export const CdssAdvisoryBanner: React.FC<CdssAdvisoryBannerProps> = ({
  className,
  compact = false
}) => {
  return (
    <aside
      role="region"
      aria-label="Mandatory Clinical Decision Support Advisory"
      data-testid="cdss-advisory-banner"
      className={makeClassName(
        'w-full bg-amber-50 border border-amber-300 rounded-xl p-4 text-amber-950 shadow-xs relative select-none',
        compact ? 'py-2.5 px-3.5 text-xs' : 'p-4 text-xs',
        className
      )}
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-start gap-3">
          <div className="p-1.5 rounded-lg bg-amber-100 text-amber-800 shrink-0 mt-0.5">
            <ShieldAlert className="w-5 h-5" aria-hidden="true" />
          </div>

          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <span className="font-bold text-amber-950 uppercase tracking-wide text-[11px]">
                Mandatory CDSS Advisory Notice
              </span>
              <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-amber-200/80 text-amber-900 border border-amber-300">
                Non-Dismissible Regulatory Region
              </span>
            </div>

            <p className="text-amber-900 leading-relaxed font-normal">
              PULSE CDSS functions exclusively as an assistive clinical decision support tool. Calibrated probabilities, 95% bootstrap confidence bounds, and local TreeSHAP attributions augment physician evaluation and do not constitute autonomous medical determinations or binding clinical directives. Final diagnosis, diagnostic ordering, and therapeutic management remain the sole responsibility of the licensed healthcare clinician.
            </p>
          </div>
        </div>

        <div className="hidden lg:flex flex-col items-end gap-1 shrink-0 text-right">
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-[10px] font-semibold tracking-wide bg-amber-100/90 text-amber-900 border border-amber-200 font-mono">
            <FileCheck className="w-3.5 h-3.5" /> EU MDR / FDA SaMD Class IIa
          </span>
          <span className="text-[10px] text-amber-700 font-mono">
            Audit ID: CDSS-REG-2026-B
          </span>
        </div>
      </div>
    </aside>
  );
};
