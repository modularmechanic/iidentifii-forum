import Avatar from '@src/components/common/ui/sm/Avatar';
import LikeCount from '@src/components/common/ui/md/LikeCount';
import { getShortDate } from '@src/common/utils/format-date';
import type { IPost } from '@src/domains/posts/Post';
import FlagBanner from './FlagBanner';

/***** Types *****/

interface IProps {
  post: IPost;
}

/***** Components *****/

/** Default component: one discussion in full, with any moderator flag above the body. */
function Discussion(props: IProps) {
  const { post } = props;

  return (
    <article className="flex flex-col gap-4 rounded-sm border border-line p-6">
      <h1 className="text-xl font-semibold">{post.title}</h1>

      <div className="flex items-center gap-3">
        <Avatar size="md" username={post.author.username} />
        <div className="text-sm">
          <p className="font-medium">{post.author.username}</p>
          <p className="text-xs text-muted">
            Posted {getShortDate(post.createdAt)}
            {post.updatedAt && ` · edited ${getShortDate(post.updatedAt)}`}
          </p>
        </div>
      </div>

      <FlagBanner tags={post.tags} />

      <div className="max-w-prose text-sm whitespace-pre-line">{post.body}</div>

      <div className="flex items-center gap-3 border-t border-line pt-4">
        <LikeCount count={post.likeCount} />
        <p className="text-sm text-muted">Log in to like or reply.</p>
      </div>
    </article>
  );
}

/***** Export default *****/

export default Discussion;
