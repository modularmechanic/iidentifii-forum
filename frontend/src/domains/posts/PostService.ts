import { fetchJson } from '@src/infra/http';
import type { IPaged } from '@src/domains/common/types/Paged';
import type { IPost } from './Post';

/***** Types *****/

export interface IPostPageRequest {
  page?: number;
  pageSize?: number;
}

/***** Functions *****/

/** Reads discussions. Components call this through a container, never directly. */
const PostService = {
  async fetchPage(request: IPostPageRequest = {}): Promise<IPaged<IPost>> {
    const query = new URLSearchParams();

    if (request.page !== undefined) {
      query.set('page', String(request.page));
    }

    if (request.pageSize !== undefined) {
      query.set('pageSize', String(request.pageSize));
    }

    const suffix = query.size > 0 ? `?${query.toString()}` : '';
    return await fetchJson<IPaged<IPost>>(`/posts${suffix}`);
  },

  async fetchById(id: string): Promise<IPost> {
    return await fetchJson<IPost>(`/posts/${id}`);
  },
} as const;

export default PostService;
