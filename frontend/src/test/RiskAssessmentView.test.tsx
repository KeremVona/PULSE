import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { makeAppStore } from '@/store';
import { RiskAssessmentView } from '@/components/assessment/RiskAssessmentView';
import { setActiveAssessment } from '@/store/slices/riskAssessmentSlice';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';

describe('RiskAssessmentView Component', () => {
  it('should render empty state when no active assessment exists', () => {
    const madeStore = makeAppStore();
    const mockNavigateToIntake = vi.fn();
    const mockNavigateToExplainability = vi.fn();

    render(
      <Provider store={madeStore}>
        <RiskAssessmentView
          onNavigateToIntake={mockNavigateToIntake}
          onNavigateToExplainability={mockNavigateToExplainability}
        />
      </Provider>
    );

    expect(screen.getByText('No Active Risk Assessment Recorded')).toBeInTheDocument();
    const intakeButton = screen.getByRole('button', { name: /Go to Clinical Intake/i });
    expect(intakeButton).toBeInTheDocument();

    fireEvent.click(intakeButton);
    expect(mockNavigateToIntake).toHaveBeenCalledTimes(1);
  });

  it('should render complete clinical dashboard when assessment is present in Redux', () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    madeStore.dispatch(setActiveAssessment(madeAssessment));

    const mockNavigateToIntake = vi.fn();
    const mockNavigateToExplainability = vi.fn();

    render(
      <Provider store={madeStore}>
        <RiskAssessmentView
          onNavigateToIntake={mockNavigateToIntake}
          onNavigateToExplainability={mockNavigateToExplainability}
        />
      </Provider>
    );

    // Header & Gauge
    expect(screen.getByText('Calibrated Cardiovascular Risk Stratification')).toBeInTheDocument();
    expect(screen.getByTestId('risk-probability-value')).toHaveTextContent('74.2%');

    // Clinical Guidance & Action
    expect(
      screen.getByText('Diagnostic Recommendations & Clinical Action Plan')
    ).toBeInTheDocument();
    expect(screen.getByText('Recommend Stress Echocardiogram / SPECT')).toBeInTheDocument();

    // Clinical Safety Alerts
    expect(screen.getByText('Active Clinical Safety Alerts')).toBeInTheDocument();
    expect(
      screen.getByText(/Severe Exertional Ischemia: ST segment depression of 2.8 mm/i)
    ).toBeInTheDocument();

    // Key Drivers summary cards
    expect(screen.getByText('Key Risk Drivers')).toBeInTheDocument();
    expect(screen.getByText('Key Protective Factors')).toBeInTheDocument();

    // Navigation callback to SHAP tab
    const shapButton = screen.getByRole('button', { name: /View SHAP Attributions/i });
    fireEvent.click(shapButton);
    expect(mockNavigateToExplainability).toHaveBeenCalledTimes(1);

    // Open Export Report modal
    const exportButton = screen.getAllByRole('button', { name: /Export Clinical Note/i })[0];
    fireEvent.click(exportButton);
    expect(screen.getByTestId('encounter-report-view')).toBeInTheDocument();
    expect(screen.getByText(/Printable Medical Record Summary Note/i)).toBeInTheDocument();
  });
});
