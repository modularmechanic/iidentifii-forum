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
| `page` | integer | 1 | From 1 |
| `pageSize` | integer | 20 | 1 to 100 |

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
| 400 | `page` below 1, or `pageSize` outside 1 to 100 |

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

| Status | When |
| --- | --- |
| 200 | Found, even with no replies |
| 400 | Paging outside its bounds |
| 404 | No discussion has that identifier |

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
