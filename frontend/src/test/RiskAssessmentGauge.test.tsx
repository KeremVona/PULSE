import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  RiskAssessmentGauge,
  makeRiskTierColor
} from '@/components/assessment/RiskAssessmentGauge';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';

describe('RiskAssessmentGauge Component', () => {
  it('should adjust colors and badges appropriately for all four clinical risk tiers', () => {
    const lowTier = makeRiskTierColor('Low');
    expect(lowTier.hex).toBe('#10b981');
    expect(lowTier.badgeVariant).toBe('low');

    const modTier = makeRiskTierColor('Moderate');
    expect(modTier.hex).toBe('#f59e0b');
    expect(modTier.badgeVariant).toBe('moderate');

    const eleTier = makeRiskTierColor('Elevated');
    expect(eleTier.hex).toBe('#ea580c');
    expect(eleTier.badgeVariant).toBe('elevated');

    const critTier = makeRiskTierColor('Critical');
    expect(critTier.hex).toBe('#e11d48');
    expect(critTier.badgeVariant).toBe('critical');
  });

  it('should render calibrated probability, tier badge, and classification', () => {
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    render(<RiskAssessmentGauge assessment={madeAssessment} />);

    // Calibrated probability percentage
    const probabilityElement = screen.getByTestId('risk-probability-value');
    expect(probabilityElement).toHaveTextContent('74.2%');

    // Risk badge
    expect(screen.getByText('Elevated Risk')).toBeInTheDocument();

    // CAD Positive classification
    expect(screen.getByText('CAD Stratified Positive')).toBeInTheDocument();
  });

  it('should render 95% bootstrap confidence interval bounds and threshold', () => {
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    render(<RiskAssessmentGauge assessment={madeAssessment} />);

    // CI interval
    const ciElement = screen.getByTestId('risk-ci-bounds');
    expect(ciElement).toHaveTextContent('[64.8% – 83.6%]');

    // Standard error
    expect(screen.getByText('±4.8%')).toBeInTheDocument();

    // Decision threshold
    expect(screen.getByText('50%')).toBeInTheDocument();
  });

  it('should render CAD Negative status correctly for Low Risk tier', () => {
    const madeAssessment = makeSampleRiskAssessment('Low');
    render(<RiskAssessmentGauge assessment={madeAssessment} />);

    expect(screen.getByTestId('risk-probability-value')).toHaveTextContent('12.5%');
    expect(screen.getByText('Low Risk')).toBeInTheDocument();
    expect(screen.getByText('CAD Stratified Negative')).toBeInTheDocument();
    expect(screen.getByTestId('risk-ci-bounds')).toHaveTextContent('[8.2% – 16.8%]');
  });

  it('should display isotonic calibration badge and inference latency', () => {
    const madeAssessment = makeSampleRiskAssessment('Critical');
    render(<RiskAssessmentGauge assessment={madeAssessment} />);

    expect(screen.getByText('Isotonic Calibrated')).toBeInTheDocument();
    expect(screen.getByText('Latency: 13.8 ms')).toBeInTheDocument();
  });
});
