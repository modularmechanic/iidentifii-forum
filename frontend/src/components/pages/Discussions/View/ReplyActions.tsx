import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import CommentService from '@src/domains/comments/CommentService';
import type { IComment } from '@src/domains/comments/Comment';
import UserOps from '@src/domains/users/UserOps';
import { useAuth } from '@src/infra/auth/useAuth';

/***** Types *****/

interface IProps {
  reply: IComment;
}

/***** Components *****/

/**
 * Default component: what the author of a reply may do to it. Editing happens in place, because
 * a reply is short and moving to another page to change one sentence is a lot of ceremony.
 */
function ReplyActions(props: IProps) {
  const { reply } = props;

  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState<string | null>(null);
  const [isConfirming, setIsConfirming] = useState(false);

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ['comments', reply.postId] });
    await queryClient.invalidateQueries({ queryKey: ['post', reply.postId] });
    await queryClient.invalidateQueries({ queryKey: ['posts'] });
  };

  const save = useMutation({
    mutationFn: (body: string) => CommentService.update(reply.id, body),
    onSuccess: async () => {
      setDraft(null);
      await refresh();
    },
  });

  const remove = useMutation({
    mutationFn: () => CommentService.remove(reply.id),
    onSuccess: refresh,
  });

  if (!UserOps.isOwner(user, reply.author.id)) {
    return null;
  }

  if (draft !== null) {
    return (
      <form
        className="mt-2 flex flex-col gap-2"
        onSubmit={(event) => {
          event.preventDefault();
          save.mutate(draft.trim());
        }}
      >
        <label className="sr-only" htmlFor={`edit-${reply.id}`}>
          Edit your reply
        </label>
        <textarea
          className="min-h-24 rounded-sm border border-line bg-surface px-2 py-1.5 text-sm text-ink"
          id={`edit-${reply.id}`}
          maxLength={2_000}
          onChange={(event) => setDraft(event.target.value)}
          value={draft}
        />
        <div className="flex items-center gap-2">
          <button
            className="rounded-sm bg-ink px-3 py-1.5 text-sm font-medium text-surface disabled:opacity-60"
            disabled={save.isPending || draft.trim() === ''}
            type="submit"
          >
            {save.isPending ? 'Saving…' : 'Save'}
          </button>
          <button
            className="rounded-sm border border-line px-3 py-1.5 text-sm"
            onClick={() => setDraft(null)}
            type="button"
          >
            Cancel
          </button>
          {save.isError && <span className="text-xs text-danger">Could not save that.</span>}
        </div>
      </form>
    );
  }

  return (
    <div className="mt-2 flex items-center gap-2 text-xs">
      <button
        className="text-muted underline underline-offset-2 hover:text-ink"
        onClick={() => setDraft(reply.body)}
        type="button"
      >
        Edit
      </button>

      {isConfirming ? (
        <>
          <span className="text-muted">Delete this reply?</span>
          <button
            className="text-danger underline underline-offset-2 disabled:opacity-60"
            disabled={remove.isPending}
            onClick={() => remove.mutate()}
            type="button"
          >
            Yes
          </button>
          <button
            className="text-muted underline underline-offset-2"
            onClick={() => setIsConfirming(false)}
            type="button"
          >
            Keep it
          </button>
        </>
      ) : (
        <button
          className="text-muted underline underline-offset-2 hover:text-danger"
          onClick={() => setIsConfirming(true)}
          type="button"
        >
          Delete
        </button>
      )}
    </div>
  );
}

/***** Export default *****/

export default ReplyActions;
