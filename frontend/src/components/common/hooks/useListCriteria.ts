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

/** Mirrors the API's own bounds, so the address cannot ask for something it would refuse. */
const MAX_PAGE = 21_474_836;
const MAX_USERNAME_LENGTH = 32;
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

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

  const criteria = useMemo<IListCriteria>(() => {
    const from = _readDate(searchParams.get('from'));
    const to = _readDate(searchParams.get('to'));

    // A range that ends before it starts would be refused by the API, so it is dropped here
    // rather than sent and reported back as an error the reader did not cause.
    const isRangeUsable = from === undefined || to === undefined || from <= to;

    return {
      page: _readPage(searchParams.get('page')),
      sort: _readOneOf(searchParams.get('sort'), PostSorts, DEFAULTS.sort),
      order: _readOneOf(searchParams.get('order'), SortOrders, DEFAULTS.order),
      from: isRangeUsable ? from : undefined,
      to: isRangeUsable ? to : undefined,
      author: _readAuthor(searchParams.get('author')),
      tag: _readOneOf(searchParams.get('tag'), ModerationTags, undefined),
    };
  }, [searchParams]);

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

/** Falls back to the first page when the address holds anything the API would not accept. */
function _readPage(value: string | null): number {
  const page = Number(value);
  return Number.isInteger(page) && page > 0 && page <= MAX_PAGE ? page : DEFAULTS.page;
}

/** Accepts a real calendar date written as YYYY-MM-DD, and nothing else. */
function _readDate(value: string | null): string | undefined {
  if (value === null || !ISO_DATE.test(value)) {
    return undefined;
  }

  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isNaN(parsed.getTime()) || !parsed.toISOString().startsWith(value)
    ? undefined
    : value;
}

/** Ignores a name no account could have. */
function _readAuthor(value: string | null): string | undefined {
  const author = value?.trim();
  return author && author.length <= MAX_USERNAME_LENGTH ? author : undefined;
}

/** Accepts a value only when the API would recognise it, so a stray address cannot break a fetch. */
function _readOneOf<T extends string, TFallback extends T | undefined>(
  value: string | null,
  allowed: Record<string, T>,
  fallback: TFallback,
): T | TFallback {
  return Object.values(allowed).includes(value as T) ? (value as T) : fallback;
}
