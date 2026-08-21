import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';
import {
  ModerationTags,
  PostSorts,
  SortOrders,
  type ModerationTag,
  type PostSort,
  type SortOrder,
} from '@src/domains/posts/Post';

/***** Constants *****/

const DEFAULTS = {
  page: 1,
  sort: PostSorts.CreatedAt,
  order: SortOrders.Descending,
} as const;

/***** Types *****/

export interface IListCriteria {
  page: number;
  sort: PostSort;
  order: SortOrder;
  from?: string;
  to?: string;
  author?: string;
  tag?: ModerationTag;
}

/***** Functions *****/

/**
 * Keeps the list criteria in the address bar, so a filtered view can be shared, bookmarked and
 * restored by the back button rather than living only in component state.
 */
export function useListCriteria() {
  const [searchParams, setSearchParams] = useSearchParams();

  const criteria = useMemo<IListCriteria>(
    () => ({
      page: _readPage(searchParams.get('page')),
      sort: _readOneOf(searchParams.get('sort'), PostSorts, DEFAULTS.sort),
      order: _readOneOf(searchParams.get('order'), SortOrders, DEFAULTS.order),
      from: searchParams.get('from') ?? undefined,
      to: searchParams.get('to') ?? undefined,
      author: searchParams.get('author') ?? undefined,
      tag: _readOneOf(searchParams.get('tag'), ModerationTags, undefined),
    }),
    [searchParams],
  );

  /** Applies a change, dropping anything left at its default so the address stays readable. */
  const update = useCallback(
    (changes: Partial<IListCriteria>) => {
      const next = { ...criteria, ...changes };

      // Any change other than turning the page starts again from the first one.
      if (changes.page === undefined) {
        next.page = 1;
      }

      const params = new URLSearchParams();
      for (const [key, value] of Object.entries(next)) {
        if (
          value !== undefined &&
          value !== '' &&
          value !== DEFAULTS[key as keyof typeof DEFAULTS]
        ) {
          params.set(key, String(value));
        }
      }

      setSearchParams(params);
    },
    [criteria, setSearchParams],
  );

  const clear = useCallback(() => setSearchParams(new URLSearchParams()), [setSearchParams]);

  const hasFilters = Boolean(criteria.from || criteria.to || criteria.author || criteria.tag);

  return { criteria, update, clear, hasFilters };
}

/** Falls back to the first page when the address holds something that is not a page number. */
function _readPage(value: string | null): number {
  const page = Number(value);
  return Number.isInteger(page) && page > 0 ? page : DEFAULTS.page;
}

/** Accepts a value only when the API would recognise it, so a stray address cannot break a fetch. */
function _readOneOf<T extends string, TFallback extends T | undefined>(
  value: string | null,
  allowed: Record<string, T>,
  fallback: TFallback,
): T | TFallback {
  return Object.values(allowed).includes(value as T) ? (value as T) : fallback;
}
