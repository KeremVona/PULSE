import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { makeAppStore } from '@/store';
import { EncounterReportView } from '@/components/assessment/EncounterReportView';
import { setActiveAssessment } from '@/store/slices/riskAssessmentSlice';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';

describe('EncounterReportView Component', () => {
  beforeEach(() => {
    vi.spyOn(window, 'print').mockImplementation(() => {});
    Object.assign(navigator, {
      clipboard: {
        writeText: vi.fn().mockResolvedValue(undefined)
      }
    });
  });

  it('should render complete printable report with institutional header, indicators, and SHAP drivers', () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    madeStore.dispatch(setActiveAssessment(madeAssessment));

    const mockClose = vi.fn();

    render(
      <Provider store={madeStore}>
        <EncounterReportView onClose={mockClose} />
      </Provider>
    );

    // Institutional Header
    expect(screen.getByText(/PULSE CARDIOVASCULAR HEALTH NETWORK/i)).toBeInTheDocument();
    expect(screen.getByText(/Encounter Evaluation Report/i)).toBeInTheDocument();

    // Section 1: Calibrated Risk Stratification
    expect(screen.getByText(/1. Calibrated Cardiovascular Risk Stratification/i)).toBeInTheDocument();
    expect(screen.getByText('74.2%')).toBeInTheDocument();
    expect(screen.getByText(/CAD Stratified Positive/i)).toBeInTheDocument();

    // Section 2: Recorded Physiological Indicators
    expect(screen.getByText(/2. Recorded Physiological Indicators/i)).toBeInTheDocument();
    expect(screen.getByText(/Resting Blood Pressure/i)).toBeInTheDocument();
    expect(screen.getByText(/Total Serum Cholesterol/i)).toBeInTheDocument();

    // Section 3: TreeSHAP Feature Attributions
    expect(screen.getByText(/3. TreeSHAP Local Feature Attributions/i)).toBeInTheDocument();
    expect(screen.getAllByText(/ST Depression \(Oldpeak\)/i).length).toBeGreaterThanOrEqual(1);

    // Mandatory Advisory Banner
    expect(screen.getByTestId('cdss-advisory-banner')).toBeInTheDocument();

    // Attestation Checkbox
    const attestCheckbox = screen.getByRole('checkbox');
    expect(attestCheckbox).not.toBeChecked();
    expect(screen.getByText('Attestation Pending')).toBeInTheDocument();

    fireEvent.click(attestCheckbox);
    expect(attestCheckbox).toBeChecked();
    expect(screen.getByText('Electronically Attested')).toBeInTheDocument();
  });

  it('should trigger window.print when Print / Save PDF button is clicked', () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    madeStore.dispatch(setActiveAssessment(madeAssessment));

    render(
      <Provider store={madeStore}>
        <EncounterReportView onClose={vi.fn()} />
      </Provider>
    );

    const printButton = screen.getByRole('button', { name: /Print \/ Save PDF/i });
    fireEvent.click(printButton);

    expect(window.print).toHaveBeenCalledTimes(1);
  });

  it('should copy EMR note to clipboard when Copy button is clicked', async () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    madeStore.dispatch(setActiveAssessment(madeAssessment));

    render(
      <Provider store={madeStore}>
        <EncounterReportView onClose={vi.fn()} />
      </Provider>
    );

    const copyButton = screen.getByRole('button', { name: /Copy EMR Note/i });
    fireEvent.click(copyButton);

    expect(navigator.clipboard.writeText).toHaveBeenCalledTimes(1);
  });

  it('should call onClose when Close button is clicked', () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    madeStore.dispatch(setActiveAssessment(madeAssessment));

    const mockClose = vi.fn();

    render(
      <Provider store={madeStore}>
        <EncounterReportView onClose={mockClose} />
      </Provider>
    );

    const closeButton = screen.getByRole('button', { name: /Close Report/i });
    fireEvent.click(closeButton);

    expect(mockClose).toHaveBeenCalledTimes(1);
  });
});
