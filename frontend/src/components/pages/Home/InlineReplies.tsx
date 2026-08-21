import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import RepliesList from '@src/components/pages/Discussions/View/RepliesList';
import CommentService from '@src/domains/comments/CommentService';
import Paths from '@src/domains/common/constants/Paths';

/***** Constants *****/

/** Enough to see how a discussion went without turning the list into the discussion. */
const PREVIEW_SIZE = 3;

/***** Types *****/

interface IProps {
  postId: string;
  totalCount: number;
}

/***** Components *****/

/**
 * Default component: the first few replies, fetched only once a reader asks for them. Opening a
 * row should not cost the twenty requests that loading every row's replies up front would.
 */
function InlineReplies(props: IProps) {
  const { postId, totalCount } = props;

  const query = useQuery({
    queryKey: ['comments', postId, { page: 1, pageSize: PREVIEW_SIZE }],
    queryFn: () => CommentService.fetchPage(postId, 1, PREVIEW_SIZE),
  });

  if (query.isPending) {
    return <Spinner label="Loading replies" />;
  }

  if (query.isError) {
    return <ErrorMessage message="Could not load replies." onRetry={() => void query.refetch()} />;
  }

  return (
    <div className="mt-3 flex flex-col gap-2">
      <RepliesList replies={query.data.items} />
      {totalCount > PREVIEW_SIZE && (
        <Link className="text-sm text-accent underline" to={Paths.discussion(postId)}>
          See all {totalCount} replies
        </Link>
      )}
    </div>
  );
}

/***** Export default *****/

export default InlineReplies;
