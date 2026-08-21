import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import Spinner from '@src/components/common/ui/sm/Spinner';
import Paths from '@src/domains/common/constants/Paths';
import UserService from '@src/domains/users/UserService';
import { HttpError } from '@src/infra/http';
import AuthCard from '../common/AuthCard';

/***** Components *****/

/**
 * Default component: confirms the address using the token the emailed link carries. Arriving here
 * is the confirmation, so nothing is asked of the reader.
 *
 * Written as a query keyed on the token rather than something fired from an effect: the result
 * then belongs to the token, and survives a remount instead of being asked for twice or lost.
 */
function VerifyEmail() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const verify = useQuery({
    queryKey: ['verify-email', token],
    queryFn: () => UserService.verifyEmail(token),
    enabled: token !== '',
    retry: false,
    staleTime: Infinity,
  });

  if (token === '') {
    return (
      <AuthCard title="Nothing to confirm">
        <Banner tone="error">This address has no confirmation link in it.</Banner>
        <p className="text-center text-sm text-muted">
          <Link className="text-accent underline" to={Paths.CheckInbox}>
            Ask for a link
          </Link>
        </p>
      </AuthCard>
    );
  }

  if (verify.isPending) {
    return (
      <AuthCard title="Confirming your address">
        <div className="flex justify-center">
          <Spinner label="Confirming" />
        </div>
      </AuthCard>
    );
  }

  if (verify.isError) {
    return (
      <AuthCard title="That link did not work">
        <Banner tone="error">{_messageFrom(verify.error)}</Banner>
        <p className="text-center text-sm text-muted">
          <Link className="text-accent underline" to={Paths.CheckInbox}>
            Ask for another link
          </Link>
        </p>
      </AuthCard>
    );
  }

  return (
    <AuthCard title="Your address is confirmed">
      <Banner tone="success">Your account is ready.</Banner>
      <Link
        className="rounded-sm bg-ink px-3 py-2 text-center text-sm font-medium text-canvas"
        to={Paths.Login}
      >
        Log in
      </Link>
    </AuthCard>
  );
}

/***** Functions *****/

function _messageFrom(error: unknown): string {
  return error instanceof HttpError && error.message
    ? error.message
    : 'This link has expired or has already been used.';
}

/***** Export default *****/

export default VerifyEmail;
