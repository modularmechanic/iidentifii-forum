import { useState } from 'react';
import { ModerationTagLabels, ModerationTags } from '@src/domains/posts/Post';
import type { IListCriteria } from '@src/components/common/hooks/useListCriteria';

/***** Types *****/

interface IProps {
  criteria: IListCriteria;
  hasFilters: boolean;
  onApply: (changes: Partial<IListCriteria>) => void;
  onClear: () => void;
}

/***** Components *****/

/** Default component: narrows the list by date, author or moderation flag. */
function FilterBar(props: IProps) {
  const { criteria, hasFilters, onApply, onClear } = props;

  const [isOpen, setIsOpen] = useState(hasFilters);

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center justify-end gap-2">
        <button
          aria-expanded={isOpen}
          className="rounded-sm border border-line px-3 py-1.5 text-sm"
          onClick={() => setIsOpen((open) => !open)}
          type="button"
        >
          Filters{hasFilters && <span className="ml-1 font-mono text-accent">on</span>}
        </button>
        {hasFilters && (
          <button className="text-sm text-accent underline" onClick={onClear} type="button">
            Clear
          </button>
        )}
      </div>

      {isOpen && <FilterFields criteria={criteria} onApply={onApply} />}
    </div>
  );
}

/** The fields themselves, submitted together so one change does not cost a request each. */
function FilterFields(props: { criteria: IListCriteria; onApply: IProps['onApply'] }) {
  const { criteria, onApply } = props;

  return (
    <form
      className="grid gap-3 rounded-sm border border-line bg-subtle p-3 sm:grid-cols-2 lg:grid-cols-5"
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
      <div className="flex items-end">
        <button
          className="rounded-sm bg-ink px-3 py-1.5 text-sm font-medium text-canvas"
          type="submit"
        >
          Apply
        </button>
      </div>
    </form>
  );
}

/** One labelled control. */
function Field(props: { label: string; children: React.ReactNode }) {
  return (
    <label className="flex flex-col gap-1 text-xs font-medium">
      {props.label}
      {props.children}
    </label>
  );
}

/***** Constants *****/

const FIELD_CLASS =
  'rounded-sm border border-line bg-surface px-2 py-1.5 text-sm font-normal text-ink';

/***** Export default *****/

export default FilterBar;
