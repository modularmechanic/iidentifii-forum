import PostsContainer from './PostsContainer';

/***** Components *****/

/** Default component: the forum landing page. */
function Home() {
  return (
    <div className="flex flex-col gap-6">
      <Introduction />
      <PostsContainer />
    </div>
  );
}

/** What the forum is for, shown to everyone until there is an account to greet instead. */
function Introduction() {
  return (
    <section className="border-b border-line pb-5">
      <h1 className="text-xl font-semibold">
        Integration questions, answered by the people who ship them.
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
