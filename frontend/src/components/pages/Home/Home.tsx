import HomeContainer from './HomeContainer';

/***** Components *****/

/** Default component: the forum landing page. */
function Home() {
  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-xl font-semibold">Discussions</h1>
        <p className="mt-1 text-sm text-muted">
          Ask questions, share integration knowledge, and flag anything misleading.
        </p>
      </div>
      <div className="rounded border border-line p-4">
        <HomeContainer />
      </div>
    </section>
  );
}

/***** Export default *****/

export default Home;
