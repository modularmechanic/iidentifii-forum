# Filtering and sorting

Who this is for: developers and testers working on how discussions are found.
What you'll get: what the filters do, how ordering stays reliable, and how to test it.

## What it does

The list can be narrowed by date range, author or moderation flag, and ordered by when a
discussion started or by how many people liked it. Filters combine: asking for Carol's flagged
discussions returns discussions that are both.

Every choice lives in the address bar, so a filtered view can be shared, bookmarked and reached
again with the back button.

## Parameters

| Parameter | Accepts | Default |
| --- | --- | --- |
| `from`, `to` | A date, `YYYY-MM-DD`, in UTC | No bound |
| `author` | A username, up to 32 characters | Everyone |
| `tag` | `MisleadingOrFalse` | Any |
| `sort` | `CreatedAt`, `LikeCount` | `CreatedAt` |
| `order` | `Ascending`, `Descending` | `Descending` |

Replies accept `order` too, so a long conversation can be read newest first.

## Decisions worth knowing

**The end of a range means the whole of that day.** `to=2026-08-20` includes a discussion started
at 23:59 that day. The comparison uses an exclusive bound on the following midnight rather than
comparing dates row by row, so the index still applies.

**Ordering is total.** Ten members can like a discussion, so many share a like count. Ordering by
like count alone would be ambiguous, and an ambiguous order means a discussion can appear on two
pages or on none. Every ordering therefore ends with the creation time and then the identifier.

**Author names ignore case.** Usernames are stored as `citext`, so `?author=ALICE` matches `alice`
and still uses the unique index rather than scanning.

**Page numbers are bounded.** Skipping to a page is `(page - 1) × pageSize`. Left unbounded, a
large page number overflows into a negative offset and the request fails. The maximum page is
derived from the maximum page size so that cannot happen.

**The address is the state.** The front-end reads its criteria from the query string rather than
holding them in component state. Anything the API would reject is ignored on the way in, so a
hand-edited address cannot break the page.

## Testing it

`backend/tests/Forum.Tests/Api/FilteringAndSortingTests.cs` covers each filter alone and combined,
both orderings of both fields, that three pages of a like-count ordering never repeat a
discussion, and the refusals: a range that ends before it starts, an unknown sort, an unknown flag,
a malformed date and an over-long author name.

`useListCriteria.test.tsx` covers reading and writing the address, ignoring values the API would
reject, returning to the first page when a filter changes, and clearing.
