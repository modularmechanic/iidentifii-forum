import { fetchJson } from '@src/infra/http';
import type { IPaged } from '@src/domains/common/types/Paged';
import type { IPost, ModerationTag, PostSort, SortOrder } from './Post';

/***** Types *****/

/** Everything the list endpoint accepts. Undefined values are simply left out. */
export interface IPostPageRequest {
  page?: number;
  pageSize?: number;
  from?: string;
  to?: string;
  author?: string;
  tag?: ModerationTag;
  sort?: PostSort;
  order?: SortOrder;
}

/***** Functions *****/

/** Reads discussions. Components call this through a container, never directly. */
const PostService = {
  async fetchPage(request: IPostPageRequest = {}): Promise<IPaged<IPost>> {
    const query = new URLSearchParams();

    for (const [key, value] of Object.entries(request)) {
      if (value !== undefined && value !== '') {
        query.set(key, String(value));
      }
    }

    const suffix = query.size > 0 ? `?${query.toString()}` : '';
    return await fetchJson<IPaged<IPost>>(`/posts${suffix}`);
  },

  async fetchById(id: string): Promise<IPost> {
    return await fetchJson<IPost>(`/posts/${id}`);
  },
} as const;

export default PostService;
