import { useState } from 'react';
import Input from '@src/components/common/ui/sm/Input';
import Banner from '@src/components/common/ui/sm/Banner';

/***** Constants *****/

/** The same limits the API enforces, so a refusal is met here rather than after a round trip. */
const TITLE_MAX = 200;
const BODY_MAX = 10_000;

/***** Types *****/

interface IProps {
  initialTitle?: string;
  initialBody?: string;
  submitLabel: string;
  /** What the submit button says while the request is out. */
  pendingLabel: string;
  isSaving: boolean;
  errorMessage?: string;
  onSubmit: (title: string, body: string) => void;
  onCancel: () => void;
}

/***** Components *****/

/**
 * Default component: writes a discussion. Shared by starting one and editing one, so the two
 * cannot disagree about what a discussion may contain.
 */
function PostForm(props: IProps) {
  const {
    initialTitle = '',
    initialBody = '',
    submitLabel,
    pendingLabel,
    isSaving,
    errorMessage,
    onSubmit,
    onCancel,
  } = props;

  const [title, setTitle] = useState(initialTitle);
  const [body, setBody] = useState(initialBody);

  const isEmpty = title.trim() === '' || body.trim() === '';

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(title.trim(), body.trim());
      }}
    >
      {errorMessage !== undefined && <Banner tone="error">{errorMessage}</Banner>}

      <Input
        hint={`${title.length} of ${TITLE_MAX} characters`}
        label="Title"
        maxLength={TITLE_MAX}
        name="title"
        onChange={(event) => setTitle(event.target.value)}
        placeholder="What is the question?"
        required
        value={title}
      />

      <div className="flex flex-col gap-1">
        <label className="text-sm font-medium" htmlFor="body">
          Body
        </label>
        <textarea
          className="min-h-48 rounded-sm border border-line bg-surface px-2 py-1.5 text-sm text-ink"
          id="body"
          maxLength={BODY_MAX}
          name="body"
          onChange={(event) => setBody(event.target.value)}
          placeholder="Give enough detail that somebody can answer without asking you first."
          required
          value={body}
        />
        <p className="text-xs text-muted">
          {body.length} of {BODY_MAX} characters
        </p>
      </div>

      <div className="flex items-center gap-2">
        <button
          className="rounded-sm bg-ink px-3 py-2 text-sm font-medium text-surface disabled:opacity-60"
          disabled={isSaving || isEmpty}
          type="submit"
        >
          {isSaving ? pendingLabel : submitLabel}
        </button>
        <button
          className="rounded-sm border border-line px-3 py-2 text-sm disabled:opacity-60"
          disabled={isSaving}
          onClick={onCancel}
          type="button"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}

/***** Export default *****/

export default PostForm;
