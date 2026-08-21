import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import ServiceStatus from './ServiceStatus';

describe('ServiceStatus', () => {
  it('reports the status once the API has answered', () => {
    render(
      <ServiceStatus
        health={{
          status: 'healthy',
          environment: 'Development',
          checkedAt: '2026-08-21T00:00:00Z',
        }}
        isLoading={false}
        onRetry={vi.fn()}
      />,
    );

    expect(screen.getByText('healthy')).toBeInTheDocument();
    expect(screen.getByText('Development')).toBeInTheDocument();
  });

  it('offers a retry when the API could not be reached', async () => {
    const onRetry = vi.fn();
    render(
      <ServiceStatus errorMessage="Could not reach the API." isLoading={false} onRetry={onRetry} />,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('Could not reach the API.');
    screen.getByRole('button', { name: 'Retry' }).click();
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it('shows progress while the check is in flight', () => {
    render(<ServiceStatus isLoading onRetry={vi.fn()} />);

    expect(screen.getByRole('status')).toHaveTextContent('Checking the API');
  });
});
