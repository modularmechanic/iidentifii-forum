# Walkthrough

Who this is for: whoever is presenting this project, and whoever is assessing it.
What you'll get: a five-minute demonstration in the order that makes sense, and straight answers
to the questions the code invites.

## Before the room

```bash
docker compose up -d --build
```

Wait for `web` to report `healthy`, then have four tabs open:

| Tab | Address |
| --- | --- |
| The forum | http://localhost:8080 |
| Mailpit | http://localhost:8025 |
| API reference | http://localhost:5080/scalar |
| Postman | The collection from `docs/postman/`, with the **Forum containers** environment selected |

Sign out of the forum first, so the demonstration starts where a stranger starts.

## The five minutes

### 1. Reading is open — 40 seconds

Land on the forum signed out. Twenty seeded discussions, with replies and likes.

Change the ordering: **Latest**, then **Top**. Open **Filters** and narrow by author.

Point at the address bar as it changes. *The filters live in the URL, so a narrowed list is a link
and the back button works.*

### 2. The flag — 30 seconds

Open a flagged discussion. The mark is deliberately hard to miss, because it exists for
regulatory reasons.

Filter the list down to flagged discussions.

### 3. Signing in takes two steps — 90 seconds

Log in as `alice` / `Password123!`.

*The password does not sign anybody in.* Switch to Mailpit, read the six-digit code, come back and
type it.

Worth saying while you type: five wrong codes spend the attempt outright.

### 4. Writing, and the rules that refuse it — 60 seconds

- Like somebody else's discussion. The count moves.
- Hover the like control on one of `alice`'s own. It is inert, and says why.
- Reply to a discussion.
- Start a discussion, then edit it. It says **edited**.

### 5. Moderation is a separate power — 45 seconds

Log out, log in as `mod`, read that code from Mailpit.

The masthead says **moderator**. Flag a discussion.

Then switch to Postman and, as `mod`, try to edit somebody else's discussion: **403**. *A moderator
flags. A moderator does not rewrite other people's words, and the API enforces it, not the
interface.*

### 6. The API is the product — 45 seconds

In Postman, run the whole collection. Eighty-two requests, every failure case included, green.

Show one failure body: a problem document with a title, a detail and a trace identifier.

*The web client is one consumer of this API. Nothing it does is a private route.*

### 7. What holds it up — 30 seconds

```bash
dotnet test backend/Forum.slnx
```

155 backend tests, 113 frontend tests. Integration tests run against a real PostgreSQL in a
throwaway container, because the design leans on things only PostgreSQL does.

## Questions, answered

### Why Clean Architecture without MediatR?

Four projects with dependencies pointing inwards, enforced by the compiler. No mediator.

A mediator earns its place when there are cross-cutting behaviours to wrap around every request
and enough handlers that hand-wiring is worse than the indirection. Here there are four services
and a handful of methods each. A mediator would replace a call you can follow in an editor with a
message you have to search for.

The cross-cutting concerns are handled elsewhere instead: validation is attributes on the request
records, failures are translated in one exception handler, rate limiting is middleware. If the
number of operations tripled and each needed the same three things wrapped around it, that
calculation changes.

### Why source-generated JSON?

Every payload type is registered on `ForumJsonContext`, and
`JsonSerializerIsReflectionEnabledByDefault` is off.

Three reasons. A type somebody forgot to register fails loudly instead of quietly falling back to
reflection. The request path uses no runtime reflection at all. And the application stays ready
for ahead-of-time compilation without a rewrite.

The cost is one attribute per type, and a confusing failure the first time somebody adds a DTO and
forgets. That is a real cost, paid once per type.

### Why email for the second factor?

An authenticator app is stronger, and needs enrolment, a QR code, a stored secret and recovery
codes — a slice of its own. SMS needs a provider, money, a phone number per member, and is the
weakest of the three.

Email is the one channel every account already has, because the address is confirmed at
registration. It demonstrates the gate without adding a dependency.

