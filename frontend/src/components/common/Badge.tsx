import React from 'react';
import { makeClassName } from '@/utils/cn';

export type BadgeVariant = 'low' | 'moderate' | 'elevated' | 'critical' | 'info' | 'neutral' | 'success';

interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  children: React.ReactNode;
  showDot?: boolean;
}

const variantStyles: Record<BadgeVariant, string> = {
  low: 'bg-emerald-50 text-emerald-800 border-emerald-200 ring-emerald-500/20',
  moderate: 'bg-amber-50 text-amber-800 border-amber-200 ring-amber-500/20',
  elevated: 'bg-orange-50 text-orange-800 border-orange-200 ring-orange-500/20',
  critical: 'bg-rose-50 text-rose-800 border-rose-200 ring-rose-500/20',
  info: 'bg-sky-50 text-sky-800 border-sky-200 ring-sky-500/20',
  neutral: 'bg-slate-100 text-slate-700 border-slate-200 ring-slate-400/20',
  success: 'bg-teal-50 text-teal-800 border-teal-200 ring-teal-500/20'
};

const dotStyles: Record<BadgeVariant, string> = {
  low: 'bg-emerald-500',
  moderate: 'bg-amber-500',
  elevated: 'bg-orange-500',
  critical: 'bg-rose-500',
  info: 'bg-sky-500',
  neutral: 'bg-slate-400',
  success: 'bg-teal-500'
};

export const Badge: React.FC<BadgeProps> = ({
  variant = 'neutral',
  children,
  showDot = false,
  className,
  ...props
}) => {
  return (
    <span
      className={makeClassName(
        'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium border transition-colors',
        variantStyles[variant],
        className
      )}
      {...props}
    >
      {showDot && (
        <span
          className={makeClassName('w-1.5 h-1.5 rounded-full', dotStyles[variant])}
          aria-hidden="true"
        />
      )}
      {children}
    </span>
  );
};
