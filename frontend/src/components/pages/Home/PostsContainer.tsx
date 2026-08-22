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
import { HttpError } from '@src/infra/http';
import PostsList from './PostsList';

/***** Constants *****/

const PAGE_SIZE = 10;

/** Said the same way wherever the list fails, so one failure does not read as two. */
const FAILED_TO_LOAD = 'Could not load the discussions.';

/***** Components *****/

/** Default component: fetches the discussions the address asks for and hands them to the list. */
function PostsContainer() {
  const { criteria, update, clear, hasFilters } = useListCriteria();

  const [isFiltering, setIsFiltering] = useState(hasFilters);
  const [hadFilters, setHadFilters] = useState(hasFilters);

  // Criteria can arrive without the switch being touched: a shared link, the back button, a
  // change of address. The panel opens when they do, because a narrowed list behind a hidden
  // control is how people lose track of what they are looking at. Only the moment filters
  // appear opens it: filters going away, whether by Clear or by the switch, leaves the panel
  // as the reader left it.
  if (hasFilters !== hadFilters) {
    setHadFilters(hasFilters);

    if (hasFilters) {
      setIsFiltering(true);
    }
  }

  const query = useQuery({
    queryKey: ['posts', { ...criteria, pageSize: PAGE_SIZE }],
    queryFn: () => PostService.fetchPage({ ...criteria, pageSize: PAGE_SIZE }),
    // Keeping the previous page on screen avoids a flash of nothing while the next one loads.
    placeholderData: keepPreviousData,
  });

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end justify-between gap-x-3 gap-y-1 border-b border-line">
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
        hasFilters={hasFilters}
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
  hasFilters: boolean;
  result?: Awaited<ReturnType<typeof PostService.fetchPage>>;
  onRetry: () => void;
  onPage: (page: number) => void;
}) {
  const { isPending, errorMessage, hasFilters, result, onRetry, onPage } = props;

  if (isPending) {
    return (
      <div className="rounded-sm border border-line p-4">
        <Spinner label="Loading discussions" />
      </div>
    );
  }

  if (errorMessage !== undefined || result === undefined) {
    return <ErrorMessage message={errorMessage ?? FAILED_TO_LOAD} onRetry={onRetry} />;
  }

  return (
    <>
      {/* Sorting and filtering change the list without moving the reader, so the new size is
          announced rather than only redrawn. */}
      <p aria-live="polite" className="font-mono text-xs text-muted tabular-nums">
        {result.totalCount} {result.totalCount === 1 ? 'discussion' : 'discussions'}
      </p>
      <PostsList hasFilters={hasFilters} posts={result.items} />
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

/** A rejected filter deserves the API's own words; anything else gets a plain sentence. */
function _describeError(error: unknown): string {
  return error instanceof HttpError && error.message ? error.message : FAILED_TO_LOAD;
}

/***** Export default *****/

export default PostsContainer;
