import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ShapAttributionChart } from '@/components/assessment/ShapAttributionChart';
import { makeSampleFeatureAttributions } from '@/utils/riskFixtures';

describe('ShapAttributionChart Component', () => {
  it('should render chart container, accessibility region, and title', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    render(<ShapAttributionChart attributions={madeAttributions} />);

    expect(screen.getByRole('region', { name: /TreeSHAP Feature Attribution Waterfall Chart/i })).toBeInTheDocument();
    expect(screen.getByText('TreeSHAP Feature Attributions')).toBeInTheDocument();
    expect(
      screen.getByText(/Two-sided attribution waterfall: Crimson bars increase risk; Emerald bars are protective/i)
    ).toBeInTheDocument();
  });

  it('should render legend indicating crimson for risk and emerald for protective factors', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    render(<ShapAttributionChart attributions={madeAttributions} />);

    expect(screen.getByText('Risk Increasing (Crimson)')).toBeInTheDocument();
    expect(screen.getByText('Protective / Risk Decreasing (Emerald)')).toBeInTheDocument();
  });

  it('should allow filtering between All Factors, Risk Drivers, and Protective', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    render(<ShapAttributionChart attributions={madeAttributions} />);

    const riskBtn = screen.getByRole('button', { name: /Risk Drivers/i });
    const protectiveBtn = screen.getByRole('button', { name: /Protective/i });
    const allBtn = screen.getByRole('button', { name: /All Factors/i });

    expect(riskBtn).toBeInTheDocument();
    expect(protectiveBtn).toBeInTheDocument();
    expect(allBtn).toBeInTheDocument();

    fireEvent.click(riskBtn);
    expect(riskBtn).toHaveClass('bg-rose-500');

    fireEvent.click(protectiveBtn);
    expect(protectiveBtn).toHaveClass('bg-emerald-600');

    fireEvent.click(allBtn);
    expect(allBtn).toHaveClass('bg-white');
  });

  it('should toggle sort mode and display count buttons', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    render(<ShapAttributionChart attributions={madeAttributions} />);

    const sortButton = screen.getByRole('button', { name: /Magnitude/i });
    expect(sortButton).toBeInTheDocument();
    fireEvent.click(sortButton);
    expect(screen.getByRole('button', { name: /Signed/i })).toBeInTheDocument();

    const countButton5 = screen.getByRole('button', { name: '5' });
    expect(countButton5).toBeInTheDocument();
    fireEvent.click(countButton5);
    expect(countButton5).toHaveClass('bg-slate-900');
  });
});
