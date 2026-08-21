import { getShortDate } from '@src/common/utils/format-date';
import { ModerationTagLabels, type IModerationTag } from '@src/domains/posts/Post';

/***** Types *****/

interface IProps {
  tags: IModerationTag[];
}

/***** Components *****/

/**
 * Default component: says a moderator has marked this discussion. It is deliberately prominent:
 * the flag exists for regulatory reasons, so a reader should not be able to miss it.
 */
function FlagBanner(props: IProps) {
  const { tags } = props;

  if (tags.length === 0) {
    return null;
  }

  return (
    <div className="flex flex-col gap-2">
      {tags.map((tag) => (
        <div
          className="flex gap-3 rounded-sm border border-danger border-l-4 p-3 text-sm"
          key={tag.tag}
          role="note"
        >
          <svg
            aria-hidden="true"
            className="mt-0.5 size-4 shrink-0 fill-danger"
            viewBox="0 0 24 24"
          >
            <path d="M12 2 1 21h22L12 2zm1 14h-2v2h2v-2zm0-7h-2v5h2V9z" />
          </svg>
          <p>
            <span className="font-medium text-danger">
              Flagged as {ModerationTagLabels[tag.tag].toLowerCase()}
            </span>
            {' — a moderator has marked this discussion; treat its claims with care.'}
            <span className="mt-1 block text-xs text-muted">
              Flagged by {tag.taggedByUsername} on {getShortDate(tag.createdAt)}
            </span>
          </p>
        </div>
      ))}
    </div>
  );
}

/***** Export default *****/

export default FlagBanner;
