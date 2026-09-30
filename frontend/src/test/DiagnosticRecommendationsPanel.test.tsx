import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { makeAppStore } from '@/store';
import {
  DiagnosticRecommendationsPanel,
  makeClinicalActionPlan
} from '@/components/assessment/DiagnosticRecommendationsPanel';
import { makeSampleRiskAssessment } from '@/utils/riskFixtures';

describe('DiagnosticRecommendationsPanel Component', () => {
  it('should formulate appropriate clinical action plans across all 4 risk tiers', () => {
    const lowPlan = makeClinicalActionPlan('Low');
    expect(lowPlan.orderBadge).toBe('Routine Primary Care Follow-up');
    expect(lowPlan.urgencyLabel).toBe('Elective / Routine');

    const modPlan = makeClinicalActionPlan('Moderate');
    expect(modPlan.orderBadge).toBe('Consider Exercise Treadmill Test (ETT)');
    expect(modPlan.urgencyLabel).toBe('Moderate Priority');

    const elePlan = makeClinicalActionPlan('Elevated');
    expect(elePlan.orderBadge).toBe('Recommend Stress Echocardiogram / SPECT');
    expect(elePlan.urgencyLabel).toBe('Elevated Priority');

    const critPlan = makeClinicalActionPlan('Critical');
    expect(critPlan.orderBadge).toBe('Urgent Invasive Angiography Consult');
    expect(critPlan.urgencyLabel).toBe('Critical / Urgent');
  });

  it('should render panel with order badge, diagnostic items, and anchored advisory banner', () => {
    const madeStore = makeAppStore();
    const madeAssessment = makeSampleRiskAssessment('Elevated');
    const mockExport = vi.fn();

    render(
      <Provider store={madeStore}>
        <DiagnosticRecommendationsPanel
          assessment={madeAssessment}
          onOpenReportExport={mockExport}
        />
      </Provider>
    );

    // Anchored non-dismissible advisory banner
    expect(screen.getByTestId('cdss-advisory-banner')).toBeInTheDocument();
    expect(screen.getByText('Mandatory CDSS Advisory Notice')).toBeInTheDocument();

    // Order badge and primary guidance
    expect(screen.getByText('Recommend Stress Echocardiogram / SPECT')).toBeInTheDocument();
    expect(screen.getByText(/Stress Echocardiography or SPECT Myocardial Perfusion Imaging/i)).toBeInTheDocument();
    expect(screen.getByText(/Initiate high-intensity statin therapy/i)).toBeInTheDocument();

    // Export button triggers callback
    const exportButtons = screen.getAllByRole('button', { name: /Export Clinical Note/i });
    expect(exportButtons.length).toBeGreaterThanOrEqual(1);

    fireEvent.click(exportButtons[0]);
    expect(mockExport).toHaveBeenCalledTimes(1);
  });
});
