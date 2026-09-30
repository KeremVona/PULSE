import React, { useState, useMemo } from 'react';
import { MlFeatureAttribution } from '@/types/clinical';
import { Badge } from '@/components/common/Badge';
import {
  TrendingUp,
  ShieldCheck,
  Search
} from 'lucide-react';
import { makeClassName } from '@/utils/cn';

interface ShapAttributionTableProps {
  attributions: MlFeatureAttribution[];
  selectedFeatureName?: string | null;
  onSelectFeature?: (attribution: MlFeatureAttribution) => void;
  className?: string;
}

export const ShapAttributionTable: React.FC<ShapAttributionTableProps> = ({
  attributions,
  selectedFeatureName,
  onSelectFeature,
  className
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [filterDirection, setFilterDirection] = useState<'all' | 'RISK_INCREASING' | 'RISK_DECREASING'>('all');

  // Compute maximum absolute attribution for relative bar width
  const madeMaxAbs = useMemo(() => {
    return Math.max(...attributions.map((a) => Math.abs(a.attributionValue)), 0.001);
  }, [attributions]);

  // Total absolute attribution sum for percentage impact calculation
  const madeTotalAbs = useMemo(() => {
    return attributions.reduce((acc, a) => acc + Math.abs(a.attributionValue), 0.001);
  }, [attributions]);

  // Filtered and searched list
  const madeFilteredRows = useMemo(() => {
    return attributions.filter((attr) => {
      const matchesSearch =
        attr.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        attr.featureName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        attr.clinicalValue.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesDirection =
        filterDirection === 'all' || attr.direction === filterDirection;

      return matchesSearch && matchesDirection;
    });
  }, [attributions, searchQuery, filterDirection]);

  return (
    <div
      role="region"
      aria-label="TreeSHAP Feature Attributions Table"
      data-testid="shap-attribution-table"
      className={makeClassName(
        'bg-white rounded-2xl border border-slate-200 overflow-hidden shadow-xs flex flex-col',
        className
      )}
    >
      {/* Table Header & Search Controls */}
      <div className="p-4 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-slate-50/50">
        <div>
          <h4 className="text-sm font-bold text-slate-900">
            Granular Feature Attribution Breakdown
          </h4>
          <p className="text-xs text-slate-500">
            Additive Shapley contributions across patient physiological indicators.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {/* Search Box */}
          <div className="relative">
            <Search className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              type="text"
              placeholder="Search features or values..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="text-xs pl-8 pr-3 py-1.5 rounded-lg border border-slate-200 bg-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 w-48 sm:w-56"
            />
          </div>

          {/* Direction Filter Dropdown */}
          <select
            value={filterDirection}
            onChange={(e) =>
              setFilterDirection(
                e.target.value as 'all' | 'RISK_INCREASING' | 'RISK_DECREASING'
              )
            }
            className="text-xs py-1.5 px-2.5 rounded-lg border border-slate-200 bg-white text-slate-700 focus:outline-none focus:ring-2 focus:ring-sky-500"
          >
            <option value="all">All Directions</option>
            <option value="RISK_INCREASING">Risk Drivers Only</option>
            <option value="RISK_DECREASING">Protective Factors Only</option>
          </select>
        </div>
      </div>

      {/* Table Element */}
      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs text-slate-600">
          <thead className="bg-slate-50 border-b border-slate-200 text-slate-700 font-semibold uppercase text-[10px] tracking-wider">
            <tr>
              <th scope="col" className="py-2.5 px-3 w-12 text-center">Rank</th>
              <th scope="col" className="py-2.5 px-3">Clinical Indicator</th>
              <th scope="col" className="py-2.5 px-3">Patient Value</th>
              <th scope="col" className="py-2.5 px-3">Direction</th>
              <th scope="col" className="py-2.5 px-3 font-mono">SHAP Impact</th>
              <th scope="col" className="py-2.5 px-3 w-40">Relative Share</th>
              <th scope="col" className="py-2.5 px-3">Clinical Context</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {madeFilteredRows.length > 0 ? (
              madeFilteredRows.map((attr) => {
                const isRisk = attr.direction === 'RISK_INCREASING';
                const isSelected = selectedFeatureName === attr.featureName;
                const absVal = Math.abs(attr.attributionValue);
                const percentShare = ((absVal / madeTotalAbs) * 100).toFixed(1);
                const barWidthPercent = ((absVal / madeMaxAbs) * 100).toFixed(0);

                return (
                  <tr
                    key={attr.featureName}
                    onClick={() => onSelectFeature && onSelectFeature(attr)}
                    className={makeClassName(
                      'hover:bg-slate-50/80 transition-colors cursor-pointer',
                      isSelected && 'bg-sky-50/60 font-medium'
                    )}
                  >
                    <td className="py-3 px-3 text-center font-mono text-slate-400 font-bold">
                      #{attr.rank}
                    </td>
                    <td className="py-3 px-3">
                      <div className="flex flex-col">
                        <span className="font-semibold text-slate-900">{attr.displayName}</span>
                        <span className="text-[10px] text-slate-400 font-mono">
                          {attr.featureName}
                        </span>
                      </div>
                    </td>
                    <td className="py-3 px-3">
                      <span className="font-mono font-semibold px-2 py-0.5 rounded-md bg-slate-100 text-slate-800 border border-slate-200">
                        {attr.clinicalValue}
                      </span>
                    </td>
                    <td className="py-3 px-3">
                      <Badge
                        variant={isRisk ? 'critical' : 'low'}
                        className="text-[10px] px-2 py-0.5 inline-flex items-center gap-1"
                      >
                        {isRisk ? (
                          <>
                            <TrendingUp className="w-3 h-3" /> Risk Driver
                          </>
                        ) : (
                          <>
                            <ShieldCheck className="w-3 h-3" /> Protective
                          </>
                        )}
                      </Badge>
                    </td>
                    <td className="py-3 px-3 font-mono font-semibold">
                      <span className={isRisk ? 'text-rose-600' : 'text-emerald-600'}>
                        {attr.attributionValue > 0
                          ? `+${attr.attributionValue.toFixed(3)}`
                          : attr.attributionValue.toFixed(3)}
                      </span>
                    </td>
                    <td className="py-3 px-3">
                      <div className="flex items-center gap-2">
                        <div className="flex-1 bg-slate-100 rounded-full h-2 overflow-hidden">
                          <div
                            className={makeClassName(
                              'h-full rounded-full',
                              isRisk ? 'bg-rose-500' : 'bg-emerald-500'
                            )}
                            style={{ width: `${barWidthPercent}%` }}
                          />
                        </div>
                        <span className="text-[10px] font-mono text-slate-500 w-9 text-right">
                          {percentShare}%
                        </span>
                      </div>
                    </td>
                    <td className="py-3 px-3 text-[11px] text-slate-500 max-w-xs">
                      {isRisk
                        ? `Elevates CAD odds due to ${attr.clinicalValue} finding.`
                        : `Protects against elevated risk classification.`}
                    </td>
                  </tr>
                );
              })
            ) : (
              <tr>
                <td colSpan={7} className="py-8 text-center text-slate-400 text-xs">
                  No matching features found for "{searchQuery}".
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Footer Info Strip */}
      <div className="p-3 bg-slate-50 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
        <span>
          Showing {madeFilteredRows.length} of {attributions.length} indicators
        </span>
        <span className="text-slate-400">
          Sum of local attributions explains calibrated log-odds deviation from baseline population mean.
        </span>
      </div>
    </div>
  );
};
