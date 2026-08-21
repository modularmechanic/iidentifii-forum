import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import Pagination from '@src/components/common/ui/md/Pagination';
import CommentService from '@src/domains/comments/CommentService';
import RepliesList from './RepliesList';

/***** Constants *****/

const PAGE_SIZE = 10;

/***** Types *****/

interface IProps {
  postId: string;
}

/***** Components *****/

/** Default component: fetches a page of replies for one discussion. */
function RepliesContainer(props: IProps) {
  const { postId } = props;

  const [page, setPage] = useState(1);

  const query = useQuery({
    queryKey: ['comments', postId, { page, pageSize: PAGE_SIZE }],
    queryFn: () => CommentService.fetchPage(postId, page, PAGE_SIZE),
    placeholderData: keepPreviousData,
  });

  if (query.isPending) {
    return <Spinner label="Loading replies" />;
  }

  if (query.isError) {
    return <ErrorMessage message="Could not load replies." onRetry={() => void query.refetch()} />;
  }

  const result = query.data;

  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-medium">
        {result.totalCount === 1 ? '1 reply' : `${result.totalCount} replies`}
      </h2>
      <RepliesList replies={result.items} />
      <Pagination
        hasNext={result.hasNext}
        hasPrevious={result.hasPrevious}
        onChange={setPage}
        page={result.page}
        totalPages={result.totalPages}
      />
    </section>
  );
}

/***** Export default *****/

export default RepliesContainer;
