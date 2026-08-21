import type { ReactNode } from 'react';
import { Link, useNavigate } from 'react-router';
import Avatar from '@src/components/common/ui/sm/Avatar';
import Paths from '@src/domains/common/constants/Paths';
import UserOps from '@src/domains/users/UserOps';
import { useAuth } from '@src/infra/auth/useAuth';

/***** Types *****/

interface IProps {
  children: ReactNode;
}

/***** Components *****/

/** Default component: the frame every page renders inside. */
function AppShell(props: IProps) {
  const { children } = props;

  return (
    <div className="min-h-screen bg-canvas">
      <SiteHeader />
      <main className="mx-auto max-w-4xl px-6 py-8">{children}</main>
    </div>
  );
}

/** The masthead, holding branding and whichever account controls apply. */
function SiteHeader() {
  return (
    <header className="border-b border-line">
      <div className="mx-auto flex h-14 max-w-4xl items-center gap-3 px-6">
        <Link className="flex items-center gap-2 font-semibold" to={Paths.Home}>
          <span className="grid size-6 place-items-center rounded bg-ink font-mono text-xs text-canvas">
            ii
          </span>
          iiDENTIFii Forum
        </Link>
        <span className="hidden text-sm text-muted sm:inline">Integration community</span>
        <AccountControls />
      </div>
    </header>
  );
}

/** Who is signed in, or the way to become somebody. */
function AccountControls() {
  const { user, isSignedIn, signOut } = useAuth();
  const navigate = useNavigate();

  if (!isSignedIn || user === null) {
    return (
      <nav className="ml-auto flex items-center gap-2">
        <Link className="px-3 py-1.5 text-sm text-muted hover:text-ink" to={Paths.Login}>
          Log in
        </Link>
        <Link className="rounded-sm border border-line px-3 py-1.5 text-sm" to={Paths.Register}>
          Join the forum
        </Link>
      </nav>
    );
  }

  return (
    <nav className="ml-auto flex items-center gap-3">
      <span className="flex items-center gap-2 text-sm">
        <Avatar username={user.username} />
        {user.username}
        {UserOps.isModerator(user) && (
          <span className="font-mono text-[10px] tracking-wide text-accent uppercase">
            moderator
          </span>
        )}
      </span>
      <button
        className="px-3 py-1.5 text-sm text-muted hover:text-ink"
        onClick={() => {
          signOut();
          navigate(Paths.Home);
        }}
        type="button"
      >
        Log out
      </button>
    </nav>
  );
}

/***** Export default *****/

export default AppShell;
