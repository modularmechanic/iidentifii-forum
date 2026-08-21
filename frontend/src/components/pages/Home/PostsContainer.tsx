import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import Pagination from '@src/components/common/ui/md/Pagination';
import PostService from '@src/domains/posts/PostService';
import PostsList from './PostsList';

/***** Constants *****/

const PAGE_SIZE = 10;

/***** Components *****/

/** Default component: fetches a page of discussions and hands it to the list. */
function PostsContainer() {
  const [page, setPage] = useState(1);

  const query = useQuery({
    queryKey: ['posts', { page, pageSize: PAGE_SIZE }],
    queryFn: () => PostService.fetchPage({ page, pageSize: PAGE_SIZE }),
    // Keeping the previous page on screen avoids a flash of nothing while the next one loads.
    placeholderData: keepPreviousData,
  });

  if (query.isPending) {
    return (
      <div className="rounded-sm border border-line p-4">
        <Spinner label="Loading discussions" />
      </div>
    );
  }

  if (query.isError) {
    return (
      <ErrorMessage
        message="Could not load discussions. Is the API running?"
        onRetry={() => void query.refetch()}
      />
    );
  }

  const result = query.data;

  return (
    <div className="flex flex-col gap-1">
      <div className="flex items-baseline justify-between pb-2">
        <h2 className="font-medium">Latest</h2>
        <p className="font-mono text-xs text-muted tabular-nums">
          {result.totalCount} {result.totalCount === 1 ? 'discussion' : 'discussions'}
        </p>
      </div>
      <PostsList posts={result.items} />
      <Pagination
        hasNext={result.hasNext}
        hasPrevious={result.hasPrevious}
        onChange={setPage}
        page={result.page}
        totalPages={result.totalPages}
      />
    </div>
  );
}

/***** Export default *****/

export default PostsContainer;
