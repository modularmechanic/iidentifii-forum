import { useEffect, useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import Banner from '@src/components/common/ui/sm/Banner';
import PostService from '@src/domains/posts/PostService';
import type { IPost } from '@src/domains/posts/Post';
import Paths from '@src/domains/common/constants/Paths';
import UserOps from '@src/domains/users/UserOps';
import { useAuth } from '@src/infra/auth/useAuth';

/***** Types *****/

interface IProps {
  post: IPost;
}

/***** Components *****/

/**
 * Default component: what an author may do to their own discussion. Deleting asks first, because
 * it takes the replies with it and cannot be undone.
 */
function OwnerActions(props: IProps) {
  const { post } = props;

  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [isConfirming, setIsConfirming] = useState(false);

  // Asking the question replaces the button that was pressed. Answering "Keep it" replaces it
  // back, and without this the focus would be left on nothing at all.
  const deleteButton = useRef<HTMLButtonElement>(null);

  // A ref rather than state: nothing renders differently because of it, and it only needs to
  // survive until the effect below reads it.
  const isReturningFocus = useRef(false);

  // After the commit, not during it: the button being focused is only in the document once the
  // question has been replaced by it.
  useEffect(() => {
    if (!isConfirming && isReturningFocus.current) {
      isReturningFocus.current = false;
      deleteButton.current?.focus();
    }
  }, [isConfirming]);

  const remove = useMutation({
    mutationFn: () => PostService.remove(post.id),
    onSuccess: async () => {
      // The list is refetched, but this discussion and its replies are thrown away rather than
      // refetched: nothing is there to fetch any more, and a cached copy would be handed straight
      // back to anyone who pressed the back button.
      queryClient.removeQueries({ queryKey: ['post', post.id] });
      queryClient.removeQueries({ queryKey: ['comments', post.id] });

      await queryClient.invalidateQueries({ queryKey: ['posts'] });
      await navigate(Paths.Home);
    },
  });

  if (!UserOps.isOwner(user, post.author.id)) {
    return null;
  }

  return (
    <div className="flex flex-col gap-2">
      {remove.isError && <Banner tone="error">Could not delete the discussion. Try again.</Banner>}

      <div className="flex flex-wrap items-center gap-2">
        <Link
          className="rounded-sm border border-line px-3 py-1.5 text-sm"
          to={Paths.editDiscussion(post.id)}
        >
          Edit
        </Link>

        {isConfirming ? (
          <>
            <span className="text-sm text-muted" role="alert">
              Delete this and its replies?
            </span>
            <button
              // The button that was under the cursor has just been replaced, so the answer takes
              // the focus rather than dropping it at the top of the page.
              autoFocus
              className="rounded-sm border border-danger px-3 py-1.5 text-sm text-danger disabled:opacity-60"
              disabled={remove.isPending}
              onClick={() => remove.mutate()}
              type="button"
            >
              {remove.isPending ? 'Deleting…' : 'Yes, delete'}
            </button>
            <button
              className="rounded-sm border border-line px-3 py-1.5 text-sm"
              onClick={() => {
                isReturningFocus.current = true;
                setIsConfirming(false);
              }}
              type="button"
            >
              Keep it
            </button>
          </>
        ) : (
          <button
            className="rounded-sm border border-line px-3 py-1.5 text-sm text-danger"
            onClick={() => setIsConfirming(true)}
            ref={deleteButton}
            type="button"
          >
            Delete
          </button>
        )}
      </div>
    </div>
  );
}

/***** Export default *****/

export default OwnerActions;
