import { useContext } from 'react';
import { AuthContext, type IAuthValue } from './auth-context';

/** Who is signed in. Throws outside the provider, which is a wiring mistake rather than a state. */
export function useAuth(): IAuthValue {
  const value = useContext(AuthContext);

  if (value === null) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return value;
}
