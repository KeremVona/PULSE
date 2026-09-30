import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { makeAppStore } from '@/store';
import { ShapExplanationView } from '@/components/assessment/ShapExplanationView';
import { setActiveAssessment } from '@/store/slices/riskAssessmentSlice';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';

describe('ShapExplanationView Component', () => {
  it('should render empty state when no explainability payload is available', () => {
    const madeStore = makeAppStore();
    const mockNavigateToIntake = vi.fn();
    const mockNavigateToAssessment = vi.fn();

    render(
      <Provider store={madeStore}>
        <ShapExplanationView
          onNavigateToIntake={mockNavigateToIntake}
          onNavigateToAssessment={mockNavigateToAssessment}
        />
      </Provider>
    );

    expect(screen.getByText('No Explainability Payload Available')).toBeInTheDocument();
    const intakeBtn = screen.getByRole('button', { name: /Go to Clinical Intake/i });
    expect(intakeBtn).toBeInTheDocument();

    fireEvent.click(intakeBtn);
    expect(mockNavigateToIntake).toHaveBeenCalledTimes(1);
  });

  it('should render chart and table when assessment is populated', () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    madeStore.dispatch(setActiveAssessment(madeAssessment));

    const mockNavigateToIntake = vi.fn();
    const mockNavigateToAssessment = vi.fn();

    render(
      <Provider store={madeStore}>
        <ShapExplanationView
          onNavigateToIntake={mockNavigateToIntake}
          onNavigateToAssessment={mockNavigateToAssessment}
        />
      </Provider>
    );

    expect(screen.getByText('Explainable AI: TreeSHAP Feature Attributions')).toBeInTheDocument();
    expect(screen.getByTestId('shap-attribution-chart')).toBeInTheDocument();
    expect(screen.getByTestId('shap-attribution-table')).toBeInTheDocument();

    // Navigation back to assessment gauge
    const backBtn = screen.getByRole('button', { name: /Back to Risk Gauge/i });
    fireEvent.click(backBtn);
    expect(mockNavigateToAssessment).toHaveBeenCalledTimes(1);
  });

  it('should display feature reasoning card when a table row is selected', () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    madeStore.dispatch(setActiveAssessment(madeAssessment));

    render(
      <Provider store={madeStore}>
        <ShapExplanationView
          onNavigateToIntake={vi.fn()}
          onNavigateToAssessment={vi.fn()}
        />
      </Provider>
    );

    // Click on ST Depression in table
    const targetRow = screen.getByText('ST Depression (Oldpeak)');
    fireEvent.click(targetRow);

    // Inspector card should appear
    expect(
      screen.getByText('Feature Reasoning: ST Depression (Oldpeak)')
    ).toBeInTheDocument();
    expect(screen.getAllByText('2.8 mm').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('+0.4120')).toBeInTheDocument();
  });
});
