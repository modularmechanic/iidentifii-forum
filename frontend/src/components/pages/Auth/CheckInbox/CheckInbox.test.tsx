import { act, fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import Paths from '@src/domains/common/constants/Paths';
import UserService from '@src/domains/users/UserService';
import { renderWithProviders } from '@src/test/render';
import CheckInbox from './CheckInbox';

/***** Constants *****/

const EMAIL = 'member@example.com';
const COOLDOWN_SECONDS = 60;

/***** Functions *****/

/** Ticks the countdown a second at a time, which is how the component schedules it. */
async function waitOutCooldown() {
  for (let second = 0; second < COOLDOWN_SECONDS; second++) {
    await act(() => vi.advanceTimersByTimeAsync(1000));
  }
}

/***** Tests *****/

describe('CheckInbox', () => {
  beforeEach(() => vi.useFakeTimers());

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  /**
   * The API keeps its own cooldown and reports success either way, so a button that reopens as
   * soon as one resend lands only invites presses that send nothing.
   */
  it('waits again before offering another link', async () => {
    const resend = vi
      .spyOn(UserService, 'resendVerification')
      .mockResolvedValue({ message: 'Sent.' });

    renderWithProviders(<CheckInbox />, Paths.checkInbox(EMAIL));

    const button = screen.getByRole('button', { name: 'Send it again' });
    expect(button).toBeDisabled();

    await waitOutCooldown();
    expect(button).toBeEnabled();

    await act(async () => {
      fireEvent.click(button);
    });
    await act(() => vi.advanceTimersByTimeAsync(0));

    expect(resend).toHaveBeenCalledWith(EMAIL);
    expect(button).toBeDisabled();
  });
});
