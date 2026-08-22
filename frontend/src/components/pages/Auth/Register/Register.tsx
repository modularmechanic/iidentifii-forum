import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useMutation } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import Input from '@src/components/common/ui/sm/Input';
import Paths from '@src/domains/common/constants/Paths';
import UserService, { type IRegisterRequest } from '@src/domains/users/UserService';
import { HttpError } from '@src/infra/http';
import AuthCard from '../common/AuthCard';

/***** Constants *****/

const USERNAME_PATTERN = /^[A-Za-z0-9_]{3,32}$/;
const MIN_PASSWORD_LENGTH = 8;

/***** Types *****/

type FieldErrors = Partial<Record<keyof IRegisterRequest, string>>;

/***** Components *****/

/** Default component: creates an account, then sends the reader to check their email. */
function Register() {
  const navigate = useNavigate();

  const [errors, setErrors] = useState<FieldErrors>({});

  const register = useMutation({
    // Called through, rather than passed by reference: the caller supplies a second argument of
    // its own, which the service should never receive.
    mutationFn: (request: IRegisterRequest) => UserService.register(request),
    onSuccess: (_result, request) => navigate(Paths.checkInbox(request.email)),
    onError: (error) => setErrors(_fieldErrorsFrom(error)),
  });

  return (
    <AuthCard
      description="An account lets you post, reply and like. Reading needs no account."
      title="Create an account"
    >
      <form
        className="flex flex-col gap-3"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const request = {
            username: String(data.get('username') ?? '').trim(),
            email: String(data.get('email') ?? '').trim(),
            password: String(data.get('password') ?? ''),
          };

          const found = _validate(request);
          setErrors(found);

          if (Object.keys(found).length === 0) {
            register.mutate(request);
          }
        }}
      >
        {register.isError && Object.keys(errors).length === 0 && (
          <Banner tone="error">{_messageFrom(register.error)}</Banner>
        )}

        <Input
          autoComplete="username"
          error={errors.username}
          hint="3 to 32 characters: letters, numbers and underscores."
          label="Username"
          name="username"
          type="text"
        />
        <Input
          autoComplete="email"
          error={errors.email}
          hint="We send a confirmation link and your sign-in codes here."
          label="Email"
          name="email"
          type="email"
        />
        <Input
          autoComplete="new-password"
          error={errors.password}
          hint={`At least ${MIN_PASSWORD_LENGTH} characters.`}
          label="Password"
          name="password"
          type="password"
        />

        <button
          className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-canvas disabled:opacity-60"
          disabled={register.isPending}
          type="submit"
        >
          {register.isPending ? 'Creating your account…' : 'Create account'}
        </button>

        {/* The API refuses an address that already has an account and says to sign in or ask for
            a new password, so both ways forward are one click from the refusal. */}
        <p className="text-center text-sm text-muted">
          Already a member?{' '}
          <Link className="text-accent underline" to={Paths.Login}>
            Log in
          </Link>{' '}
          or{' '}
          <Link className="text-accent underline" to={Paths.ForgotPassword}>
            reset your password
          </Link>
          .
        </p>
      </form>
    </AuthCard>
  );
}

/***** Functions *****/

/** Checks what the API checks, so an obvious mistake is caught without a round trip. */
function _validate(request: IRegisterRequest): FieldErrors {
  const found: FieldErrors = {};

  if (!USERNAME_PATTERN.test(request.username)) {
    found.username = 'Use 3 to 32 letters, numbers or underscores.';
  }

  if (!request.email.includes('@')) {
    found.email = 'Enter an email address you can read.';
  }

  if (request.password.length < MIN_PASSWORD_LENGTH) {
    found.password = `Use at least ${MIN_PASSWORD_LENGTH} characters.`;
  }

  return found;
}

/** Turns the API's field errors into the shape the form already understands. */
function _fieldErrorsFrom(error: unknown): FieldErrors {
  if (!(error instanceof HttpError)) {
    return {};
  }

  const found: FieldErrors = {};
  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const key = field.toLowerCase() as keyof IRegisterRequest;
    if (key === 'username' || key === 'email' || key === 'password') {
      found[key] = messages[0];
    }
  }

  return found;
}

function _messageFrom(error: unknown): string {
  return error instanceof HttpError && error.message
    ? error.message
    : 'Could not create your account. Try again.';
}

/***** Export default *****/

export default Register;
