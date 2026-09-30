import React, { useState, useMemo } from 'react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
  Cell
} from 'recharts';
import { MlFeatureAttribution } from '@/types/clinical';
import { Badge } from '@/components/common/Badge';
import { Button } from '@/components/common/Button';
import {
  TrendingUp,
  ShieldCheck,
  ArrowUpDown,
  Info
} from 'lucide-react';
import { makeClassName } from '@/utils/cn';

interface ShapAttributionChartProps {
  attributions: MlFeatureAttribution[];
  onSelectFeature?: (attribution: MlFeatureAttribution) => void;
  className?: string;
}

type FilterMode = 'all' | 'risk_only' | 'protective_only';
type SortMode = 'magnitude' | 'signed';

interface ChartItem extends MlFeatureAttribution {
  absAttribution: number;
}

/**
 * Custom accessible tooltip rendering clinical display name, exact patient value,
 * and SHAP attribution contribution.
 */
const CustomShapTooltip = ({
  active,
  payload
}: {
  active?: boolean;
  payload?: Array<{ payload: ChartItem }>;
}) => {
  if (!active || !payload || !payload.length) {
    return null;
  }

  const madeItem = payload[0].payload;
  const isRisk = madeItem.direction === 'RISK_INCREASING';
  const madeSignedString = madeItem.attributionValue > 0
    ? `+${madeItem.attributionValue.toFixed(3)}`
    : madeItem.attributionValue.toFixed(3);
  const madePercentage = (Math.abs(madeItem.attributionValue) * 100).toFixed(1);

  return (
    <div
      role="tooltip"
      className="bg-slate-900 text-white rounded-xl p-3 shadow-xl border border-slate-700 max-w-xs text-xs space-y-2 z-50 pointer-events-none"
    >
      <div className="flex items-center justify-between gap-2 border-b border-slate-800 pb-1.5">
        <span className="font-bold text-slate-100 text-sm">{madeItem.displayName}</span>
        <Badge variant={isRisk ? 'critical' : 'low'} className="text-[10px] px-1.5 py-0">
          {isRisk ? 'Risk Driver' : 'Protective'}
        </Badge>
      </div>

      <div className="space-y-1">
        <div className="flex justify-between">
          <span className="text-slate-400">Patient Value:</span>
          <span className="font-semibold text-sky-400 font-mono">{madeItem.clinicalValue}</span>
        </div>

        <div className="flex justify-between">
          <span className="text-slate-400">SHAP Attribution:</span>
          <span
            className={makeClassName(
              'font-bold font-mono',
              isRisk ? 'text-rose-400' : 'text-emerald-400'
            )}
          >
            {madeSignedString} ({madePercentage}% contribution)
          </span>
        </div>

        <div className="flex justify-between text-[11px]">
          <span className="text-slate-400">Importance Rank:</span>
          <span className="text-slate-300 font-mono">#{madeItem.rank}</span>
        </div>
      </div>

      <p className="text-[11px] text-slate-300 pt-1 border-t border-slate-800 leading-normal">
        {isRisk
          ? `Patient's ${madeItem.displayName.toLowerCase()} (${madeItem.clinicalValue}) elevates the calibrated CAD risk score by ${madePercentage}%.`
          : `Patient's ${madeItem.displayName.toLowerCase()} (${madeItem.clinicalValue}) provides a protective effect, lowering CAD risk score by ${madePercentage}%.`}
      </p>
    </div>
  );
};