The honest limit: the second factor is only as strong as the inbox. Somebody who has taken over
the mailbox has both halves. That is why the code is six digits, lives ten minutes, and dies after
five wrong guesses.

### Why can a session not be revoked?

A session is a signed JSON Web Token, valid for eight hours. Nothing is stored and nothing is read
to check it.

That means a token already issued keeps working until it expires, even after the password changes.
Setting a password retires every outstanding one-time token — links and codes — but not a session.

The alternative is a stored session, or refresh tokens with a revocation list: a database read on
every request, or a second moving part. For a proof of concept the stateless token is the honest
choice, and the eight hours is what bounds the damage. In anything holding real accounts this is
the first thing to change, and it is written down in the security model rather than left to be
discovered.

### Why a unique index instead of reading first?

Liking inserts a row keyed on `(PostId, UserId)`. If PostgreSQL refuses with `23505`, that becomes
a 409.

Read-then-write reads more naturally and is wrong. Between the read and the write another request
can insert the same row: two people pressing like at the same moment both find nothing and both
insert — and the second insert fails anyway, now as an unhandled 500 rather than a considered 409.

The database is the only thing that can decide uniqueness under concurrency, because it is the
only thing holding the lock. The entity checks its loaded collection too, so the rule is stated
where a reader looks for it and a test can exercise it without a database. The index is what makes
it true when two requests arrive together.

The same reasoning covers usernames and email addresses, which are `citext` with unique indexes,
so `Alice` is refused once `alice` exists.

### Why is a 403 a 403 and not a 404?

Editing somebody else's discussion returns 403, not 404. The discussion exists; pretending
otherwise would be a different untruth, and the caller can already see it by reading the list.

There are two shapes of 403, and the difference is useful. `Forbidden` with no detail comes from
the authorization policy — you lack the role. `Not allowed.` with a sentence comes from a rule of
the forum — you have the role and this still is not yours.

### What is deliberately not built?

| Not built | Why |
| --- | --- |
| Refresh tokens and revocation | Eight hours bounds a stolen session; a store is the next step |
| Authenticator apps or SMS | Email two-factor already demonstrates the gate |
| Deleting somebody else's content | Moderation here is flagging, which is what the brief asks for |
| End-to-end browser tests | Component tests and API integration tests meet in the middle |
| Horizontal scaling | Migrations run at startup and rate limits are in memory |
| Audit log | Nothing here has to be reconstructed after the fact |

Each of these is in [decisions and trade-offs](explanation/decisions-and-trade-offs.md) or
[the security model](explanation/security-model.md), with the cost written next to it.

### How would you take this to production?

In order:

1. A real secret store for `Jwt:SigningKey` and `Tokens:Pepper`, and no seeding.
2. Sessions that can be revoked, which means a store and a read on each request.
3. Migrations as a deployment step rather than at startup, so more than one instance is safe.
4. Rate limiting held somewhere shared, for the same reason.
5. TLS terminated in front, and `Strict-Transport-Security` added there.
6. A real mail provider in place of Mailpit.

Nothing on that list needs the code rearranged. That is the point of the boundaries.

## If something goes wrong on the day

| Symptom | Do this |
| --- | --- |
| `web` never turns healthy | `docker compose logs api` — the API is usually waiting on the database |
| Mailpit is empty | The API cannot reach it; check `docker compose ps` |
| A sign-in code is refused | It expired, or five wrong guesses spent the attempt. Start again from the password |
| Postman 429s | The API is running in Production. Development raises the limits |
| The forum shows nothing | The database was seeded before the API could reach it, or `docker compose down -v` and start again |

## Related

- [The tutorial](tutorials/01-run-the-forum-and-log-in-with-2fa.md), if the audience wants to
  follow along themselves
- [Architecture](explanation/architecture.md)
- [QA test matrix](reference/qa-test-matrix.md)
