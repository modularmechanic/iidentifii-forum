import type { ReactNode } from 'react';
import { Link, useNavigate } from 'react-router';
import Avatar from '@src/components/common/ui/sm/Avatar';
import Paths from '@src/domains/common/constants/Paths';
import UserOps from '@src/domains/users/UserOps';
import { useAuth } from '@src/infra/auth/useAuth';

/***** Constants *****/

/** The skip link and the landmark it jumps to have to agree on this. */
const MAIN_ID = 'content';

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
      <SkipLink />
      <SiteHeader />
      <main className="mx-auto max-w-4xl px-4 py-8 sm:px-6" id={MAIN_ID}>
        {children}
      </main>
    </div>
  );
}

/** Lets a keyboard reader step over the masthead instead of tabbing through it on every page. */
function SkipLink() {
  return (
    <a
      className="sr-only focus:not-sr-only focus:absolute focus:z-10 focus:m-2 focus:rounded-sm focus:border focus:border-line focus:bg-surface focus:px-3 focus:py-2 focus:text-sm"
      href={`#${MAIN_ID}`}
    >
      Skip to the content
    </a>
  );
}

/** The masthead, holding branding and whichever account controls apply. */
function SiteHeader() {
  return (
    <header className="border-b border-line">
      {/* Wraps rather than squeezing: at 375px the account controls take a second line instead of
          breaking the wordmark and the buttons across two lines each. */}
      <div className="mx-auto flex min-h-14 max-w-4xl flex-wrap items-center gap-3 px-4 py-2 sm:px-6">
        <Link className="flex items-center gap-2 font-semibold whitespace-nowrap" to={Paths.Home}>
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
        <Link
          className="px-2 py-1.5 text-sm whitespace-nowrap text-muted hover:text-ink sm:px-3"
          to={Paths.Login}
        >
          Log in
        </Link>
        <Link
          className="rounded-sm border border-line px-2 py-1.5 text-sm whitespace-nowrap sm:px-3"
          to={Paths.Register}
        >
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
        className="px-2 py-1.5 text-sm whitespace-nowrap text-muted hover:text-ink sm:px-3"
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
