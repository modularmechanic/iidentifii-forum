import { useState } from 'react';
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

  const remove = useMutation({
    mutationFn: () => PostService.remove(post.id),
    onSuccess: async () => {
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

      <div className="flex items-center gap-2">
        <Link
          className="rounded-sm border border-line px-3 py-1.5 text-sm"
          to={Paths.editDiscussion(post.id)}
        >
          Edit
        </Link>

        {isConfirming ? (
          <>
            <span className="text-sm text-muted">Delete this and its replies?</span>
            <button
              className="rounded-sm border border-danger px-3 py-1.5 text-sm text-danger disabled:opacity-60"
              disabled={remove.isPending}
              onClick={() => remove.mutate()}
              type="button"
            >
              {remove.isPending ? 'Deleting…' : 'Yes, delete'}
            </button>
            <button
              className="rounded-sm border border-line px-3 py-1.5 text-sm"
              onClick={() => setIsConfirming(false)}
              type="button"
            >
              Keep it
            </button>
          </>
        ) : (
          <button
            className="rounded-sm border border-line px-3 py-1.5 text-sm text-danger"
            onClick={() => setIsConfirming(true)}
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
