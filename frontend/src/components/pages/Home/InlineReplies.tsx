import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import RepliesList from '@src/components/pages/Discussions/View/RepliesList';
import ReplyComposer from '@src/components/pages/Discussions/View/ReplyComposer';
import CommentService from '@src/domains/comments/CommentService';
import { SortOrders } from '@src/domains/posts/Post';
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
 *
 * A member can answer from here. Somebody who has read the thread has the answer in mind now, and
 * making them open the discussion first is a good way to lose it.
 */
function InlineReplies(props: IProps) {
  const { postId, totalCount } = props;

  // Newest first here, unlike the discussion itself, which reads in conversation order. A reader
  // scanning the list wants the latest word, and a reply written here then appears where it was
  // written instead of on a later page of the preview.
  const query = useQuery({
    queryKey: ['comments', postId, { page: 1, pageSize: PREVIEW_SIZE, order: 'newest' }],
    queryFn: () => CommentService.fetchPage(postId, 1, PREVIEW_SIZE, SortOrders.Descending),
  });

  if (query.isPending) {
    return <Spinner label="Loading replies" />;
  }

  if (query.isError) {
    return <ErrorMessage message="Could not load replies." onRetry={() => void query.refetch()} />;
  }

  return (
    <div className="mt-3 flex flex-col gap-2">
      <p className="text-xs font-medium tracking-wide text-muted uppercase">
        {totalCount > PREVIEW_SIZE ? `Latest ${PREVIEW_SIZE} replies` : 'Replies'}
      </p>
      <RepliesList replies={query.data.items} />
      {totalCount > PREVIEW_SIZE && (
        <Link className="text-sm text-accent underline" to={Paths.discussion(postId)}>
          See all {totalCount} replies
        </Link>
      )}
      <ReplyComposer postId={postId} />
    </div>
  );
}

/***** Export default *****/

export default InlineReplies;