export const ShapAttributionChart: React.FC<ShapAttributionChartProps> = ({
  attributions,
  onSelectFeature,
  className
}) => {
  const [filterMode, setFilterMode] = useState<FilterMode>('all');
  const [sortMode, setSortMode] = useState<SortMode>('magnitude');
  const [displayCount, setDisplayCount] = useState<number>(10);

  // Prepare and sort chart data
  const madeChartData: ChartItem[] = useMemo(() => {
    let madeFiltered = attributions.map((a) => ({
      ...a,
      absAttribution: Math.abs(a.attributionValue)
    }));

    if (filterMode === 'risk_only') {
      madeFiltered = madeFiltered.filter((a) => a.direction === 'RISK_INCREASING');
    } else if (filterMode === 'protective_only') {
      madeFiltered = madeFiltered.filter((a) => a.direction === 'RISK_DECREASING');
    }

    if (sortMode === 'magnitude') {
      madeFiltered.sort((a, b) => b.absAttribution - a.absAttribution);
    } else {
      madeFiltered.sort((a, b) => b.attributionValue - a.attributionValue);
    }

    return madeFiltered.slice(0, displayCount);
  }, [attributions, filterMode, sortMode, displayCount]);

  // Compute dynamic height based on number of bars (42px per bar + 100px padding)
  const chartHeight = Math.max(340, madeChartData.length * 42 + 90);

  return (
    <div
      role="region"
      aria-label="TreeSHAP Feature Attribution Waterfall Chart"
      data-testid="shap-attribution-chart"
      className={makeClassName(
        'bg-white rounded-2xl border border-slate-200 p-6 shadow-xs space-y-4',
        className
      )}
    >
      {/* Title & Controls Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-slate-100">
        <div>
          <div className="flex items-center gap-2">
            <h3 className="text-base font-bold text-slate-900 tracking-tight">
              TreeSHAP Feature Attributions
            </h3>
            <span
              title="TreeSHAP decomposes the ensemble log-odds into exact additive feature contributions."
              className="cursor-help"
            >
              <Info className="w-4 h-4 text-slate-400" />
            </span>
          </div>
          <p className="text-xs text-slate-500 mt-0.5">
            Two-sided attribution waterfall: Crimson bars increase risk; Emerald bars are protective.
          </p>
        </div>

        {/* Filter & Count Buttons */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Filter Pills */}
          <div className="flex items-center p-0.5 bg-slate-100 rounded-lg text-xs">
            <button
              type="button"
              onClick={() => setFilterMode('all')}
              className={makeClassName(
                'px-2.5 py-1 rounded-md font-medium transition-colors',
                filterMode === 'all'
                  ? 'bg-white text-slate-900 shadow-xs'
                  : 'text-slate-600 hover:text-slate-900'
              )}
            >
              All Factors
            </button>
            <button
              type="button"
              onClick={() => setFilterMode('risk_only')}
              className={makeClassName(
                'px-2.5 py-1 rounded-md font-medium transition-colors flex items-center gap-1',
                filterMode === 'risk_only'
                  ? 'bg-rose-500 text-white shadow-xs'
                  : 'text-rose-700 hover:text-rose-900'
              )}
            >
              <TrendingUp className="w-3 h-3" /> Risk Drivers
            </button>
            <button
              type="button"
              onClick={() => setFilterMode('protective_only')}
              className={makeClassName(
                'px-2.5 py-1 rounded-md font-medium transition-colors flex items-center gap-1',
                filterMode === 'protective_only'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-emerald-700 hover:text-emerald-900'
              )}
            >
              <ShieldCheck className="w-3 h-3" /> Protective
            </button>
          </div>

          {/* Sort Order Toggle */}
          <Button
            size="sm"
            variant="ghost"
            leftIcon={<ArrowUpDown className="w-3.5 h-3.5" />}
            onClick={() =>
              setSortMode((prev) => (prev === 'magnitude' ? 'signed' : 'magnitude'))
            }
            className="text-xs text-slate-600"
          >
            {sortMode === 'magnitude' ? 'Magnitude' : 'Signed'}
          </Button>

          {/* Display Count Toggle */}
          <div className="flex items-center gap-1 text-xs">
            <span className="text-slate-400">Show:</span>
            {[5, 10, 13].map((count) => (
              <button
                key={count}
                type="button"
                onClick={() => setDisplayCount(count)}
                className={makeClassName(
                  'w-6 h-6 rounded-md font-mono font-medium transition-colors',
                  displayCount === count
                    ? 'bg-slate-900 text-white'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                )}
              >
                {count}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Recharts Two-Sided Horizontal Bar Waterfall Chart */}
      <div className="w-full" style={{ height: chartHeight }}>
        <ResponsiveContainer width="100%" height="100%" minWidth={300} minHeight={300}>
          <BarChart
            layout="vertical"
            data={madeChartData}
            margin={{ top: 15, right: 30, left: 165, bottom: 25 }}
          >
            <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="#f1f5f9" />
            <XAxis
              type="number"
              domain={['auto', 'auto']}
              stroke="#64748b"
              tick={{ fontSize: 11, fill: '#64748b' }}
              tickFormatter={(val: number) =>
                val > 0 ? `+${val.toFixed(2)}` : val.toFixed(2)
              }
            />
            <YAxis
              type="category"
              dataKey="displayName"
              width={160}
              tick={{ fontSize: 12, fill: '#1e293b', fontWeight: 500 }}
              stroke="#cbd5e1"
            />
            <ReferenceLine
              x={0}
              stroke="#334155"
              strokeWidth={1.75}
              label={{
                value: 'Baseline (Zero Impact)',
                position: 'top',
                fill: '#64748b',
                fontSize: 10
              }}
            />
            <Tooltip content={<CustomShapTooltip />} cursor={{ fill: '#f8fafc' }} />
            <Bar
              dataKey="attributionValue"
              isAnimationActive={false}
              radius={[4, 4, 4, 4]}
              onClick={(entry) => {
                if (onSelectFeature && entry) {
                  onSelectFeature(entry as unknown as MlFeatureAttribution);
                }
              }}
              className="cursor-pointer"
            >
              {madeChartData.map((entry, index) => (
                <Cell
                  key={`shap-cell-${entry.featureName}-${index}`}
                  fill={entry.direction === 'RISK_INCREASING' ? '#e11d48' : '#059669'}
                  className="transition-opacity hover:opacity-85"
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Explanatory Legend Footer */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-3 border-t border-slate-100 text-xs text-slate-500">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-1.5">
            <span className="w-3.5 h-3.5 rounded-sm bg-rose-600 inline-block" />
            <span className="font-medium text-slate-700">Risk Increasing (Crimson)</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="w-3.5 h-3.5 rounded-sm bg-emerald-600 inline-block" />
            <span className="font-medium text-slate-700">Protective / Risk Decreasing (Emerald)</span>
          </div>
        </div>

        <span className="text-[11px] text-slate-400 italic">
          Click any bar to inspect feature reasoning
        </span>
      </div>
    </div>
  );
};
