import type { IAuthor } from '@src/domains/common/types/Author';

/***** Constants *****/

/** The marks a moderator can apply. A constant object rather than an enum, which cannot be erased. */
export const ModerationTags = {
  MisleadingOrFalse: 'MisleadingOrFalse',
} as const;

export type ModerationTag = (typeof ModerationTags)[keyof typeof ModerationTags];

/** How each mark reads on screen. */
export const ModerationTagLabels: Record<ModerationTag, string> = {
  [ModerationTags.MisleadingOrFalse]: 'Misleading or false',
};

/** How the list is ordered. */
export const PostSorts = {
  CreatedAt: 'CreatedAt',
  LikeCount: 'LikeCount',
} as const;

export type PostSort = (typeof PostSorts)[keyof typeof PostSorts];

/** Which end of the ordering comes first. */
export const SortOrders = {
  Ascending: 'Ascending',
  Descending: 'Descending',
} as const;

export type SortOrder = (typeof SortOrders)[keyof typeof SortOrders];

/***** Types *****/

export interface IModerationTag {
  tag: ModerationTag;
  taggedByUsername: string;
  createdAt: string;
}

/** A discussion, in the shape the list and the detail view both use. */
export interface IPost {
  id: string;
  title: string;
  body: string;
  author: IAuthor;
  createdAt: string;
  updatedAt: string | null;
  likeCount: number;
  commentCount: number;
  tags: IModerationTag[];
  likedByMe: boolean;
}
