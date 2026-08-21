import { Route, Routes, useLocation } from 'react-router';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import Paths from '@src/domains/common/constants/Paths';
import UserService from '@src/domains/users/UserService';
import { HttpError } from '@src/infra/http';
import { renderWithProviders } from '@src/test/render';
import Register from './Register';

/***** Functions *****/

async function fillIn(overrides: Partial<Record<'Username' | 'Email' | 'Password', string>> = {}) {
  const values = {
    Username: 'newmember',
    Email: 'newmember@example.com',
    Password: 'Password123!',
    ...overrides,
  };

  for (const [label, value] of Object.entries(values)) {
    if (value !== '') {
      await userEvent.type(screen.getByLabelText(label), value);
    }
  }
}

/** Stands in for the inbox page, so a test can read where the form sent the reader. */
function LandedAt() {
  const location = useLocation();

  return <span data-testid="landed-at">{`${location.pathname}${location.search}`}</span>;
}

/***** Tests *****/

describe('Register', () => {
  afterEach(() => vi.restoreAllMocks());

  it('sends the account to the API and moves on to the inbox', async () => {
    const register = vi
      .spyOn(UserService, 'register')
      .mockResolvedValue({ message: 'Check your email.' });

    renderWithProviders(
      <Routes>
        <Route element={<Register />} path={Paths.Register} />
        <Route element={<LandedAt />} path={Paths.CheckInbox} />
      </Routes>,
      Paths.Register,
    );
    await fillIn();
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    await waitFor(() =>
      expect(register).toHaveBeenCalledWith({
        username: 'newmember',
        email: 'newmember@example.com',
        password: 'Password123!',
      }),
    );

    expect(await screen.findByTestId('landed-at')).toHaveTextContent(
      Paths.checkInbox('newmember@example.com'),
    );
  });

  it('refuses a username the API would refuse, without asking it', async () => {
    const register = vi.spyOn(UserService, 'register');

    renderWithProviders(<Register />);
    await fillIn({ Username: 'no' });
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByText(/3 to 32 letters/)).toBeInTheDocument();
    expect(register).not.toHaveBeenCalled();
  });

  it('refuses a password shorter than the minimum', async () => {
    const register = vi.spyOn(UserService, 'register');

    renderWithProviders(<Register />);
    await fillIn({ Password: 'short' });
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByText(/at least 8 characters/i)).toBeInTheDocument();
    expect(register).not.toHaveBeenCalled();
  });

  it('shows the API refusal against the field it belongs to', async () => {
    vi.spyOn(UserService, 'register').mockRejectedValue(
      new HttpError(400, 'Invalid request.', { Username: ['That name is already taken.'] }),
    );

    renderWithProviders(<Register />);
    await fillIn();
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByText('That name is already taken.')).toBeInTheDocument();
  });

  it('reports a refusal that belongs to no single field', async () => {
    vi.spyOn(UserService, 'register').mockRejectedValue(
      new HttpError(409, 'That username or email address is already registered.'),
    );

    renderWithProviders(<Register />);
    await fillIn();
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already registered');
  });
});
