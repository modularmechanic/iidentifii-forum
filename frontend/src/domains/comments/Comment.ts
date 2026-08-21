import type { IAuthor } from '@src/domains/common/types/Author';

/** A reply to a discussion. */
export interface IComment {
  id: string;
  postId: string;
  body: string;
  author: IAuthor;
  createdAt: string;
  updatedAt: string | null;
}
