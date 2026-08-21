import { Link } from 'react-router';
import { useMutation } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import Input from '@src/components/common/ui/sm/Input';
import Paths from '@src/domains/common/constants/Paths';
import UserService from '@src/domains/users/UserService';
import AuthCard from '../common/AuthCard';

/***** Components *****/

/** Default component: asks for a link to set a new password. */
function ForgotPassword() {
  const request = useMutation({
    mutationFn: (email: string) => UserService.forgotPassword(email),
  });

  return (
    <AuthCard
      description="Enter the address on your account and we will send a link to set a new password."
      title="Reset your password"
    >
      <form
        className="flex flex-col gap-3"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          request.mutate(String(data.get('email') ?? '').trim());
        }}
      >
        {/* Said whether or not the address has an account, so the reply reveals nothing. */}
        {request.isSuccess && (
          <Banner tone="success">
            If that address has an account, a link is on its way. It expires in an hour.
          </Banner>
        )}

        {request.isError && (
          <Banner tone="error">Could not send the link. Try again shortly.</Banner>
        )}

        <Input autoComplete="email" label="Email" name="email" type="email" />

        <button
          className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-canvas disabled:opacity-60"
          disabled={request.isPending}
          type="submit"
        >
          {request.isPending ? 'Sending…' : 'Send the link'}
        </button>

        <p className="text-center text-sm text-muted">
          <Link className="text-accent underline" to={Paths.Login}>
            Back to log in
          </Link>
        </p>
      </form>
    </AuthCard>
  );
}

/***** Export default *****/

export default ForgotPassword;
