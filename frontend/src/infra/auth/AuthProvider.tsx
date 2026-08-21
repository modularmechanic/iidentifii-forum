import { useCallback, useMemo, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { setAuthorizationHeader } from '@src/infra/http';
import { AuthContext, type IAuthValue } from './auth-context';
import {
  clearStoredSession,
  getStoredSession,
  storeSession,
  type ISession,
} from './session-storage';

/***** Types *****/

interface IProps {
  children: ReactNode;
}

/***** Components *****/

/**
 * Default component: holds who is signed in. The stored session is read once at startup, so a
 * reload does not sign the reader out, and the token is handed to the fetch wrapper rather than
 * threaded through every call.
 */
function AuthProvider(props: IProps) {
  const { children } = props;

  const queryClient = useQueryClient();

  const [session, setSession] = useState<ISession | null>(() => {
    const stored = getStoredSession();
    setAuthorizationHeader(stored?.token ?? null);
    return stored;
  });

  const signIn = useCallback(
    (next: ISession) => {
      storeSession(next);
      setAuthorizationHeader(next.token);
      setSession(next);

      // Anything already fetched was fetched as somebody else, including whether a discussion
      // is liked, so it is asked for again rather than shown to the wrong person.
      void queryClient.invalidateQueries();
    },
    [queryClient],
  );

  const signOut = useCallback(() => {
    clearStoredSession();
    setAuthorizationHeader(null);
    setSession(null);

    // Cleared rather than invalidated: none of it belongs to whoever is here now.
    queryClient.clear();
  }, [queryClient]);

  const value = useMemo<IAuthValue>(
    () => ({ user: session?.user ?? null, isSignedIn: session !== null, signIn, signOut }),
    [session, signIn, signOut],
  );

  return <AuthContext value={value}>{children}</AuthContext>;
}

/***** Export default *****/

export default AuthProvider;
