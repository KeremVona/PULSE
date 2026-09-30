import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ShapAttributionTable } from '@/components/assessment/ShapAttributionTable';
import { makeSampleFeatureAttributions } from '@/utils/riskFixtures';

describe('ShapAttributionTable Component', () => {
  it('should render all feature attributions with clinical values and rank', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    render(<ShapAttributionTable attributions={madeAttributions} />);

    expect(screen.getByText('ST Depression (Oldpeak)')).toBeInTheDocument();
    expect(screen.getByText('2.8 mm')).toBeInTheDocument();
    expect(screen.getByText('+0.412')).toBeInTheDocument();
    expect(screen.getByText('#1')).toBeInTheDocument();

    expect(screen.getByText('Serum Cholesterol')).toBeInTheDocument();
    expect(screen.getByText('310 mg/dL')).toBeInTheDocument();
    expect(screen.getByText('+0.143')).toBeInTheDocument();
  });

  it('should filter features by search query', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    render(<ShapAttributionTable attributions={madeAttributions} />);

    const searchInput = screen.getByPlaceholderText('Search features or values...');
    fireEvent.change(searchInput, { target: { value: 'Cholesterol' } });

    expect(screen.getByText('Serum Cholesterol')).toBeInTheDocument();
    expect(screen.queryByText('ST Depression (Oldpeak)')).not.toBeInTheDocument();
  });

  it('should filter features by direction (risk drivers vs protective)', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    render(<ShapAttributionTable attributions={madeAttributions} />);

    const filterSelect = screen.getByRole('combobox');
    fireEvent.change(filterSelect, { target: { value: 'RISK_DECREASING' } });

    expect(screen.getByText('Max Achieved Heart Rate')).toBeInTheDocument();
    expect(screen.getByText('-0.165')).toBeInTheDocument();
    expect(screen.queryByText('ST Depression (Oldpeak)')).not.toBeInTheDocument();
  });

  it('should invoke onSelectFeature when a row is clicked', () => {
    const madeAttributions = makeSampleFeatureAttributions();
    const mockSelect = vi.fn();
    render(
      <ShapAttributionTable
        attributions={madeAttributions}
        onSelectFeature={mockSelect}
      />
    );

    const targetRow = screen.getByText('ST Depression (Oldpeak)');
    fireEvent.click(targetRow);

    expect(mockSelect).toHaveBeenCalledTimes(1);
    expect(mockSelect).toHaveBeenCalledWith(
      expect.objectContaining({
        featureName: 'oldpeak',
        displayName: 'ST Depression (Oldpeak)'
      })
    );
  });
});
