import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import UserService from '@src/domains/users/UserService';
import { HttpError } from '@src/infra/http';
import { renderWithProviders } from '@src/test/render';
import VerifyEmail from './VerifyEmail';

describe('VerifyEmail', () => {
  afterEach(() => vi.restoreAllMocks());

  it('confirms the address using the token in the address bar', async () => {
    const verify = vi
      .spyOn(UserService, 'verifyEmail')
      .mockResolvedValue({ message: 'Confirmed.' });

    renderWithProviders(<VerifyEmail />, '/verify-email?token=abc123');

    expect(await screen.findByText('Your address is confirmed')).toBeInTheDocument();
    expect(verify).toHaveBeenCalledWith('abc123');
    expect(verify).toHaveBeenCalledTimes(1);
  });

  it('explains a link that has expired or been used', async () => {
    vi.spyOn(UserService, 'verifyEmail').mockRejectedValue(
      new HttpError(400, 'This link has expired or has already been used.'),
    );

    renderWithProviders(<VerifyEmail />, '/verify-email?token=stale');

    expect(await screen.findByRole('alert')).toHaveTextContent('expired or has already been used');
    expect(screen.getByRole('link', { name: 'Ask for another link' })).toBeInTheDocument();
  });

  it('asks for nothing when the address carries no token', () => {
    const verify = vi.spyOn(UserService, 'verifyEmail');

    renderWithProviders(<VerifyEmail />, '/verify-email');

    expect(screen.getByText('Nothing to confirm')).toBeInTheDocument();
    expect(verify).not.toHaveBeenCalled();
  });
});
