import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import Spinner from '@src/components/common/ui/sm/Spinner';
import PostForm from '@src/components/pages/Discussions/common/PostForm';
import PostService, { type ICreatePostRequest } from '@src/domains/posts/PostService';
import Paths from '@src/domains/common/constants/Paths';
import UserOps from '@src/domains/users/UserOps';
import { useAuth } from '@src/infra/auth/useAuth';
import { HttpError } from '@src/infra/http';

/***** Components *****/

/** Default component: corrects a discussion, reusing the composer that wrote it. */
function EditDiscussion() {
  const { id = '' } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['post', id],
    queryFn: () => PostService.fetchById(id),
  });

  const save = useMutation({
    mutationFn: (request: ICreatePostRequest) => PostService.update(id, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['post', id] });
      await queryClient.invalidateQueries({ queryKey: ['posts'] });
      await navigate(Paths.discussion(id));
    },
  });

  if (query.isPending) {
    return <Spinner label="Loading discussion" />;
  }

  if (query.isError) {
    return (
      <ErrorMessage
        message="Could not load this discussion."
        onRetry={() => void query.refetch()}
      />
    );
  }

  // The API refuses this anyway; saying so here spares the reader a form that cannot be saved.
  if (!UserOps.isOwner(user, query.data.author.id)) {
    return (
      <section className="py-12 text-center">
        <h1 className="text-lg font-semibold">This is not yours to edit</h1>
        <p className="mt-1 text-sm text-muted">Only the author can change a discussion.</p>
      </section>
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-2xl flex-col gap-4 py-6">
      <header>
        <h1 className="text-lg font-semibold">Edit discussion</h1>
        <p className="mt-1 text-sm text-muted">Readers will see that it was edited.</p>
      </header>

      <PostForm
        errorMessage={save.isError ? _describeError(save.error) : undefined}
        initialBody={query.data.body}
        initialTitle={query.data.title}
        isSaving={save.isPending}
        onCancel={() => void navigate(Paths.discussion(id))}
        onSubmit={(title, body) => save.mutate({ title, body })}
        pendingLabel="Saving…"
        submitLabel="Save changes"
      />
    </section>
  );
}

/***** Functions *****/

/** A refused field deserves the API's own words; anything else gets a plain sentence. */
function _describeError(error: unknown): string {
  if (error instanceof HttpError) {
    const firstField = Object.values(error.fieldErrors)[0]?.[0];
    const described = firstField ?? error.message;

    return described.trim() === '' ? 'Could not save the changes. Try again.' : described;
  }

  return 'Could not save the changes. Try again.';
}

/***** Export default *****/

export default EditDiscussion;
