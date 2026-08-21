import { Link, useParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import PostService from '@src/domains/posts/PostService';
import Paths from '@src/domains/common/constants/Paths';
import { HttpError } from '@src/infra/http';
import Discussion from './Discussion';
import RepliesContainer from './RepliesContainer';

/***** Components *****/

/** Default component: fetches one discussion, then renders it with its replies. */
function ViewDiscussion() {
  const { id = '' } = useParams();

  const query = useQuery({
    queryKey: ['post', id],
    queryFn: () => PostService.fetchById(id),
    retry: (failureCount, error) =>
      // A missing discussion will stay missing; only retry failures that might resolve.
      !(error instanceof HttpError && error.status === 404) && failureCount < 1,
  });

  if (query.isPending) {
    return <Spinner label="Loading discussion" />;
  }

  if (query.isError) {
    return <NotFoundOrError error={query.error} onRetry={() => void query.refetch()} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <Link className="text-sm text-muted hover:text-accent" to={Paths.Home}>
        ‹ All discussions
      </Link>
      <Discussion post={query.data} />
      {/* Keyed by discussion so moving to another one starts its replies at the first page. */}
      <RepliesContainer key={id} postId={id} />
    </div>
  );
}

/** Tells a reader the discussion is gone, rather than that something broke. */
function NotFoundOrError(props: { error: unknown; onRetry: () => void }) {
  const { error, onRetry } = props;

  if (error instanceof HttpError && error.status === 404) {
    return (
      <section className="py-12 text-center">
        <h1 className="font-medium">Discussion not found</h1>
        <p className="mt-1 text-sm text-muted">
          It may have been deleted.{' '}
          <Link className="text-accent underline" to={Paths.Home}>
            Back to all discussions
          </Link>
          .
        </p>
      </section>
    );
  }

  return <ErrorMessage message="Could not load this discussion." onRetry={onRetry} />;
}

/***** Export default *****/

export default ViewDiscussion;
