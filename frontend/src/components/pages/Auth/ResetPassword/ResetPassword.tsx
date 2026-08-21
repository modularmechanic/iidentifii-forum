import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useMutation } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import Input from '@src/components/common/ui/sm/Input';
import Paths from '@src/domains/common/constants/Paths';
import UserService from '@src/domains/users/UserService';
import { HttpError } from '@src/infra/http';
import AuthCard from '../common/AuthCard';

/***** Constants *****/

const MIN_PASSWORD_LENGTH = 8;

/***** Components *****/

/** Default component: sets a new password, using the token the emailed link carries. */
function ResetPassword() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const [error, setError] = useState<string>();

  const reset = useMutation({
    mutationFn: (newPassword: string) => UserService.resetPassword(token, newPassword),
  });

  if (token === '') {
    return (
      <AuthCard title="Nothing to reset">
        <Banner tone="error">This address has no reset link in it.</Banner>
        <p className="text-center text-sm text-muted">
          <Link className="text-accent underline" to={Paths.ForgotPassword}>
            Ask for a link
          </Link>
        </p>
      </AuthCard>
    );
  }

  if (reset.isSuccess) {
    return (
      <AuthCard title="Your password is changed">
        <Banner tone="success">You can sign in with it now.</Banner>
        <Link
          className="rounded-sm bg-ink px-3 py-2 text-center text-sm font-medium text-canvas"
          to={Paths.Login}
        >
          Log in
        </Link>
      </AuthCard>
    );
  }

  return (
    <AuthCard title="Choose a new password">
      <form
        className="flex flex-col gap-3"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const password = String(data.get('password') ?? '');
          const confirmation = String(data.get('confirmation') ?? '');

          if (password.length < MIN_PASSWORD_LENGTH) {
            setError(`Use at least ${MIN_PASSWORD_LENGTH} characters.`);
            return;
          }

          if (password !== confirmation) {
            setError('Both entries need to match.');
            return;
          }

          setError(undefined);
          reset.mutate(password);
        }}
      >
        {reset.isError && <Banner tone="error">{_messageFrom(reset.error)}</Banner>}

        <Input
          autoComplete="new-password"
          error={error}
          hint={`At least ${MIN_PASSWORD_LENGTH} characters.`}
          label="New password"
          name="password"
          type="password"
        />
        <Input
          autoComplete="new-password"
          label="Confirm new password"
          name="confirmation"
          type="password"
        />

        <button
          className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-canvas disabled:opacity-60"
          disabled={reset.isPending}
          type="submit"
        >
          {reset.isPending ? 'Saving…' : 'Save password'}
        </button>
      </form>
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

export default ResetPassword;
