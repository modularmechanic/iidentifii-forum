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

## Contributing content

Every endpoint here needs `Authorization: Bearer <token>`; without one the answer is 401.

### `POST /posts`

`{ "title": "...", "body": "..." }`

| Status | When |
| --- | --- |
| 201 | Created. `Location` carries the new discussion |
| 400 | The title or body is empty, or longer than the limit |
| 401 | No session |

### `POST /posts/{id}/comments`

`{ "body": "..." }`

| Status | When |
| --- | --- |
| 201 | Created |
| 400 | The body is empty or longer than 2,000 characters |
| 401 | No session |
| 404 | No discussion has that identifier |

### `POST /posts/{id}/like`

No body.

| Status | When |
| --- | --- |
| 201 | The like was recorded |
| 401 | No session |
| 404 | No discussion has that identifier |
| 409 | You have already liked it. Two requests at once produce one 201 and one 409 |
| 422 | It is your own discussion |

### `DELETE /posts/{id}/like`

No body.

| Status | When |
| --- | --- |
| 204 | The like is gone |
| 401 | No session |
| 404 | No discussion has that identifier, or you had not liked it |

## Moderation and ownership

Every endpoint here needs a session. A member who is not permitted gets 403, not 404: the
discussion exists, and pretending otherwise would be a different lie.

### `POST /posts/{id}/tags`

`{ "tag": "MisleadingOrFalse" }`. Moderators only.

| Status | When |
| --- | --- |
| 201 | The flag was applied |
| 400 | The tag is not one the forum recognises |
| 403 | The caller is not a moderator |
| 404 | No discussion has that identifier |
| 409 | It already carries that flag |

### `DELETE /posts/{id}/tags/{tag}`

Moderators only, checked by the endpoint policy and again by the discussion itself.

| Status | When |
| --- | --- |
| 204 | The flag was removed |
| 401 | No session |
| 403 | The caller is not a moderator |
| 404 | No discussion has that identifier, or it did not carry that flag |

### `PUT /posts/{id}`

`{ "title": "...", "body": "..." }`. The author only. Returns the discussion with `updatedAt` set.

| Status | When |
| --- | --- |
| 200 | Saved |
| 400 | The title or body is empty, or too long |
| 403 | The caller did not write it. A moderator is refused here too |
| 404 | No discussion has that identifier |

### `DELETE /posts/{id}`

The author only. 204, and the replies, likes and flags go with it by cascade.

### `PUT /comments/{id}` and `DELETE /comments/{id}`

`{ "body": "..." }` for the edit. The author of the reply only; 200 or 204, 403 for anybody else.

## Signing in

### `POST /auth/login`

`{ "username": "...", "password": "..." }`

Checks the password and emails a six-digit code. The password alone does not sign anybody in.

Returns `{ "challengeId": "...", "maskedEmail": "b**@forum.local", "expiresAt": "..." }`.

| Status | When |
| --- | --- |
| 200 | The password was right; a code is on its way |
| 400 | A field is missing |
| 401 | The username is unknown, or the password is wrong. The two are not told apart |
| 403 | The address has not been confirmed yet |

### `POST /auth/verify-2fa`

`{ "challengeId": "...", "code": "123456" }`

Returns `{ "token": "...", "expiresAt": "...", "user": { ... } }`.

| Status | When |
| --- | --- |
| 200 | The code was right, and a session is returned |
| 400 | The code is not six digits, or the challenge identifier is missing |
| 401 | The code is wrong, expired, already used, or the challenge is spent |

Five wrong codes spend the challenge. After that the right code is refused too, and signing in
starts again from the password.

### `POST /auth/forgot-password`

`{ "email": "..." }`

| Status | When |
| --- | --- |
| 202 | The request was well formed. Whether or not the address belongs to an account |
| 400 | `email` is missing, or is not shaped like an address |

The 400 is about the shape of the request, not about the account: a well-formed address always
gets 202, and the cooldown is applied silently. Neither the status, the body nor the timing
reveals who is registered.

### `POST /auth/reset-password`

`{ "token": "...", "newPassword": "..." }`, the token taken from the emailed link.

| Status | When |
| --- | --- |
| 200 | The password is changed |
| 400 | The token is unknown, expired or already used, or the password breaks a rule |

Setting a password retires every outstanding one-time token for that account. Sessions already
issued are not revoked; see [the security model](../explanation/security-model.md).

### `GET /auth/me`

Requires `Authorization: Bearer <token>`. Returns the signed-in member.

| Status | When |
| --- | --- |
| 200 | The token is valid |
| 401 | The token is missing, expired or not trusted |

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

A body the API cannot read is reported the same way, against the field it could not read rather
than against the position in the document where reading stopped:

```json
{
  "status": 400,
  "errors": { "challengeId": ["The value is not in a form this field accepts."] }
}
```

| Status | Meaning |
| --- | --- |
| 400 | The request was malformed or outside allowed bounds |
| 404 | The item named does not exist |
| 429 | Too many requests; `Retry-After` says how long to wait |
| 500 | Something unexpected. `traceId` identifies it in the logs |

No failure includes internal detail: an unexpected one is reported without its message, and a
value the reader could not parse is described in the forum's words rather than the parser's, which
would otherwise name types and count bytes. Quote `traceId` when reporting one.

## Rate limits

| Scope | Limit |
| --- | --- |
| Everything | 120 requests a minute per address |
| Authentication routes | 10 requests a minute per address |
| `GET /auth/me` | 120 requests a minute per address |

`GET /auth/me` sits with the authentication routes but is not an attempt to authenticate: a page
checking who is signed in should not spend the small sign-in budget. It gets the ordinary
allowance rather than being excused from limiting, which would take the global limit with it.
