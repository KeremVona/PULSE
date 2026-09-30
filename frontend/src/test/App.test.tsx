import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { Provider } from 'react-redux';
import { makeAppStore } from '@/store';
import { axiosClient } from '@/api/axiosClient';
import App from '@/App';

describe('App Portal Integration', () => {
  beforeEach(() => {
    vi.spyOn(axiosClient, 'get').mockResolvedValue({ data: [] });
    vi.spyOn(axiosClient, 'post').mockResolvedValue({ data: {} });
  });

  it('renders clinical portal with default intake indicators and tabs', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <App />
        </Provider>
      );
    });

    expect(screen.getByText(/Two-Column Clinical Intake/i)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /execute ml risk evaluation/i })[0]).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /prefill high-risk profile/i })).toBeInTheDocument();
  });

  it('prefills high-risk sample when button clicked', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <App />
        </Provider>
      );
    });

    const madePrefillBtn = screen.getByRole('button', { name: /prefill high-risk profile/i });
    await act(async () => {
      fireEvent.click(madePrefillBtn);
    });

    const madeState = madeStore.getState().encounter;
    expect(madeState.indicators.restingBp).toBe(165);
    expect(madeState.indicators.oldpeakStDepression).toBe(3.1);
    expect(madeState.isDirty).toBe(true);
  });

  it('switches between tabs when navigation clicked', async () => {
    const madeStore = makeAppStore();

    await act(async () => {
      render(
        <Provider store={madeStore}>
          <App />
        </Provider>
      );
    });

    // Switch to Assessment tab
    const madeAssessmentNav = screen.getByRole('button', { name: /risk stratification/i });
    await act(async () => {
      fireEvent.click(madeAssessmentNav);
    });
    expect(screen.getByText('No Active Risk Assessment Recorded')).toBeInTheDocument();

    // Switch to SHAP Explainability tab
    const madeShapNav = screen.getByRole('button', { name: /shap explainability/i });
    await act(async () => {
      fireEvent.click(madeShapNav);
    });
    expect(screen.getByText('No Explainability Payload Available')).toBeInTheDocument();

    // Switch to Audit tab
    const madeAuditNav = screen.getByRole('button', { name: /audit & governance/i });
    await act(async () => {
      fireEvent.click(madeAuditNav);
    });
    expect(screen.getByText('Audit & Compliance Records')).toBeInTheDocument();

    // Switch to History tab
    const madeHistoryNav = screen.getByRole('button', { name: /patient history/i });
    await act(async () => {
      fireEvent.click(madeHistoryNav);
    });
    expect(screen.getByText(/Historical Encounters for/i)).toBeInTheDocument();
  });
});
