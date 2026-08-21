import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useAuth } from '@src/infra/auth/useAuth';
import Paths from '@src/domains/common/constants/Paths';

/***** Types *****/

interface IProps {
  children: ReactNode;
}

/***** Components *****/

/**
 * Default component: keeps a page for members. Somebody who arrives without a session is sent to
 * sign in, and the address they wanted travels with them so they land there afterwards rather
 * than at the front page.
 */
function RequireAuth(props: IProps) {
  const { children } = props;

  const { isSignedIn } = useAuth();
  const location = useLocation();

  if (!isSignedIn) {
    return (
      <Navigate replace state={{ from: location.pathname + location.search }} to={Paths.Login} />
    );
  }

  return children;
}

/***** Export default *****/

export default RequireAuth;
