import { fetchJson } from '@src/infra/http';
import type { IAcknowledgement } from './User';

/***** Types *****/

export interface IRegisterRequest {
  username: string;
  email: string;
  password: string;
}

/***** Functions *****/

/** Everything the forum knows how to do with an account. */
const UserService = {
  async register(request: IRegisterRequest): Promise<IAcknowledgement> {
    return await fetchJson<IAcknowledgement>('/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  },

  async verifyEmail(token: string): Promise<IAcknowledgement> {
    return await fetchJson<IAcknowledgement>('/auth/verify-email', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token }),
    });
  },

  async resendVerification(email: string): Promise<IAcknowledgement> {
    return await fetchJson<IAcknowledgement>('/auth/resend-verification', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email }),
    });
  },
} as const;

export default UserService;
