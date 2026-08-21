import type { ReactNode } from 'react';
import { ModerationTagLabels, ModerationTags } from '@src/domains/posts/Post';
import type { IListCriteria } from '@src/components/common/hooks/useListCriteria';

/***** Constants *****/

const FIELD_CLASS =
  'rounded-sm border border-line bg-surface px-2 py-1.5 text-sm font-normal text-ink';

/***** Types *****/

interface IProps {
  criteria: IListCriteria;
  hasFilters: boolean;
  onApply: (changes: Partial<IListCriteria>) => void;
  onClear: () => void;
}

/***** Components *****/

/**
 * Default component: narrows the list by date, author or moderation flag. The fields are applied
 * together, so choosing three of them costs one request rather than three.
 */
function FilterPanel(props: IProps) {
  const { criteria, hasFilters, onApply, onClear } = props;

  return (
    <form
      className="grid gap-3 rounded-sm border border-line bg-subtle p-3 sm:grid-cols-2 lg:grid-cols-[repeat(4,minmax(0,1fr))_auto]"
      onSubmit={(event) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        onApply({
          from: String(data.get('from') ?? '') || undefined,
          to: String(data.get('to') ?? '') || undefined,
          author: String(data.get('author') ?? '').trim() || undefined,
          tag: (String(data.get('tag') ?? '') || undefined) as IListCriteria['tag'],
        });
      }}
    >
      <Field label="From">
        <input className={FIELD_CLASS} defaultValue={criteria.from ?? ''} name="from" type="date" />
      </Field>
      <Field label="To">
        <input className={FIELD_CLASS} defaultValue={criteria.to ?? ''} name="to" type="date" />
      </Field>
      <Field label="Author">
        <input
          className={FIELD_CLASS}
          defaultValue={criteria.author ?? ''}
          maxLength={32}
          name="author"
          placeholder="username"
          type="text"
        />
      </Field>
      <Field label="Flagged">
        <select className={FIELD_CLASS} defaultValue={criteria.tag ?? ''} name="tag">
          <option value="">Any</option>
          <option value={ModerationTags.MisleadingOrFalse}>
            {ModerationTagLabels[ModerationTags.MisleadingOrFalse]}
          </option>
        </select>
      </Field>

      <div className="flex items-end gap-2">
        <button
          className="rounded-sm bg-ink px-3 py-1.5 text-sm font-medium text-canvas"
          type="submit"
        >
          Apply
        </button>
        <button
          className="rounded-sm border border-line px-3 py-1.5 text-sm disabled:opacity-40"
          disabled={!hasFilters}
          onClick={onClear}
          type="button"
        >
          Clear
        </button>
      </div>
    </form>
  );
}

/** One labelled control. */
function Field(props: { label: string; children: ReactNode }) {
  const { label, children } = props;

  return (
    <label className="flex flex-col gap-1 text-xs font-medium">
      {label}
      {children}
    </label>
  );
}

/***** Export default *****/

export default FilterPanel;
