import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import LikeButton from './LikeButton';

describe('LikeButton', () => {
  it('shows the count and offers to like', async () => {
    const onToggle = vi.fn();
    render(<LikeButton count={14} isLiked={false} isPending={false} onToggle={onToggle} />);

    // The label states the count as well as the action: it replaces the text inside the
    // button, so anything left out of it is never announced.
    const button = screen.getByRole('button', { name: 'Like this discussion. 14 likes' });
    expect(button).toHaveTextContent('14');
    expect(button).toHaveAttribute('aria-pressed', 'false');

    await userEvent.click(button);

    expect(onToggle).toHaveBeenCalledOnce();
  });

  it('offers to take a like back once it has been given', () => {
    render(<LikeButton count={1} isLiked isPending={false} onToggle={vi.fn()} />);

    expect(
      screen.getByRole('button', { name: 'Remove your like. 1 like', pressed: true }),
    ).toBeEnabled();
  });

  it('says why it cannot be used, rather than doing nothing when pressed', async () => {
    const onToggle = vi.fn();
    render(
      <LikeButton
        count={3}
        disabledReason="You cannot like your own discussion"
        isLiked={false}
        isPending={false}
        onToggle={onToggle}
      />,
    );

    // A disabled control cannot be focused, so the reason has to reach a reader through the
    // label rather than through the tooltip alone.
    const button = screen.getByRole('button', {
      name: 'You cannot like your own discussion. 3 likes',
    });
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute('title', 'You cannot like your own discussion');

    await userEvent.click(button);

    expect(onToggle).not.toHaveBeenCalled();
  });

  it('cannot be pressed twice while a change is in flight', () => {
    render(<LikeButton count={3} isLiked={false} isPending onToggle={vi.fn()} />);

    expect(screen.getByRole('button')).toBeDisabled();
  });
});
