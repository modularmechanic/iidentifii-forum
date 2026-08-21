import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useMutation } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import Input from '@src/components/common/ui/sm/Input';
import Paths from '@src/domains/common/constants/Paths';
import UserService from '@src/domains/users/UserService';
import { HttpError } from '@src/infra/http';
import AuthCard from '../common/AuthCard';

/***** Components *****/

/**
 * Default component: the first step of signing in. A correct password sends a code and moves on;
 * it does not sign anybody in on its own.
 */
function Login() {
  const navigate = useNavigate();

  const [needsConfirming, setNeedsConfirming] = useState(false);

  const signIn = useMutation({
    mutationFn: (credentials: { username: string; password: string }) =>
      UserService.beginSignIn(credentials.username, credentials.password),
    onSuccess: (challenge) => navigate(Paths.LoginCode, { state: { challenge }, replace: true }),
    onError: (error) => setNeedsConfirming(error instanceof HttpError && error.status === 403),
  });

  return (
    <AuthCard
      description="We send a code to your email address to finish signing in."
      title="Log in"
    >
      <form
        className="flex flex-col gap-3"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          setNeedsConfirming(false);
          signIn.mutate({
            username: String(data.get('username') ?? '').trim(),
            password: String(data.get('password') ?? ''),
          });
        }}
      >
        {needsConfirming && (
          <Banner tone="info">
            Confirm your email address before signing in.{' '}
            <Link className="underline" to={Paths.CheckInbox}>
              Send the link again
            </Link>
            .
          </Banner>
        )}

        {signIn.isError && !needsConfirming && (
          <Banner tone="error">{_messageFrom(signIn.error)}</Banner>
        )}

        <Input autoComplete="username" label="Username" name="username" type="text" />
        <Input autoComplete="current-password" label="Password" name="password" type="password" />

        <button
          className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-canvas disabled:opacity-60"
          disabled={signIn.isPending}
          type="submit"
        >
          {signIn.isPending ? 'Checking…' : 'Continue'}
        </button>

        <div className="flex items-center justify-between text-sm text-muted">
          <Link className="text-accent underline" to={Paths.Register}>
            Create an account
          </Link>
          <Link className="text-accent underline" to={Paths.ForgotPassword}>
            Forgot password?
          </Link>
        </div>
      </form>
    </AuthCard>
  );
}

/***** Functions *****/

function _messageFrom(error: unknown): string {
  return error instanceof HttpError && error.message
    ? error.message
    : 'Could not sign you in. Try again.';
}

/***** Export default *****/

export default Login;
