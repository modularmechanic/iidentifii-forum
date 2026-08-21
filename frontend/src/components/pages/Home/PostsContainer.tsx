import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import Switch from '@src/components/common/ui/sm/Switch';
import FilterPanel from '@src/components/common/ui/md/FilterPanel';
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

  // Filters that are already applied are shown, so a reader can see what narrowed the list.
  const [isFiltering, setIsFiltering] = useState(hasFilters);

  const query = useQuery({
    queryKey: ['posts', { ...criteria, pageSize: PAGE_SIZE }],
    queryFn: () => PostService.fetchPage({ ...criteria, pageSize: PAGE_SIZE }),
    // Keeping the previous page on screen avoids a flash of nothing while the next one loads.
    placeholderData: keepPreviousData,
  });

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-end justify-between gap-3 border-b border-line">
        <SortTabs
          onChange={(sort, order) => update({ sort, order })}
          order={criteria.order}
          sort={criteria.sort}
        />
        <Switch
          isOn={isFiltering}
          label="Filters"
          onChange={(isOn) => {
            setIsFiltering(isOn);

            // Turning the switch off removes the filters as well as the panel: leaving a
            // narrowed list behind a hidden control is how people lose track of what they see.
            if (!isOn && hasFilters) {
              clear();
            }
          }}
        />
      </div>

      {isFiltering && (
        <FilterPanel
          // Remounted when the criteria change, so the fields follow the address rather than
          // holding what was typed before Clear.
          key={`${criteria.from}|${criteria.to}|${criteria.author}|${criteria.tag}`}
          criteria={criteria}
          hasFilters={hasFilters}
          onApply={update}
          onClear={clear}
        />
      )}

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
