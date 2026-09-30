import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CdssAdvisoryBanner } from '@/components/common/CdssAdvisoryBanner';

describe('CdssAdvisoryBanner Component', () => {
  it('should render non-dismissible regulatory region with advisory notice', () => {
    render(<CdssAdvisoryBanner />);

    expect(screen.getByRole('region', { name: /Mandatory Clinical Decision Support Advisory/i })).toBeInTheDocument();
    expect(screen.getByText('Mandatory CDSS Advisory Notice')).toBeInTheDocument();
    expect(screen.getByText('Non-Dismissible Regulatory Region')).toBeInTheDocument();
    expect(screen.getByText(/EU MDR \/ FDA SaMD Class IIa/i)).toBeInTheDocument();
  });

  it('should contain explicit legal affirmation that CDSS is non-autonomous', () => {
    render(<CdssAdvisoryBanner />);

    expect(
      screen.getByText(/PULSE CDSS functions exclusively as an assistive clinical decision support tool/i)
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Final diagnosis, diagnostic ordering, and therapeutic management remain the sole responsibility/i)
    ).toBeInTheDocument();
  });
});
