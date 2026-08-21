import { fetchJson } from '@src/infra/http';
import type { IPaged } from '@src/domains/common/types/Paged';
import type { IComment } from './Comment';

/** Reads and writes replies to one discussion. */
const CommentService = {
  async fetchPage(postId: string, page = 1, pageSize = 20): Promise<IPaged<IComment>> {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return await fetchJson<IPaged<IComment>>(`/posts/${postId}/comments?${query.toString()}`);
  },

  async create(postId: string, body: string): Promise<IComment> {
    return await fetchJson<IComment>(`/posts/${postId}/comments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ body }),
    });
  },
} as const;

export default CommentService;
