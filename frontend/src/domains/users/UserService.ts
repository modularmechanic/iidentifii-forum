import { fetchJson } from '@src/infra/http';
import type { IAcknowledgement, IUser } from './User';

/***** Types *****/

export interface IRegisterRequest {
  username: string;
  email: string;
  password: string;
}

/** What the password step returns: a code is on its way, and this says which attempt it is for. */
export interface ILoginChallenge {
  challengeId: string;
  maskedEmail: string;
  expiresAt: string;
}

/** A signed-in session. */
export interface IAuthenticated {
  token: string;
  expiresAt: string;
  user: IUser;
}

/***** Functions *****/

/** Everything the forum knows how to do with an account. */
const UserService = {
  async register(request: IRegisterRequest): Promise<IAcknowledgement> {
    return await post<IAcknowledgement>('/auth/register', request);
  },

  async verifyEmail(token: string): Promise<IAcknowledgement> {
    return await post<IAcknowledgement>('/auth/verify-email', { token });
  },

  async resendVerification(email: string): Promise<IAcknowledgement> {
    return await post<IAcknowledgement>('/auth/resend-verification', { email });
  },

  /** The first step of signing in. A correct password sends a code; it does not sign anybody in. */
  async beginSignIn(username: string, password: string): Promise<ILoginChallenge> {
    return await post<ILoginChallenge>('/auth/login', { username, password });
  },

  /** The second step: the code from the email, exchanged for a session. */
  async completeSignIn(challengeId: string, code: string): Promise<IAuthenticated> {
    return await post<IAuthenticated>('/auth/verify-2fa', { challengeId, code });
  },

  async forgotPassword(email: string): Promise<IAcknowledgement> {
    return await post<IAcknowledgement>('/auth/forgot-password', { email });
  },

  async resetPassword(token: string, newPassword: string): Promise<IAcknowledgement> {
    return await post<IAcknowledgement>('/auth/reset-password', { token, newPassword });
  },

  async fetchSignedIn(): Promise<IUser> {
    return await fetchJson<IUser>('/auth/me');
  },
} as const;

/***** Functions *****/

/** Every account call is a POST of JSON, so the shape of one is written down once. */
async function post<T>(path: string, body: unknown): Promise<T> {
  return await fetchJson<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}

export default UserService;
