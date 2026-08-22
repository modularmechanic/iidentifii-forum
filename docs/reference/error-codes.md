# Error codes

Who this is for: anyone calling the API and deciding what to do when it says no.
What you'll get: every failure shape the API produces, what causes it, and how to tell two
similar-looking failures apart.

Every failure is an RFC 7807 problem document sent as `application/problem+json`. Nothing fails
with an HTML page or a bare status.

## The shape

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not found.",
  "status": 404,
  "detail": "Discussion 01a02518-0000-7000-8000-000000000000 was not found.",
  "traceId": "00-2cb1b9262c3ed281710b90a660f5e87a-949ce4348b0eda7d-00"
}
```

| Field | Always present | Notes |
| --- | --- | --- |
| `type` | yes | A link to the section of the specification that defines the status |
| `title` | yes | A short, fixed phrase. Suitable for switching on |
| `status` | yes | Matches the HTTP status |
| `detail` | no | A sentence for a person. Absent when the framework refused the request before any of the forum's own code ran |
| `traceId` | yes | Identifies this exact request in the log |
| `errors` | validation only | Field name to the list of things wrong with it |

`detail` is written to be shown to a reader. `title` is written to be matched in code. Neither
carries internal detail: an unexpected failure says the same fixed sentence every time.

## Statuses

| Status | Title | Raised when |
| --- | --- | --- |
| 400 | `One or more validation errors occurred.` | A field or query parameter broke a declared rule |
| 400 | `Invalid token.` | A confirmation or reset link is unknown, expired, spent, or retired by a newer one |
| 401 | `Unauthorized` | No `Authorization` header, or a token that is expired, malformed or not signed by this API |
| 401 | `Not signed in.` | The username and password do not match, or a sign-in code is wrong, expired or spent |
| 403 | `Forbidden` | The session is valid but lacks the role the endpoint requires — an ordinary member flagging content |
| 403 | `Not allowed.` | A rule of the forum refused this member — editing or deleting somebody else's discussion or reply |
| 403 | `Email not confirmed.` | The password was right, but the address on the account has not been confirmed |
| 404 | `Not Found` | The route does not exist, or a path parameter is not the shape the route expects |
| 404 | `Not found.` | The route exists and the thing it names does not |
| 409 | `Already exists.` | The username or address is registered already |
| 409 | `Already done.` | The discussion is already liked, or already carries that flag |
| 422 | `Rule violated.` | Understood, but against a rule: liking your own discussion, an empty title |
| 429 | `Too many requests.` | The rate limit for this caller. `Retry-After` says how many seconds to wait |
| 500 | `An unexpected error occurred.` | Something not anticipated. Quote `traceId` |

## Two 401s, and two 403s

The framework refuses some requests before the forum sees them, and those refusals carry a
one-word title and no `detail`:

```json
{ "type": "…#section-15.5.2", "title": "Unauthorized", "status": 401, "traceId": "…" }
```

The forum's own refusals carry a sentence:

```json
{ "type": "…#section-15.5.2", "title": "Not signed in.", "status": 401,
  "detail": "That username and password do not match.", "traceId": "…" }
```

The distinction is useful: `Unauthorized` means *present a valid token*, while `Not signed in.`
means *these credentials are wrong*. Likewise `Forbidden` means *you lack the role*, while
`Not allowed.` means *you have the role and this still is not yours*.

## Validation failures

A rejected field carries `errors`, keyed by the name of the field:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "…",
  "errors": { "PageSize": ["Page size must be between 1 and 100."] }
}
```

Keys are the property names as declared, so a query parameter appears as `PageSize`, and a field
that failed while the body was being read appears with a JSON path such as `$.challengeId`.

The web client reads `errors` and puts each message beside the field it belongs to, which is the
reason the failure is shaped this way rather than as one sentence.

Messages you will actually see:

| Message | Cause |
| --- | --- |
| `Page numbering starts at 1.` | `page` below 1, or above 21474836 |
| `Page size must be between 1 and 100.` | `pageSize` outside its range |
| `A username cannot be longer than 32 characters.` | `author` filter too long |
| `The start of the range cannot be after its end.` | `from` later than `to` |
| `A username may contain letters, numbers and underscores.` | Punctuation in a username |
| `A password must be at least 8 characters.` | Registration or reset |
| `A sign-in code is six digits.` | The code is the wrong shape, and is refused before it is checked |

An unrecognised value for `sort`, `order` or `tag` is a 400 as well, because the parameter is
bound to an enumeration and an unknown name does not bind.

## 404 for something that is not a UUID

`GET /posts/not-a-guid` answers 404, not 400. The route constrains the identifier to a UUID, so a
value of another shape does not match the route at all and there is nothing to validate. This is
the framework's `Not Found`, with no `detail`.

## 429

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
  "title": "Too many requests.",
  "status": 429,
  "detail": "Rate limit exceeded. Try again in 60 seconds."
}
```

`Retry-After` carries the same number as a header. Limits are per caller address and are
configurable; see [configuration](configuration.md).

## 500

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An unexpected error occurred.",
  "status": 500,
  "detail": "The request could not be completed. Quote the trace identifier when reporting this.",
  "traceId": "…"
}
```

The message is fixed. The exception, its type and its stack are logged against the same
`traceId` and go no further, because the reader of a log is trusted and a caller is not.

## Related

- [Endpoint reference](api-endpoints.md) — which statuses each endpoint can produce
- [Security model](../explanation/security-model.md) — what the API refuses to disclose, and why
