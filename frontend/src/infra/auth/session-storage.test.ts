import { beforeEach, describe, expect, it } from 'vitest';
import { clearStoredSession, getStoredSession, storeSession } from './session-storage';

/***** Functions *****/

function buildSession(expiresAt: Date) {
  return {
    token: 'a-token',
    expiresAt: expiresAt.toISOString(),
    user: { id: '1', username: 'alice', email: 'alice@forum.local', role: 'Member' as const },
  };
}

/***** Tests *****/

describe('session storage', () => {
  beforeEach(() => localStorage.clear());

  it('gives back a session that is still valid', () => {
    const session = buildSession(new Date(Date.now() + 3_600_000));

    storeSession(session);

    expect(getStoredSession()).toEqual(session);
  });

  /** A lapsed session is worse than none: it would show a signed-in page that cannot fetch. */
  it('discards a session that has lapsed', () => {
    storeSession(buildSession(new Date(Date.now() - 1_000)));

    expect(getStoredSession()).toBeNull();
  });

  it('treats unreadable storage as no session', () => {
    localStorage.setItem('forum.session', 'not json at all');

    expect(getStoredSession()).toBeNull();
  });

  it('reports no session once cleared', () => {
    storeSession(buildSession(new Date(Date.now() + 3_600_000)));

    clearStoredSession();

    expect(getStoredSession()).toBeNull();
  });
});
