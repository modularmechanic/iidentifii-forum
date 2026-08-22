import { fetchJson } from '@src/infra/http';
import type { IPaged } from '@src/domains/common/types/Paged';
import type { IPost, ModerationTag, PostSort, SortOrder } from './Post';

/***** Types *****/

/** What starting a discussion needs. */
export interface ICreatePostRequest {
  title: string;
  body: string;
}

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

/** Reads and writes discussions. Components call this through a container, never directly. */
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

  async create(request: ICreatePostRequest): Promise<IPost> {
    return await fetchJson<IPost>('/posts', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  },

  async update(id: string, request: ICreatePostRequest): Promise<IPost> {
    return await fetchJson<IPost>(`/posts/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
  },

  async remove(id: string): Promise<void> {
    await fetchJson<void>(`/posts/${id}`, { method: 'DELETE' });
  },

  async flag(id: string, tag: ModerationTag): Promise<void> {
    await fetchJson<void>(`/posts/${id}/tags`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ tag }),
    });
  },

  async unflag(id: string, tag: ModerationTag): Promise<void> {
    await fetchJson<void>(`/posts/${id}/tags/${tag}`, { method: 'DELETE' });
  },

  async like(id: string): Promise<void> {
    await fetchJson<void>(`/posts/${id}/like`, { method: 'POST' });
  },

  async unlike(id: string): Promise<void> {
    await fetchJson<void>(`/posts/${id}/like`, { method: 'DELETE' });
  },
} as const;

export default PostService;
