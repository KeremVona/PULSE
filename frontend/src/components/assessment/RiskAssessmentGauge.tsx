import React, { useMemo } from 'react';
import { RiskAssessmentPayload } from '@/types/clinical';
import { Badge } from '@/components/common/Badge';
import { CheckCircle2, AlertOctagon, HelpCircle } from 'lucide-react';
import { makeClassName } from '@/utils/cn';

interface RiskAssessmentGaugeProps {
  assessment: RiskAssessmentPayload;
  className?: string;
}

export function makeRiskTierColor(tier: 'Low' | 'Moderate' | 'Elevated' | 'Critical'): {
  text: string;
  bg: string;
  border: string;
  hex: string;
  badgeVariant: 'low' | 'moderate' | 'elevated' | 'critical';
} {
  switch (tier) {
    case 'Low':
      return {
        text: 'text-emerald-700',
        bg: 'bg-emerald-50',
        border: 'border-emerald-300',
        hex: '#10b981',
        badgeVariant: 'low'
      };
    case 'Moderate':
      return {
        text: 'text-amber-700',
        bg: 'bg-amber-50',
        border: 'border-amber-300',
        hex: '#f59e0b',
        badgeVariant: 'moderate'
      };
    case 'Elevated':
      return {
        text: 'text-orange-700',
        bg: 'bg-orange-50',
        border: 'border-orange-300',
        hex: '#ea580c',
        badgeVariant: 'elevated'
      };
    case 'Critical':
      return {
        text: 'text-rose-700',
        bg: 'bg-rose-50',
        border: 'border-rose-300',
        hex: '#e11d48',
        badgeVariant: 'critical'
      };
  }
}

/**
 * Calculates (x, y) coordinates on a semi-circular gauge arc.
 * 0% corresponds to 180 degrees (far left), 100% corresponds to 0 degrees (far right).
 */
function makeGaugeCoordinates(
  cx: number,
  cy: number,
  radius: number,
  fraction: number
): { x: number; y: number; angleDegrees: number } {
  const madeClamped = Math.max(0, Math.min(1, fraction));
  const madeAngle = 180 - madeClamped * 180;
  const madeRadians = (madeAngle * Math.PI) / 180;
  return {
    x: cx + radius * Math.cos(madeRadians),
    y: cy - radius * Math.sin(madeRadians),
    angleDegrees: madeAngle
  };
}

/**
 * Generates an SVG arc path between two fractions [0, 1] on the semi-circle.
 */
function makeArcPath(
  cx: number,
  cy: number,
  radius: number,
  startFraction: number,
  endFraction: number
): string {
  const madeStart = makeGaugeCoordinates(cx, cy, radius, startFraction);
  const madeEnd = makeGaugeCoordinates(cx, cy, radius, endFraction);

  // In our 180-degree sweep, largeArcFlag is 0 because sweep is <= 180
  return `M ${madeStart.x} ${madeStart.y} A ${radius} ${radius} 0 0 1 ${madeEnd.x} ${madeEnd.y}`;
}

