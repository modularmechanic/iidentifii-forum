import type { IComment } from '@src/domains/comments/Comment';
import { ModerationTags, type IPost } from '@src/domains/posts/Post';

/** Builds a discussion for tests, letting each test state only what it cares about. */
export function buildPost(overrides: Partial<IPost> = {}): IPost {
  return {
    id: '01a02518-cbf8-7b23-8b12-613e5bf64a39',
    title: 'Liveness webhook retries',
    body: 'The same callback arrives twice.',
    author: { id: '01a02518-cb7b-7e87-b96d-c25b67ec74f0', username: 'bob' },
    createdAt: '2026-08-20T14:02:00+00:00',
    updatedAt: null,
    likeCount: 14,
    commentCount: 7,
    tags: [],
    likedByMe: false,
    ...overrides,
  };
}

export function buildFlag(taggedByUsername = 'mod') {
  return {
    tag: ModerationTags.MisleadingOrFalse,
    taggedByUsername,
    createdAt: '2026-08-21T08:12:00+00:00',
  };
}

export function buildComment(overrides: Partial<IComment> = {}): IComment {
  return {
    id: '01a02518-0000-7000-8000-000000000001',
    postId: '01a02518-cbf8-7b23-8b12-613e5bf64a39',
    body: 'At least once.',
    author: { id: '01a02518-cb9d-7fca-9a45-73d299654100', username: 'carol' },
    createdAt: '2026-08-20T14:30:00+00:00',
    updatedAt: null,
    ...overrides,
  };
}
