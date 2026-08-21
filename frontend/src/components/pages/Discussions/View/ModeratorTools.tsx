import { useMutation, useQueryClient } from '@tanstack/react-query';
import Banner from '@src/components/common/ui/sm/Banner';
import PostService from '@src/domains/posts/PostService';
import { ModerationTags, type IPost } from '@src/domains/posts/Post';
import UserOps from '@src/domains/users/UserOps';
import { useAuth } from '@src/infra/auth/useAuth';

/***** Types *****/

interface IProps {
  post: IPost;
}

/***** Components *****/

/**
 * Default component: a moderator's controls. Hiding them from everybody else is a courtesy, not
 * the rule: the API refuses a member who calls the endpoint directly.
 */
function ModeratorTools(props: IProps) {
  const { post } = props;

  const { user } = useAuth();
  const queryClient = useQueryClient();

  const isFlagged = post.tags.some((tag) => tag.tag === ModerationTags.MisleadingOrFalse);

  const change = useMutation({
    mutationFn: () =>
      isFlagged
        ? PostService.unflag(post.id, ModerationTags.MisleadingOrFalse)
        : PostService.flag(post.id, ModerationTags.MisleadingOrFalse),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['post', post.id] });
      await queryClient.invalidateQueries({ queryKey: ['posts'] });
    },
  });

  if (!UserOps.isModerator(user)) {
    return null;
  }

  return (
    <div className="flex flex-col gap-2 rounded-sm border border-line border-dashed p-3">
      <p className="text-xs font-medium tracking-wide text-muted uppercase">Moderator</p>

      {change.isError && <Banner tone="error">Could not change the flag. Try again.</Banner>}

      <div className="flex items-center gap-3">
        <button
          className="rounded-sm border border-line px-3 py-1.5 text-sm disabled:opacity-60"
          disabled={change.isPending}
          onClick={() => change.mutate()}
          type="button"
        >
          {isFlagged ? 'Remove the flag' : 'Flag as misleading or false'}
        </button>
        <p className="text-xs text-muted">
          {isFlagged
            ? 'Readers are being warned about this discussion.'
            : 'Marks the discussion so readers treat its claims with care.'}
        </p>
      </div>
    </div>
  );
}

/***** Export default *****/

export default ModeratorTools;
