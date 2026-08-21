import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import SortTabs from './SortTabs';

describe('SortTabs', () => {
  it('marks the ordering currently in use', () => {
    render(<SortTabs onChange={vi.fn()} order="Descending" sort="LikeCount" />);

    expect(screen.getByRole('tab', { name: 'Top', selected: true })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Latest' })).toHaveAttribute('aria-selected', 'false');
  });

  it('reports both the field and the direction when chosen', async () => {
    const onChange = vi.fn();
    render(<SortTabs onChange={onChange} order="Descending" sort="CreatedAt" />);

    await userEvent.click(screen.getByRole('tab', { name: 'Oldest' }));

    expect(onChange).toHaveBeenCalledWith('CreatedAt', 'Ascending');
  });
});
