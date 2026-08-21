import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import Switch from './Switch';

describe('Switch', () => {
  it('reports whether it is on', () => {
    render(<Switch isOn label="Filters" onChange={vi.fn()} />);

    expect(screen.getByRole('switch', { name: 'Filters', checked: true })).toBeInTheDocument();
  });

  it('asks for the opposite of its current state when pressed', async () => {
    const onChange = vi.fn();
    render(<Switch isOn label="Filters" onChange={onChange} />);

    await userEvent.click(screen.getByRole('switch'));

    expect(onChange).toHaveBeenCalledWith(false);
  });
});
