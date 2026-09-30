import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { Badge } from '@/components/common/Badge';
import { Button } from '@/components/common/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/common/Card';
import { CdssBanner } from '@/components/common/CdssBanner';

describe('Common Clinical Design System Components', () => {
  describe('Badge', () => {
    it('renders with text and default styles', () => {
      render(<Badge>Routine Screening</Badge>);
      expect(screen.getByText('Routine Screening')).toBeInTheDocument();
    });

    it('renders critical variant with dot', () => {
      const { container } = render(
        <Badge variant="critical" showDot>
          Critical Risk
        </Badge>
      );
      expect(screen.getByText('Critical Risk')).toBeInTheDocument();
      expect(container.querySelector('.bg-rose-500')).toBeInTheDocument();
    });
  });

  describe('Button', () => {
    it('handles click events when enabled', () => {
      const handleClick = vi.fn();
      render(<Button onClick={handleClick}>Evaluate Risk</Button>);

      const madeButton = screen.getByRole('button', { name: /evaluate risk/i });
      fireEvent.click(madeButton);

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('disables button and shows spinner when isLoading is true', () => {
      const handleClick = vi.fn();
      render(
        <Button isLoading onClick={handleClick}>
          Submitting
        </Button>
      );

      const madeButton = screen.getByRole('button');
      expect(madeButton).toBeDisabled();
      expect(madeButton).toHaveAttribute('aria-busy', 'true');

      fireEvent.click(madeButton);
      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe('Card', () => {
    it('renders composable card header and content', () => {
      render(
        <Card>
          <CardHeader>
            <CardTitle>Vascular Status</CardTitle>
          </CardHeader>
          <CardContent>
            <p>Fluoroscopy vessels clear</p>
          </CardContent>
        </Card>
      );

      expect(screen.getByText('Vascular Status')).toBeInTheDocument();
      expect(screen.getByText('Fluoroscopy vessels clear')).toBeInTheDocument();
    });
  });

  describe('CdssBanner', () => {
    it('renders regulatory advisory region with legal disclaimer', () => {
      render(<CdssBanner />);

      const madeRegion = screen.getByRole('region', {
        name: /clinical decision support advisory/i
      });
      expect(madeRegion).toBeInTheDocument();
      expect(screen.getByText(/CDSS Clinical Advisory:/i)).toBeInTheDocument();
      expect(screen.getByText(/Class IIa CDSS Aid/i)).toBeInTheDocument();
    });
  });
});
