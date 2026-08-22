import Avatar from '@src/components/common/ui/sm/Avatar';
import Empty from '@src/components/common/ui/sm/Empty';
import { getShortDate } from '@src/common/utils/format-date';
import type { IComment } from '@src/domains/comments/Comment';
import ReplyActions from './ReplyActions';

/***** Types *****/

interface IProps {
  replies: IComment[];
}

interface IReplyProps {
  reply: IComment;
}

/***** Components *****/

/** Default component: the replies on this page, oldest first. */
function RepliesList(props: IProps) {
  const { replies } = props;

  if (replies.length === 0) {
    return (
      <div className="rounded-sm border border-line">
        <Empty title="No replies yet">Be the first to answer.</Empty>
      </div>
    );
  }

  return (
    <ul className="divide-y divide-line rounded-sm border border-line">
      {replies.map((reply) => (
        <Reply key={reply.id} reply={reply} />
      ))}
    </ul>
  );
}

/** One reply, with who wrote it and when. */
function Reply(props: IReplyProps) {
  const { reply } = props;

  return (
    <li className="flex gap-3 p-4">
      <Avatar username={reply.author.username} />
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2 text-xs text-muted">
          <span className="font-medium text-ink">{reply.author.username}</span>
          <time dateTime={reply.createdAt}>{getShortDate(reply.createdAt)}</time>
          {reply.updatedAt && <span>edited</span>}
        </div>
        <p className="mt-1 text-sm whitespace-pre-line">{reply.body}</p>
        <ReplyActions reply={reply} />
      </div>
    </li>
  );
}

/***** Export default *****/

export default RepliesList;
