import { fetchJson } from '@src/infra/http';
import type { IPaged } from '@src/domains/common/types/Paged';
import type { IComment } from './Comment';

/** Reads replies to one discussion. */
const CommentService = {
  async fetchPage(postId: string, page = 1, pageSize = 20): Promise<IPaged<IComment>> {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return await fetchJson<IPaged<IComment>>(`/posts/${postId}/comments?${query.toString()}`);
  },
} as const;

export default CommentService;
