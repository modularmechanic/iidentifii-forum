import { createContext } from 'react';
import type { IUser } from '@src/domains/users/User';
import type { ISession } from './session-storage';

/***** Types *****/

export interface IAuthValue {
  user: IUser | null;
  isSignedIn: boolean;
  signIn: (session: ISession) => void;
  signOut: () => void;
}

/***** Constants *****/

/**
 * Held in its own file rather than beside the provider: a module that exports both a component and
 * something else loses fast refresh.
 */
export const AuthContext = createContext<IAuthValue | null>(null);
