import { Link } from 'react-router';
import Paths from '@src/domains/common/constants/Paths';
import { useAuth } from '@src/infra/auth/useAuth';
import PostsContainer from './PostsContainer';

/***** Components *****/

/** Default component: the forum landing page. */
function Home() {
  const { isSignedIn } = useAuth();

  return (
    <div className="flex flex-col gap-6">
      {isSignedIn ? <Welcome /> : <Introduction />}
      <PostsContainer />
    </div>
  );
}

/** A member has already read the pitch; what they need is somewhere to start writing. */
function Welcome() {
  return (
    <section className="flex flex-wrap items-center justify-between gap-3 border-b border-line pb-5">
      <div>
        <h1 className="text-xl font-semibold">Discussions</h1>
        <p className="mt-1 text-sm text-muted">Ask something, or answer somebody who has.</p>
      </div>
      <Link
        className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-surface"
        to={Paths.NewDiscussion}
      >
        Start a discussion
      </Link>
    </section>
  );
}

/** What the forum is for, shown to everyone until there is an account to greet instead. */
function Introduction() {
  return (
    <section className="border-b border-line pb-5">
      <h1 className="text-xl font-semibold">
        Integration questions, answered by the people who ship them
      </h1>
      <p className="mt-2 max-w-prose text-sm text-muted">
        A forum for engineers, clients and partners to ask questions, share integration knowledge,
        and flag anything misleading. Reading is open to everyone.
      </p>
    </section>
  );
}

/***** Export default *****/

export default Home;
