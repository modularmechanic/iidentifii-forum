import Avatar from '@src/components/common/ui/sm/Avatar';
import LikeButton from '@src/components/common/ui/md/LikeButton';
import { useLike } from '@src/components/common/hooks/useLike';
import { getShortDate } from '@src/common/utils/format-date';
import type { IPost } from '@src/domains/posts/Post';
import FlagBanner from './FlagBanner';
import ReplyComposer from './ReplyComposer';

/***** Types *****/

interface IProps {
  post: IPost;
}

/***** Components *****/

/** Default component: one discussion in full, with any moderator flag above the body. */
function Discussion(props: IProps) {
  const { post } = props;

  const like = useLike(post);

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

      <div className="flex flex-col gap-4 border-t border-line pt-4">
        <div className="flex items-center gap-3">
          <LikeButton
            count={post.likeCount}
            disabledReason={like.disabledReason}
            isLiked={post.likedByMe}
            isPending={like.isPending}
            onToggle={like.toggle}
          />
          {like.disabledReason !== undefined && (
            <p className="text-sm text-muted">{like.disabledReason}.</p>
          )}
        </div>

        <ReplyComposer postId={post.id} />
      </div>
    </article>
  );
}

/***** Export default *****/

export default Discussion;
