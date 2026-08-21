import type { IUser } from '@src/domains/users/User';

/***** Constants *****/

const STORAGE_KEY = 'forum.session';

/***** Types *****/

export interface ISession {
  token: string;
  expiresAt: string;
  user: IUser;
}

/***** Functions *****/

/**
 * Reads the stored session, discarding one that has lapsed. Kept in local storage so a reload
 * does not sign the reader out; see docs/explanation/security-model.md for the trade-off.
 */
export function getStoredSession(): ISession | null {
  const stored = localStorage.getItem(STORAGE_KEY);

  if (stored === null) {
    return null;
  }

  try {
    const session = JSON.parse(stored) as ISession;
    return new Date(session.expiresAt) > new Date() ? session : null;
  } catch {
    // Anything unreadable is treated as no session at all.
    return null;
  }
}

export function storeSession(session: ISession): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearStoredSession(): void {
  localStorage.removeItem(STORAGE_KEY);
}
