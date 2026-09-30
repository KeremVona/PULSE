import React from 'react';
import { ShieldAlert } from 'lucide-react';
import { makeClassName } from '@/utils/cn';

interface CdssBannerProps {
  className?: string;
}

export const CdssBanner: React.FC<CdssBannerProps> = ({ className }) => {
  return (
    <div
      role="region"
      aria-label="Clinical Decision Support Advisory"
      className={makeClassName(
        'w-full bg-amber-500/10 border-b border-amber-500/25 px-4 py-2 text-xs text-amber-950 flex items-center justify-between gap-4 select-none',
        className
      )}
    >
      <div className="flex items-center gap-2.5 max-w-5xl">
        <ShieldAlert className="w-4 h-4 text-amber-600 shrink-0" aria-hidden="true" />
        <p className="leading-snug">
          <strong className="font-semibold text-amber-900 tracking-wide uppercase mr-1">
            CDSS Clinical Advisory:
          </strong>
          PULSE is an assistive clinical decision support system designed to stratify cardiovascular risk using calibrated isotonic models and SHAP explainability. All risk calculations and feature attributions are non-autonomous recommendations and must be verified by a licensed medical clinician.
        </p>
      </div>

      <div className="hidden md:flex items-center gap-2 shrink-0">
        <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-semibold tracking-wider bg-amber-200/80 text-amber-900 uppercase">
          Class IIa CDSS Aid
        </span>
      </div>
    </div>
  );
};
