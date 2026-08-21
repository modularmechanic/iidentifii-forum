import { PostSorts, SortOrders, type PostSort, type SortOrder } from '@src/domains/posts/Post';

/***** Constants *****/

const OPTIONS = [
  { label: 'Latest', sort: PostSorts.CreatedAt, order: SortOrders.Descending },
  { label: 'Top', sort: PostSorts.LikeCount, order: SortOrders.Descending },
  { label: 'Oldest', sort: PostSorts.CreatedAt, order: SortOrders.Ascending },
] as const;

/***** Types *****/

interface IProps {
  sort: PostSort;
  order: SortOrder;
  onChange: (sort: PostSort, order: SortOrder) => void;
}

/***** Components *****/

/**
 * Default component: the three orderings people actually want, named rather than described.
 * A group of buttons rather than tabs, because choosing one reorders the list in place instead
 * of revealing a different panel.
 */
function SortTabs(props: IProps) {
  const { sort, order, onChange } = props;

  return (
    <div aria-label="Sort discussions" className="flex" role="group">
      {OPTIONS.map((option) => {
        const isSelected = option.sort === sort && option.order === order;

        return (
          <button
            aria-pressed={isSelected}
            className={`-mb-px border-b-2 px-3 py-2 text-sm ${
              isSelected ? 'border-ink font-medium' : 'border-transparent text-muted hover:text-ink'
            }`}
            key={option.label}
            onClick={() => onChange(option.sort, option.order)}
            type="button"
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}

/***** Export default *****/

export default SortTabs;
