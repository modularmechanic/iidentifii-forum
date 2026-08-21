# API endpoints

Who this is for: anyone integrating with the API, whether through the web client or directly.
What you'll get: every endpoint, what it accepts, and what it returns when things go wrong.

Every endpoint below is prefixed `/api/v1`, except `GET /health`, which is not versioned.
Requests and responses are JSON. Failures are RFC 7807
problem documents sent as `application/problem+json`.

## Reading discussions

### `GET /posts`

Returns a page of discussions, newest first.

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `page` | integer | 1 | From 1, up to 21474836 |
| `pageSize` | integer | 20 | 1 to 100 |
| `from` | date | none | `YYYY-MM-DD`, UTC; on or after this day |
| `to` | date | none | `YYYY-MM-DD`, UTC; the whole of this day is included |
| `author` | string | none | Username, case ignored, up to 32 characters |
| `tag` | string | none | `MisleadingOrFalse` |
| `sort` | string | `CreatedAt` | `CreatedAt` or `LikeCount` |
| `order` | string | `Descending` | `Ascending` or `Descending` |

Filters combine. Ordering always ends with the creation time and the identifier, so paging never
repeats or skips a discussion when several share a like count.

```json
{
  "items": [
    {
      "id": "01a02518-cbf8-7b23-8b12-613e5bf64a39",
      "title": "Liveness webhook retries",
      "body": "The same callback arrives twice.",
      "author": { "id": "01a02518-cb7b-7e87-b96d-c25b67ec74f0", "username": "bob" },
      "createdAt": "2026-08-20T14:02:00+00:00",
      "updatedAt": null,
      "likeCount": 14,
      "commentCount": 7,
      "tags": [
        { "tag": "MisleadingOrFalse", "taggedByUsername": "mod", "createdAt": "2026-08-21T08:12:00+00:00" }
      ],
      "likedByMe": false
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 20,
  "totalPages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

`likedByMe` is always `false` for an anonymous caller.

| Status | When |
| --- | --- |
| 200 | Always, even when no discussion matches |
| 400 | Paging outside its bounds, a range that ends before it starts, an `author` longer than 32 characters, or an unrecognised `sort`, `order`, `tag` or date |

### `GET /posts/{id}`

Returns one discussion, in the same shape as a list entry.

| Status | When |
| --- | --- |
| 200 | Found |
| 404 | No discussion has that identifier, or it is not a UUID |

### `GET /posts/{id}/comments`

Returns a page of replies, oldest first.

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `page` | integer | 1 | From 1 |
| `pageSize` | integer | 20 | 1 to 100 |
| `order` | string | `Ascending` | Oldest first by default |

| Status | When |
| --- | --- |
| 200 | Found, even with no replies |
| 400 | Paging outside its bounds |
| 404 | No discussion has that identifier |

## Accounts

### `POST /auth/register`

`{ "username": "...", "email": "...", "password": "..." }`

| Field | Rule |
| --- | --- |
| `username` | 3 to 32 characters, letters, numbers and underscores; unique regardless of case |
| `email` | A valid address, unique regardless of case |
| `password` | 8 to 128 characters |

| Status | When |
| --- | --- |
| 201 | The account was created and a confirmation link sent |
| 400 | A field breaks one of the rules above |
| 409 | The username or address is already registered |

### `POST /auth/verify-email`

`{ "token": "..." }`, taken from the emailed link.

| Status | When |
| --- | --- |
| 200 | The address is confirmed |
| 400 | The token is unknown, expired, or already used |

### `POST /auth/resend-verification`

`{ "email": "..." }`

Always returns 202, whether or not the address belongs to an account. A cooldown is applied
silently. Neither the status nor the body reveals who is registered.

## Service

### `GET /health`

Returns `{ "status": "healthy", "environment": "Development", "checkedAt": "..." }`. Not versioned,
because a probe should not have to know the API version.

## Failures

Every failure carries the same shape:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not found.",
  "status": 404,
  "detail": "Discussion 01a0... was not found.",
  "traceId": "00-c592f2...-e7d667...-00"
}
```

A validation failure adds `errors`, keyed by field:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "PageSize": ["Page size must be between 1 and 100."] }
}
```

| Status | Meaning |
| --- | --- |
| 400 | The request was malformed or outside allowed bounds |
| 404 | The item named does not exist |
| 429 | Too many requests; `Retry-After` says how long to wait |
| 500 | Something unexpected. `traceId` identifies it in the logs |

An unexpected failure never includes internal detail. Quote `traceId` when reporting one.

## Rate limits

| Scope | Limit |
| --- | --- |
| Everything | 120 requests a minute per address |
| Authentication routes | 10 requests a minute per address |