export const RiskAssessmentGauge: React.FC<RiskAssessmentGaugeProps> = ({
  assessment,
  className
}) => {
  const {
    riskProbability,
    riskBand,
    confidenceInterval,
    decisionThreshold,
    isCadPositive,
    inferenceLatencyMs
  } = assessment;

  const madeColor = useMemo(() => makeRiskTierColor(riskBand), [riskBand]);

  // SVG Geometry Constants
  const cx = 160;
  const cy = 145;
  const radius = 105;
  const strokeWidth = 16;

  // Arc paths for the 4 clinical risk tiers
  const tierArcs = useMemo(() => {
    return [
      { id: 'low', path: makeArcPath(cx, cy, radius, 0.0, 0.2), color: '#10b981', label: '<20% Low' },
      { id: 'mod', path: makeArcPath(cx, cy, radius, 0.2, 0.5), color: '#f59e0b', label: '20-50% Mod' },
      { id: 'ele', path: makeArcPath(cx, cy, radius, 0.5, 0.75), color: '#f97316', label: '50-75% Elev' },
      { id: 'crt', path: makeArcPath(cx, cy, radius, 0.75, 1.0), color: '#e11d48', label: '>75% Crit' }
    ];
  }, [cx, cy, radius]);

  // Confidence interval arc geometry
  const madeCiArc = useMemo(() => {
    const madeLower = Math.max(0, confidenceInterval.lowerBound);
    const madeUpper = Math.min(1, confidenceInterval.upperBound);
    return makeArcPath(cx, cy, radius + 14, madeLower, madeUpper);
  }, [confidenceInterval, cx, cy, radius]);

  // Needle pointer coordinates
  const madeNeedleTip = useMemo(() => {
    return makeGaugeCoordinates(cx, cy, radius - 18, riskProbability);
  }, [cx, cy, radius, riskProbability]);

  // Threshold marker coordinates
  const madeThresholdMark = useMemo(() => {
    const madeInner = makeGaugeCoordinates(cx, cy, radius - 12, decisionThreshold);
    const madeOuter = makeGaugeCoordinates(cx, cy, radius + 12, decisionThreshold);
    return { inner: madeInner, outer: madeOuter };
  }, [cx, cy, radius, decisionThreshold]);

  const probabilityPercent = (riskProbability * 100).toFixed(1);
  const lowerCiPercent = (confidenceInterval.lowerBound * 100).toFixed(1);
  const upperCiPercent = (confidenceInterval.upperBound * 100).toFixed(1);
  const standardErrorPercent = (confidenceInterval.standardError * 100).toFixed(1);

  return (
    <div
      role="region"
      aria-label={`Cardiovascular Risk Gauge: ${probabilityPercent}% ${riskBand} Risk`}
      data-testid="risk-assessment-gauge"
      className={makeClassName(
        'flex flex-col items-center bg-white rounded-2xl border border-slate-200 p-6 shadow-xs',
        className
      )}
    >
      {/* Header Badge & Title */}
      <div className="w-full flex items-center justify-between mb-2">
        <span className="text-xs font-bold tracking-wider uppercase text-slate-500">
          Calibrated Risk Gauge
        </span>
        <Badge variant={madeColor.badgeVariant} className="font-semibold text-xs px-2.5 py-0.5">
          {riskBand} Risk
        </Badge>
      </div>

      {/* SVG Radial Gauge */}
      <div className="relative w-full max-w-[320px] aspect-[320/190] flex items-center justify-center">
        <svg
          viewBox="0 0 320 185"
          className="w-full h-full overflow-visible"
          role="img"
          aria-hidden="true"
        >
          {/* Background Track Arc */}
          <path
            d={makeArcPath(cx, cy, radius, 0.0, 1.0)}
            fill="none"
            stroke="#f1f5f9"
            strokeWidth={strokeWidth}
            strokeLinecap="round"
          />

          {/* 4 Colored Risk Tier Segments */}
          {tierArcs.map((arc) => (
            <path
              key={arc.id}
              d={arc.path}
              fill="none"
              stroke={arc.color}
              strokeWidth={strokeWidth}
              strokeOpacity={0.88}
              strokeLinecap="butt"
            />
          ))}

          {/* 95% Confidence Interval Arc Ribbon */}
          <path
            d={madeCiArc}
            fill="none"
            stroke={madeColor.hex}
            strokeWidth={4}
            strokeDasharray="4 3"
            strokeLinecap="round"
            className="transition-all duration-500"
          />

          {/* Decision Threshold Marker Line */}
          <line
            x1={madeThresholdMark.inner.x}
            y1={madeThresholdMark.inner.y}
            x2={madeThresholdMark.outer.x}
            y2={madeThresholdMark.outer.y}
            stroke="#334155"
            strokeWidth={2.5}
            strokeDasharray="2 2"
          />

          {/* Needle Pointer */}
          <line
            x1={cx}
            y1={cy}
            x2={madeNeedleTip.x}
            y2={madeNeedleTip.y}
            stroke="#1e293b"
            strokeWidth={3.5}
            strokeLinecap="round"
            className="transition-all duration-700 ease-out"
          />
          {/* Needle Center Pivot Cap */}
          <circle cx={cx} cy={cy} r={8} fill="#1e293b" />
          <circle cx={cx} cy={cy} r={4} fill="#ffffff" />

          {/* Tier Label Markers on Dial Base */}
          <text x={42} y={160} textAnchor="middle" className="text-[10px] font-semibold fill-slate-400">
            0%
          </text>
          <text x={cx} y={42} textAnchor="middle" className="text-[10px] font-semibold fill-slate-500">
            Threshold (50%)
          </text>
          <text x={278} y={160} textAnchor="middle" className="text-[10px] font-semibold fill-slate-400">
            100%
          </text>
        </svg>

        {/* Floating Quantitative Readout inside Gauge Base */}
        <div className="absolute bottom-0 flex flex-col items-center justify-center pointer-events-none">
          <span
            data-testid="risk-probability-value"
            className={makeClassName('text-4xl font-extrabold tracking-tight', madeColor.text)}
          >
            {probabilityPercent}%
          </span>
          <span className="text-xs font-semibold text-slate-600 mt-0.5">
            {isCadPositive ? 'CAD Stratified Positive' : 'CAD Stratified Negative'}
          </span>
        </div>
      </div>

      {/* Uncertainty & Model Statistics Box */}
      <div className="w-full mt-5 p-3.5 bg-slate-50 rounded-xl border border-slate-200/80 space-y-2 text-xs">
        <div className="flex items-center justify-between">
          <span className="text-slate-500 flex items-center gap-1">
            95% Bootstrap CI:
            <span
              title="Calculated over 1,000 bootstrap resamples on calibrated ensemble"
              className="cursor-help"
            >
              <HelpCircle className="w-3 h-3 text-slate-400" />
            </span>
          </span>
          <span className="font-semibold text-slate-800 font-mono" data-testid="risk-ci-bounds">
            [{lowerCiPercent}% – {upperCiPercent}%]
          </span>
        </div>

        <div className="flex items-center justify-between">
          <span className="text-slate-500">Standard Error:</span>
          <span className="font-semibold text-slate-800 font-mono">
            ±{standardErrorPercent}%
          </span>
        </div>

        <div className="flex items-center justify-between">
          <span className="text-slate-500">Decision Threshold:</span>
          <span className="font-semibold text-slate-800 font-mono">
            {(decisionThreshold * 100).toFixed(0)}%
          </span>
        </div>

        <div className="flex items-center justify-between">
          <span className="text-slate-500">Classification:</span>
          <span
            className={makeClassName(
              'font-semibold flex items-center gap-1',
              isCadPositive ? 'text-rose-700' : 'text-emerald-700'
            )}
          >
            {isCadPositive ? (
              <>
                <AlertOctagon className="w-3 h-3" /> CAD Positive
              </>
            ) : (
              <>
                <CheckCircle2 className="w-3 h-3" /> CAD Negative
              </>
            )}
          </span>
        </div>
      </div>

      {/* Model Latency & Isotonic Calibration Footer */}
      <div className="w-full mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
        <span className="flex items-center gap-1 text-emerald-700 font-medium">
          <CheckCircle2 className="w-3.5 h-3.5" /> Isotonic Calibrated
        </span>
        <span className="font-mono text-slate-400">Latency: {inferenceLatencyMs.toFixed(1)} ms</span>
      </div>
    </div>
  );
};
