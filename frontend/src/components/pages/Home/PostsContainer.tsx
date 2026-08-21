import { keepPreviousData, useQuery } from '@tanstack/react-query';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import FilterBar from '@src/components/common/ui/md/FilterBar';
import Pagination from '@src/components/common/ui/md/Pagination';
import SortTabs from '@src/components/common/ui/md/SortTabs';
import { useListCriteria } from '@src/components/common/hooks/useListCriteria';
import PostService from '@src/domains/posts/PostService';
import PostsList from './PostsList';

/***** Constants *****/

const PAGE_SIZE = 10;

/***** Components *****/

/** Default component: fetches the discussions the address asks for and hands them to the list. */
function PostsContainer() {
  const { criteria, update, clear, hasFilters } = useListCriteria();

  const query = useQuery({
    queryKey: ['posts', { ...criteria, pageSize: PAGE_SIZE }],
    queryFn: () => PostService.fetchPage({ ...criteria, pageSize: PAGE_SIZE }),
    // Keeping the previous page on screen avoids a flash of nothing while the next one loads.
    placeholderData: keepPreviousData,
  });

  return (
    <div className="flex flex-col gap-3">
      <SortTabs
        onChange={(sort, order) => update({ sort, order })}
        order={criteria.order}
        sort={criteria.sort}
      />
      <FilterBar criteria={criteria} hasFilters={hasFilters} onApply={update} onClear={clear} />

      <Results
        errorMessage={query.isError ? _describeError(query.error) : undefined}
        isPending={query.isPending}
        onRetry={() => void query.refetch()}
        result={query.data}
        onPage={(page) => update({ page })}
      />
    </div>
  );
}

/** Whichever of loading, failure or content applies right now. */
function Results(props: {
  isPending: boolean;
  errorMessage?: string;
  result?: Awaited<ReturnType<typeof PostService.fetchPage>>;
  onRetry: () => void;
  onPage: (page: number) => void;
}) {
  const { isPending, errorMessage, result, onRetry, onPage } = props;

  if (isPending) {
    return (
      <div className="rounded-sm border border-line p-4">
        <Spinner label="Loading discussions" />
      </div>
    );
  }

  if (errorMessage !== undefined || result === undefined) {
    return (
      <ErrorMessage message={errorMessage ?? 'Could not load discussions.'} onRetry={onRetry} />
    );
  }

  return (
    <>
      <p className="font-mono text-xs text-muted tabular-nums">
        {result.totalCount} {result.totalCount === 1 ? 'discussion' : 'discussions'}
      </p>
      <PostsList posts={result.items} />
      <Pagination
        hasNext={result.hasNext}
        hasPrevious={result.hasPrevious}
        onChange={onPage}
        page={result.page}
        totalPages={result.totalPages}
      />
    </>
  );
}

/***** Functions *****/

/** A rejected filter deserves a different message from an unreachable API. */
function _describeError(error: unknown): string {
  return error instanceof Error && error.message
    ? error.message
    : 'Could not load discussions. Is the API running?';
}

/***** Export default *****/

export default PostsContainer;
