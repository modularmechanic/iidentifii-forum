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
 * Kept in its own module so the provider's file exports a component and nothing else, and so a
 * component that only needs to read the session does not have to import the provider to get it.
 */
export const AuthContext = createContext<IAuthValue | null>(null);
