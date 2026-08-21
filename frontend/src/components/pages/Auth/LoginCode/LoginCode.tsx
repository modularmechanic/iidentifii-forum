import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router';
import { useMutation } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import CodeInput from '@src/components/common/ui/sm/CodeInput';
import Paths from '@src/domains/common/constants/Paths';
import UserService, { type ILoginChallenge } from '@src/domains/users/UserService';
import { useAuth } from '@src/infra/auth/useAuth';
import { HttpError } from '@src/infra/http';
import AuthCard from '../common/AuthCard';

/***** Constants *****/

const CODE_LENGTH = 6;

/***** Components *****/

/**
 * Default component: the second step of signing in. The challenge arrives through navigation
 * state rather than the address bar, so a half-finished sign-in cannot be shared as a link.
 */
function LoginCode() {
  const location = useLocation();
  const navigate = useNavigate();
  const { signIn } = useAuth();

  const [code, setCode] = useState('');

  const challenge = (location.state as { challenge?: ILoginChallenge } | null)?.challenge;

  const complete = useMutation({
    mutationFn: (entered: string) => UserService.completeSignIn(challenge!.challengeId, entered),
    onSuccess: (session) => {
      signIn(session);
      navigate(Paths.Home, { replace: true });
    },
  });

  // Arriving here directly means there is nothing to confirm, so the journey starts again.
  if (challenge === undefined) {
    return <Navigate replace to={Paths.Login} />;
  }

  return (
    <AuthCard title="Enter your code">
      <p className="text-center text-sm text-muted">
        We sent a six-digit code to <strong>{challenge.maskedEmail}</strong>. It expires in ten
        minutes.
      </p>

      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          complete.mutate(code);
        }}
      >
        <CodeInput onChange={setCode} value={code} />

        {complete.isError && <Banner tone="error">{_messageFrom(complete.error)}</Banner>}

        <button
          className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-canvas disabled:opacity-60"
          disabled={code.length < CODE_LENGTH || complete.isPending}
          type="submit"
        >
          {complete.isPending ? 'Signing you in…' : 'Log in'}
        </button>

        <p className="text-center text-sm text-muted">
          <Link className="text-accent underline" to={Paths.Login}>
            Start again
          </Link>
        </p>
      </form>
    </AuthCard>
  );
}

/***** Functions *****/

function _messageFrom(error: unknown): string {
  return error instanceof HttpError && error.message
    ? error.message
    : 'That code is wrong or has expired.';
}

/***** Export default *****/

export default LoginCode;
