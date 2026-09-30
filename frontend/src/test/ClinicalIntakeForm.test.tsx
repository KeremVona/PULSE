import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { Provider } from 'react-redux';
import { makeAppStore } from '@/store';
import { ClinicalIntakeForm } from '@/components/intake/ClinicalIntakeForm';
import { axiosClient } from '@/api/axiosClient';

describe('ClinicalIntakeForm Two-Column Component', () => {
  beforeEach(() => {
    vi.spyOn(axiosClient, 'post').mockResolvedValue({
      data: {
        encounterId: 'enc-test-123',
        patientId: 'patient-test-123',
        clinicianId: 'clinician-test-123',
        encounterNumber: 'ENC-TEST-99',
        encounterDate: '2026-09-30T12:00:00Z',
        status: 2,
        riskAssessment: {
          encounterId: 'enc-test-123',
          correlationId: 'corr-test-123',
          riskProbability: 0.68,
          riskBand: 'Elevated',
          isCadPositive: true,
          decisionThreshold: 0.29,
          confidenceInterval: {
            lowerBound: 0.61,
            upperBound: 0.75,
            confidenceLevel: 0.95,
            standardError: 7.0
          },
          topRiskDrivers: [],
          topProtectiveFactors: [],
          allAttributions: [],
          explanations: [],
          clinicalWarnings: [],
          recommendedAction: 'Cardiology consult',
          clinicalDisclaimer: 'Assistive aid',
          inferenceLatencyMs: 14.2
        }
      }
    });
  });

  it('renders dual-column headings and all primary vitals fields', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <ClinicalIntakeForm />
        </Provider>
      );
    });

    expect(screen.getByText(/Column 1: Hemodynamics & Exercise Vitals/i)).toBeInTheDocument();
    expect(screen.getByText(/Column 2: Demographics & Clinical Diagnostic Findings/i)).toBeInTheDocument();

    expect(screen.getByLabelText(/Resting Systolic Blood Pressure/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Resting Diastolic Blood Pressure/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Total Serum Cholesterol/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Max Heart Rate Achieved/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/ST Depression Induced by Exercise/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Patient Age/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Biological Sex/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Chest Pain Type/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Fluoroscopy Vessels Colored/i)).toBeInTheDocument();
  });

  it('populates high-risk profile and renders active physiological warning alerts', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <ClinicalIntakeForm />
        </Provider>
      );
    });

    const madeHighRiskBtn = screen.getByRole('button', { name: /prefill high-risk profile/i });
    await act(async () => {
      fireEvent.click(madeHighRiskBtn);
    });

    // Verify values updated in inputs
    const madeSystolicInput = screen.getByLabelText(/Resting Systolic Blood Pressure/i) as HTMLInputElement;
    expect(madeSystolicInput.value).toBe('165');

    const madeOldpeakInput = screen.getByLabelText(/ST Depression Induced by Exercise/i) as HTMLInputElement;
    expect(madeOldpeakInput.value).toBe('3.1');

    // Verify real-time warnings appear
    expect(screen.getByText(/Active Physiological Pre-Inference Alerts/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Stage 2 Hypertension/i).length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/Severe Exertional Ischemia/i).length).toBeGreaterThanOrEqual(1);
  });

  it('populates low-risk baseline and updates form inputs', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <ClinicalIntakeForm />
        </Provider>
      );
    });

    const madeLowRiskBtn = screen.getByRole('button', { name: /prefill low-risk profile/i });
    await act(async () => {
      fireEvent.click(madeLowRiskBtn);
    });

    const madeSystolicInput = screen.getByLabelText(/Resting Systolic Blood Pressure/i) as HTMLInputElement;
    expect(madeSystolicInput.value).toBe('115');

    const madeOldpeakInput = screen.getByLabelText(/ST Depression Induced by Exercise/i) as HTMLInputElement;
    expect(madeOldpeakInput.value).toBe('0.2');
  });

  it('resets form when reset baseline button is clicked', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <ClinicalIntakeForm />
        </Provider>
      );
    });

    const madeHighRiskBtn = screen.getByRole('button', { name: /prefill high-risk profile/i });
    await act(async () => {
      fireEvent.click(madeHighRiskBtn);
    });
    expect((screen.getByLabelText(/Resting Systolic Blood Pressure/i) as HTMLInputElement).value).toBe('165');

    const madeResetBtn = screen.getByRole('button', { name: /reset baseline/i });
    await act(async () => {
      fireEvent.click(madeResetBtn);
    });

    expect((screen.getByLabelText(/Resting Systolic Blood Pressure/i) as HTMLInputElement).value).toBe('130');
  });

  it('displays real-time validation error when physiological boundaries are violated', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <ClinicalIntakeForm />
        </Provider>
      );
    });

    const madeSystolicInput = screen.getByLabelText(/Resting Systolic Blood Pressure/i);
    await act(async () => {
      fireEvent.change(madeSystolicInput, { target: { value: '290' } });
    });

    const madeSubmitBtn = screen.getAllByRole('button', { name: /execute ml risk evaluation/i })[0];
    await act(async () => {
      fireEvent.click(madeSubmitBtn);
    });

    expect(screen.getByText(/cannot exceed 260 mmHg/i)).toBeInTheDocument();
  });

  it('executes ML risk evaluation on valid submit and triggers navigation', async () => {
    const madeStore = makeAppStore();
    const handleSuccessNavigate = vi.fn();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <ClinicalIntakeForm onSuccessNavigate={handleSuccessNavigate} />
        </Provider>
      );
    });

    const madeSubmitBtn = screen.getAllByRole('button', { name: /execute ml risk evaluation/i })[0];
    await act(async () => {
      fireEvent.click(madeSubmitBtn);
    });

    expect(axiosClient.post).toHaveBeenCalledTimes(1);
    expect(handleSuccessNavigate).toHaveBeenCalledTimes(1);
  });
});
