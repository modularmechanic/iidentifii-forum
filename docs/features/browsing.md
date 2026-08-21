# Browsing

Who this is for: developers and testers working on how the forum is read.
What you'll get: what anonymous reading does, and which pieces make it work.

## What it does

Anyone can read the forum without an account. The landing page lists discussions newest first,
ten at a time. Opening one shows it in full, any moderator flag, and its replies oldest first.

Like counts are visible to everyone, but there is no like control while signed out, and the API
never reports a discussion as liked by an anonymous reader.

## Endpoints

| Method | Path | Returns |
| --- | --- | --- |
| GET | `/api/v1/posts` | A page of discussions |
| GET | `/api/v1/posts/{id}` | One discussion |
| GET | `/api/v1/posts/{id}/comments` | A page of replies |

Paging uses `page` (from 1) and `pageSize` (1 to 100). Anything outside those bounds returns 400
naming the field. An unknown identifier returns 404. Both are RFC 7807 payloads.

## How it hangs together

```
PostsContainer ─┐                         PostsController
                ├─ services (web) ─────▶  (discussions and replies) (HTTP)
RepliesContainer┘                                │
                                          PostService, CommentService   (rules and flow)
                                                 │
                                          PostProjections            (one shape, shared)
                                                 │
                                          ForumDbContext             (Entity Framework)
```

Containers fetch and hold state; presenters such as `PostsList` and `RepliesList` take props and
render. Components never call the network directly.

## Decisions worth knowing

**One projection for list and detail.** `PostProjections.ToDto` defines the payload once. Both
paths use it, so the two shapes cannot drift apart, and it translates into a single query with
subquery counts rather than a query per row.

**Counts come from the database.** `likeCount` and `commentCount` are counted in the same
statement. Listing twenty discussions costs two queries: one count, one page.

**Ordering is stable.** Discussions are ordered by creation time and then by identifier. Without
the second key, rows sharing a timestamp could appear on two pages or on none.

**Replies read oldest first.** A conversation makes sense in the order it happened, which is the
opposite of the list.

## Testing it

Integration tests in `backend/tests/Forum.Tests/Api/BrowsingTests.cs` run against a throwaway
PostgreSQL container with the same seed data. They cover paging, the payload shape, flagged
discussions, out-of-range paging, unknown identifiers, and that three consecutive pages never
repeat a discussion.

Component tests sit beside the components they cover.
