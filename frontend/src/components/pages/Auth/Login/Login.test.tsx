import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import UserService from '@src/domains/users/UserService';
import { HttpError } from '@src/infra/http';
import { renderWithProviders } from '@src/test/render';
import Login from './Login';

/***** Functions *****/

async function signIn(username = 'alice', password = 'Password123!') {
  await userEvent.type(screen.getByLabelText('Username'), username);
  await userEvent.type(screen.getByLabelText('Password'), password);
  await userEvent.click(screen.getByRole('button', { name: 'Continue' }));
}

/***** Tests *****/

describe('Login', () => {
  afterEach(() => vi.restoreAllMocks());

  it('asks the API to start a sign-in rather than signing anybody in', async () => {
    const begin = vi.spyOn(UserService, 'beginSignIn').mockResolvedValue({
      challengeId: 'c1',
      maskedEmail: 'a****@forum.local',
      expiresAt: new Date(Date.now() + 600_000).toISOString(),
    });

    renderWithProviders(<Login />);
    await signIn();

    await waitFor(() => expect(begin).toHaveBeenCalledWith('alice', 'Password123!'));
  });

  it('reports a wrong password without saying which part was wrong', async () => {
    vi.spyOn(UserService, 'beginSignIn').mockRejectedValue(
      new HttpError(401, 'That username and password do not match.'),
    );

    renderWithProviders(<Login />);
    await signIn();

    expect(await screen.findByRole('alert')).toHaveTextContent('do not match');
  });

  /** An unconfirmed address is a different problem, and has a different way out. */
  it('offers the confirmation link when the address is unconfirmed', async () => {
    vi.spyOn(UserService, 'beginSignIn').mockRejectedValue(
      new HttpError(403, 'Confirm your email address before signing in.'),
    );

    renderWithProviders(<Login />);
    await signIn();

    expect(await screen.findByRole('status')).toHaveTextContent('Confirm your email address');
    expect(screen.getByRole('link', { name: 'Send the link again' })).toBeInTheDocument();
  });

  it('offers a way to a forgotten password and to a new account', () => {
    renderWithProviders(<Login />);

    expect(screen.getByRole('link', { name: 'Forgot password?' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Create an account' })).toBeInTheDocument();
  });
});
