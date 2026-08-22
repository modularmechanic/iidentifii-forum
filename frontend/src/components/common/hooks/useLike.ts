import { useMutation, useQueryClient } from '@tanstack/react-query';
import PostService from '@src/domains/posts/PostService';
import type { IPost } from '@src/domains/posts/Post';
import { useAuth } from '@src/infra/auth/useAuth';

/***** Types *****/

interface ILikeControl {
  /** Why the control cannot be used, or undefined when it can. */
  disabledReason?: string;
  isPending: boolean;
  toggle: () => void;
}

/***** Functions *****/

/**
 * The like control's behaviour, shared by the list and the discussion itself so the two cannot
 * disagree about when liking is allowed. The rules are the API's; they are repeated here only to
 * explain the disabled control, never to decide the outcome.
 */
export function useLike(post: IPost): ILikeControl {
  const { user, isSignedIn } = useAuth();
  const queryClient = useQueryClient();

  const toggle = useMutation({
    mutationFn: () => (post.likedByMe ? PostService.unlike(post.id) : PostService.like(post.id)),
    onSettled: async () => {
      // Refetch rather than adjust the count by one: the true number may have moved for other
      // reasons while this reader was looking at it.
      await queryClient.invalidateQueries({ queryKey: ['posts'] });
      await queryClient.invalidateQueries({ queryKey: ['post', post.id] });
    },
  });

  return {
    disabledReason: _describeWhyNot(isSignedIn, user?.id === post.author.id),
    isPending: toggle.isPending,
    toggle: () => toggle.mutate(),
  };
}

/** Says why the control is inert, in the reader's terms. */
function _describeWhyNot(isSignedIn: boolean, isOwn: boolean): string | undefined {
  if (!isSignedIn) {
    return 'Log in to like.';
  }

  if (isOwn) {
    return 'You cannot like your own discussion.';
  }

  return undefined;
}
