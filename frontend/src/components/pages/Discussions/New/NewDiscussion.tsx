import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import PostForm from '@src/components/pages/Discussions/common/PostForm';
import PostService, { type ICreatePostRequest } from '@src/domains/posts/PostService';
import Paths from '@src/domains/common/constants/Paths';
import { HttpError } from '@src/infra/http';

/***** Components *****/

/** Default component: starts a discussion and opens it once it exists. */
function NewDiscussion() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const create = useMutation({
    mutationFn: (request: ICreatePostRequest) => PostService.create(request),
    onSuccess: async (post) => {
      // The list is now out of date, and the reader is about to look at the thing they wrote.
      await queryClient.invalidateQueries({ queryKey: ['posts'] });
      await navigate(Paths.discussion(post.id));
    },
  });

  return (
    <section className="mx-auto flex w-full max-w-2xl flex-col gap-4 py-6">
      <header>
        <h1 className="text-lg font-semibold">Start a discussion</h1>
        <p className="mt-1 text-sm text-muted">
          Everyone can read this. Say what you tried and what happened.
        </p>
      </header>

      <PostForm
        errorMessage={create.isError ? _describeError(create.error) : undefined}
        isSaving={create.isPending}
        onCancel={() => void navigate(Paths.Home)}
        onSubmit={(title, body) => create.mutate({ title, body })}
        pendingLabel="Posting…"
        submitLabel="Post discussion"
      />
    </section>
  );
}

/***** Functions *****/

/** A refused field deserves the API's own words; anything else gets a plain sentence. */
function _describeError(error: unknown): string {
  if (error instanceof HttpError) {
    const firstField = Object.values(error.fieldErrors)[0]?.[0];
    return firstField ?? error.message;
  }

  return 'Could not post the discussion. Try again.';
}

/***** Export default *****/

export default NewDiscussion;
