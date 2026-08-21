import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useMutation } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import Paths from '@src/domains/common/constants/Paths';
import UserService from '@src/domains/users/UserService';
import AuthCard from '../common/AuthCard';

/***** Constants *****/

const COOLDOWN_SECONDS = 60;

/***** Components *****/

/** Default component: tells the reader to confirm their address, and can send the link again. */
function CheckInbox() {
  const [searchParams] = useSearchParams();
  const email = searchParams.get('email') ?? '';

  const secondsLeft = useCountdown(COOLDOWN_SECONDS);

  const resend = useMutation({
    mutationFn: () => UserService.resendVerification(email),
  });

  return (
    <AuthCard title="Check your inbox">
      <Banner tone="info">
        A confirmation link is on its way
        {email && (
          <>
            {' '}
            to <strong>{email}</strong>
          </>
        )}
        . It works once and expires in an hour.
      </Banner>

      {resend.isSuccess && <Banner tone="success">Sent. Give it a moment to arrive.</Banner>}
      {resend.isError && (
        <Banner tone="error">Could not send another link. Try again shortly.</Banner>
      )}

      <div className="flex items-center justify-between gap-3">
        <button
          className="rounded-sm border border-line px-3 py-2 text-sm disabled:opacity-50"
          disabled={secondsLeft > 0 || resend.isPending || email === ''}
          onClick={() => resend.mutate()}
          type="button"
        >
          {resend.isPending ? 'Sending…' : 'Send it again'}
        </button>
        {secondsLeft > 0 && (
          <span aria-live="polite" className="font-mono text-xs text-muted tabular-nums">
            available in {secondsLeft}s
          </span>
        )}
      </div>

      <p className="text-center text-sm text-muted">
        Once confirmed,{' '}
        <Link className="text-accent underline" to={Paths.Login}>
          log in
        </Link>
        .
      </p>
    </AuthCard>
  );
}

/***** Functions *****/

/**
 * Counts down to zero. The wait matches the one the API keeps, so the button is only offered
 * when pressing it would actually send something.
 */
function useCountdown(seconds: number): number {
  const [remaining, setRemaining] = useState(seconds);

  useEffect(() => {
    if (remaining <= 0) {
      return;
    }

    const timer = setTimeout(() => setRemaining((value) => value - 1), 1000);
    return () => clearTimeout(timer);
  }, [remaining]);

  return remaining;
}

/***** Export default *****/

export default CheckInbox;
