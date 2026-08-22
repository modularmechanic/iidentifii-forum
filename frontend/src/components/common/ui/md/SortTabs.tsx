import { PostSorts, SortOrders, type PostSort, type SortOrder } from '@src/domains/posts/Post';

/***** Constants *****/

/** Every ordering the API serves, so no address can put the list in an order no button names. */
const OPTIONS = [
  { label: 'Latest', sort: PostSorts.CreatedAt, order: SortOrders.Descending },
  { label: 'Oldest', sort: PostSorts.CreatedAt, order: SortOrders.Ascending },
  { label: 'Top', sort: PostSorts.LikeCount, order: SortOrders.Descending },
  { label: 'Least liked', sort: PostSorts.LikeCount, order: SortOrders.Ascending },
] as const;

/***** Types *****/

interface IProps {
  sort: PostSort;
  order: SortOrder;
  onChange: (sort: PostSort, order: SortOrder) => void;
}

/***** Components *****/

/**
 * Default component: the orderings named rather than described. A group of buttons rather than
 * tabs, because choosing one reorders the list in place instead of revealing a different panel.
 *
 * Narrow screens scroll the strip rather than wrapping it: a wrapped label doubles the height of
 * the row and drags the underline away from the tab it belongs to.
 */
function SortTabs(props: IProps) {
  const { sort, order, onChange } = props;

  return (
    // Wrapping rather than scrolling. Making this a scroll container fitted the four orderings
    // into a narrow window, but on any platform drawing classic scrollbars it also drew one
    // across the row — a scrollbar for content the reader can simply be shown instead.
    <div aria-label="Sort discussions" className="flex min-w-0 flex-wrap" role="group">
      {OPTIONS.map((option) => {
        const isSelected = option.sort === sort && option.order === order;

        return (
          <button
            aria-pressed={isSelected}
            className={`-mb-px shrink-0 border-b-2 px-2 py-2 text-sm whitespace-nowrap sm:px-3 ${
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
