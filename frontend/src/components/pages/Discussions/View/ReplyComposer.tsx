import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router';
import Banner from '@src/components/common/ui/sm/Banner';
import CommentService from '@src/domains/comments/CommentService';
import Paths from '@src/domains/common/constants/Paths';
import { useAuth } from '@src/infra/auth/useAuth';

/***** Constants *****/

const BODY_MAX = 2_000;

/***** Types *****/

interface IProps {
  postId: string;
}

/***** Components *****/

/**
 * Default component: adds a reply. An anonymous reader sees why they cannot, rather than a
 * control that does nothing when pressed.
 */
function ReplyComposer(props: IProps) {
  const { postId } = props;

  const { isSignedIn } = useAuth();
  const queryClient = useQueryClient();
  const [body, setBody] = useState('');

  const reply = useMutation({
    mutationFn: (text: string) => CommentService.create(postId, text),
    onSuccess: async () => {
      setBody('');
      // The replies moved, and so did the reply count the discussion carries — which is shown
      // both on the discussion itself and on its row in the list.
      await queryClient.invalidateQueries({ queryKey: ['comments', postId] });
      await queryClient.invalidateQueries({ queryKey: ['post', postId] });
      await queryClient.invalidateQueries({ queryKey: ['posts'] });
    },
  });

  if (!isSignedIn) {
    return (
      <p className="rounded-sm border border-line p-4 text-sm text-muted">
        <Link className="text-accent underline" to={Paths.Login}>
          Log in
        </Link>{' '}
        to reply. Reading needs no account.
      </p>
    );
  }

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault();
        reply.mutate(body.trim());
      }}
    >
      <label className="text-sm font-medium" htmlFor="reply">
        Your reply
      </label>
      <textarea
        className="min-h-28 rounded-sm border border-line bg-surface px-2 py-1.5 text-sm text-ink"
        id="reply"
        maxLength={BODY_MAX}
        name="reply"
        onChange={(event) => setBody(event.target.value)}
        placeholder="Answer the question, or add what you found."
        value={body}
      />

      {reply.isError && <Banner tone="error">Could not post the reply. Try again.</Banner>}

      <div className="flex items-center gap-3">
        <button
          className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-surface disabled:opacity-60"
          disabled={reply.isPending || body.trim() === ''}
          type="submit"
        >
          {reply.isPending ? 'Posting…' : 'Post reply'}
        </button>
        <span className="text-xs text-muted">
          {body.length} of {BODY_MAX} characters
        </span>
      </div>
    </form>
  );
}

/***** Export default *****/

export default ReplyComposer;
