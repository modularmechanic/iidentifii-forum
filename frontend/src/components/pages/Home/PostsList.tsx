import { Link } from 'react-router';
import Avatar from '@src/components/common/ui/sm/Avatar';
import Empty from '@src/components/common/ui/sm/Empty';
import Pill from '@src/components/common/ui/sm/Pill';
import LikeCount from '@src/components/common/ui/md/LikeCount';
import { getShortDate } from '@src/common/utils/format-date';
import { ModerationTagLabels, type IPost } from '@src/domains/posts/Post';
import Paths from '@src/domains/common/constants/Paths';

/***** Types *****/

interface IProps {
  posts: IPost[];
}

interface IRowProps {
  post: IPost;
}

/***** Components *****/

/** Default component: the discussions on this page, or a note that there are none. */
function PostsList(props: IProps) {
  const { posts } = props;

  if (posts.length === 0) {
    return (
      <div className="rounded-sm border border-line">
        <Empty title="No discussions match these filters">
          Try a wider date range, or clear the filters.
        </Empty>
      </div>
    );
  }

  return (
    <ul className="divide-y divide-line rounded-sm border border-line">
      {posts.map((post) => (
        <PostRow key={post.id} post={post} />
      ))}
    </ul>
  );
}

/** One discussion in the list: its like count, title, opening and who started it. */
function PostRow(props: IRowProps) {
  const { post } = props;

  return (
    <li className="flex gap-3 p-4 hover:bg-subtle">
      <LikeCount count={post.likeCount} />
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <h3 className="font-medium">
            <Link className="hover:text-accent" to={Paths.discussion(post.id)}>
              {post.title}
            </Link>
          </h3>
          {post.tags.map((tag) => (
            <Pill key={tag.tag} label={ModerationTagLabels[tag.tag]} />
          ))}
        </div>
        <p className="mt-1 line-clamp-2 text-sm text-muted">{post.body}</p>
        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-muted">
          <Avatar username={post.author.username} />
          <span className="font-medium text-ink">{post.author.username}</span>
          <time dateTime={post.createdAt}>{getShortDate(post.createdAt)}</time>
          <span aria-hidden="true">·</span>
          <span>{_describeReplies(post)}</span>
        </div>
      </div>
    </li>
  );
}

/***** Functions *****/

/** Reads better than a bare number when there are no replies at all. */
function _describeReplies(post: IPost): string {
  if (post.commentCount === 0) {
    return 'No replies yet';
  }

  return `${post.commentCount} ${post.commentCount === 1 ? 'reply' : 'replies'}`;
}

/***** Export default *****/

export default PostsList;
