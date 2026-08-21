import type { ReactNode } from 'react';
import { Link } from 'react-router';
import Paths from '@src/domains/common/constants/Paths';

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

/** The masthead, holding branding and the account controls. */
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
        <span className="text-sm text-muted">Integration community</span>
      </div>
    </header>
  );
}

/***** Export default *****/

export default AppShell;
