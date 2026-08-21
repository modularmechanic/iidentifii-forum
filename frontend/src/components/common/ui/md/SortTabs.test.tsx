import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import SortTabs from './SortTabs';

describe('SortTabs', () => {
  it('marks the ordering currently in use', () => {
    render(<SortTabs onChange={vi.fn()} order="Descending" sort="LikeCount" />);

    expect(screen.getByRole('button', { name: 'Top', pressed: true })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Latest' })).toHaveAttribute('aria-pressed', 'false');
  });

  it('reports both the field and the direction when chosen', async () => {
    const onChange = vi.fn();
    render(<SortTabs onChange={onChange} order="Descending" sort="CreatedAt" />);

    await userEvent.click(screen.getByRole('button', { name: 'Oldest' }));

    expect(onChange).toHaveBeenCalledWith('CreatedAt', 'Ascending');
  });

  it('offers like count in both directions', async () => {
    const onChange = vi.fn();
    render(<SortTabs onChange={onChange} order="Descending" sort="CreatedAt" />);

    await userEvent.click(screen.getByRole('button', { name: 'Least liked' }));

    expect(onChange).toHaveBeenCalledWith('LikeCount', 'Ascending');
  });

  it('marks the ascending like count ordering when the address asks for it', () => {
    render(<SortTabs onChange={vi.fn()} order="Ascending" sort="LikeCount" />);

    expect(screen.getByRole('button', { name: 'Least liked', pressed: true })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Top' })).toHaveAttribute('aria-pressed', 'false');
  });
});
