import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import AuthProvider from './AuthProvider';
import { useAuth } from './useAuth';
import { storeSession } from './session-storage';

/***** Constants *****/

const SESSION = {
  token: 'a-token',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  user: { id: '1', username: 'alice', email: 'alice@forum.local', role: 'Member' as const },
};

/***** Components *****/

/** Shows what the provider is holding, and can change it. */
function Harness() {
  const { user, isSignedIn, signIn, signOut } = useAuth();

  return (
    <>
      <output>{isSignedIn ? user?.username : 'nobody'}</output>
      <button onClick={() => signIn(SESSION)} type="button">
        Sign in
      </button>
      <button onClick={signOut} type="button">
        Sign out
      </button>
    </>
  );
}

function renderHarness() {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <AuthProvider>
        <Harness />
      </AuthProvider>
    </QueryClientProvider>,
  );
}

/***** Tests *****/

describe('AuthProvider', () => {
  beforeEach(() => localStorage.clear());

  it('starts with nobody signed in', () => {
    renderHarness();

    expect(screen.getByRole('status')).toHaveTextContent('nobody');
  });

  /** A reload should not sign the reader out. */
  it('picks up a session stored earlier', () => {
    storeSession(SESSION);

    renderHarness();

    expect(screen.getByRole('status')).toHaveTextContent('alice');
  });

  it('ignores a stored session that has lapsed', () => {
    storeSession({ ...SESSION, expiresAt: new Date(Date.now() - 1_000).toISOString() });

    renderHarness();

    expect(screen.getByRole('status')).toHaveTextContent('nobody');
  });

  it('remembers a sign-in across a reload', async () => {
    renderHarness();

    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(screen.getByRole('status')).toHaveTextContent('alice');
    expect(localStorage.getItem('forum.session')).toContain('alice');
  });

  it('forgets everything on signing out', async () => {
    storeSession(SESSION);
    renderHarness();

    await userEvent.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(screen.getByRole('status')).toHaveTextContent('nobody');
    expect(localStorage.getItem('forum.session')).toBeNull();
  });
});
