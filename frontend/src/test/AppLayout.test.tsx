import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { makeAppStore } from '@/store';
import { AppLayout } from '@/components/layout/AppLayout';

describe('AppLayout Component', () => {
  it('renders CDSS banner, Header brand, and Sidebar navigation tabs', () => {
    const madeStore = makeAppStore();
    const handleTabChange = vi.fn();

    render(
      <Provider store={madeStore}>
        <AppLayout activeTab="intake" onTabChange={handleTabChange}>
          <div data-testid="test-content">Main Clinical Workspace</div>
        </AppLayout>
      </Provider>
    );

    expect(screen.getByRole('heading', { name: /pulse cdss/i })).toBeInTheDocument();
    expect(
      screen.getByRole('region', { name: /clinical decision support advisory/i })
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clinical intake/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /risk stratification/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /shap explainability/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /patient history/i })).toBeInTheDocument();
    expect(screen.getByTestId('test-content')).toBeInTheDocument();
  });

  it('calls onTabChange when a sidebar navigation item is clicked', () => {
    const madeStore = makeAppStore();
    const handleTabChange = vi.fn();

    render(
      <Provider store={madeStore}>
        <AppLayout activeTab="intake" onTabChange={handleTabChange}>
          <div>Workspace</div>
        </AppLayout>
      </Provider>
    );

    const madeAssessmentNav = screen.getByRole('button', { name: /risk stratification/i });
    fireEvent.click(madeAssessmentNav);

    expect(handleTabChange).toHaveBeenCalledWith('assessment');
  });
});
