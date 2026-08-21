# Decisions and trade-offs

Who this is for: reviewers, and anyone who would have built this differently and wants to know
whether the difference was considered.
What you'll get: each decision that shaped the code, the alternative it beat, and what it costs.

Nothing here is free. A decision without a cost written next to it has usually not been made.

## Clean Architecture, without a mediator

Four projects, dependencies pointing inwards, enforced by the references.

**Not chosen: MediatR or a similar in-process bus.** A mediator earns its place when there are
cross-cutting behaviours to insert around every request — validation, logging, transactions,
retries — and enough handlers that wiring them by hand is worse than the indirection.

Here there are four services and a handful of methods each. A mediator would replace a call you
can follow in an editor with a message you have to search for, and would add a package, a
pipeline, and a request class per operation.

**What it costs:** cross-cutting concerns have to be arranged another way. Validation is
attributes on the request records, failures are translated in one exception handler, and rate
limiting is middleware. If the number of operations tripled and each needed the same three things
wrapped around it, a mediator would start to pay.

## Rules on entities, not in services

`Post.Like` refuses your own discussion. `Post.EnsureOwnedBy` refuses anybody but the author.
`Post.Flag` refuses a member who is not a moderator.

**Not chosen: checking in the service, or in the controller.** Both work, and both create a second
place the rule lives. The endpoint carries an authorization policy as well — so an unauthorised
caller is turned away before any work happens — but the policy is an optimisation, not the rule.
Delete the policy and the entity still refuses.

**What it costs:** the entity has to be loaded before the rule can run, so refusing a flag from a
non-moderator reads the discussion first. That is one query to say no, and it is worth it.

## Source-generated JSON

Every payload type is registered on `ForumJsonContext`, and
`JsonSerializerIsReflectionEnabledByDefault` is off.

**Not chosen: reflection-based serialisation, the default.** It needs no registration at all.

Turning it off makes a forgotten type a loud failure instead of a silent fallback, removes
reflection from the request path, and leaves the application ready for ahead-of-time compilation
without a rewrite.

**What it costs:** one attribute per type, and a confusing error the first time somebody adds a
DTO and forgets. That is a real cost, paid once per type, in return for the request path having no
reflection in it at all.

## Two steps to sign in, with the code sent by email

A password identifies the account and causes a six-digit code to be emailed. The code returns the
session.

**Not chosen: an authenticator app (TOTP).** It is stronger — nothing is transmitted — but it
needs enrolment, a QR code, a secret to store, and recovery codes for the phone that broke. That
is a slice of its own.

**Not chosen: SMS.** It needs an account with a provider, money, and a phone number per member,
and it is the weakest of the three.

Email is the one channel every account already has, since the address is confirmed at
registration. It demonstrates the gate the assessment asks for without adding a dependency.

**What it costs:** the security of the second factor is the security of the inbox. Somebody who
has taken over the mailbox has both halves. That is the honest limit of email two-factor, and it
is why the code is six digits with five attempts and a ten-minute life rather than something
longer-lived.

## A session that cannot be revoked

A session is a signed JSON Web Token, valid for eight hours. Nothing is stored, and nothing is
looked up to check it.

**Not chosen: a stored session, or a short access token with refresh tokens and a revocation
list.** Either allows a session to be struck off immediately.

**What it costs:** a token already issued keeps working until it expires, even after the password
is changed. Setting a password retires every outstanding *one-time* token, but not a session. The
window is eight hours.

For a proof of concept the stateless token is the honest choice: it adds no database read to every
request and no second moving part. In anything holding real accounts, this is the first thing to
change. The full reasoning is in [the security model](security-model.md).

## A unique index rather than reading first

Liking a discussion inserts a row whose primary key is `(PostId, UserId)`. Registering inserts a
user whose username has a unique index. If the database refuses with `23505`, that becomes a 409.

**Not chosen: read, decide, then write.** It reads more naturally, and it is wrong. Between the
read and the write, another request can insert the same row. Two people pressing like at the same
moment would both find no existing like and both insert one — except the second insert fails
anyway, now as an unhandled 500 instead of a considered 409.

The database is the only thing that can decide uniqueness under concurrency, because it is the
only thing holding the lock.

**What it costs:** the happy path takes an exception in the unhappy case, and exceptions are not
cheap. At this scale that is invisible; if likes were the hot path, an upsert that reports whether
it inserted would be the next step.

The entity *also* checks its loaded collection, so the rule is stated where a reader looks for it
and a test can exercise it without a database. The index is what makes it true under concurrency.

## Case-insensitive uniqueness in the database

`Username` and `Email` are `citext`, with unique indexes on both.

**Not chosen: normalising to lower case in the application, or comparing with `ILIKE`.** The first
means every write has to remember; the second cannot use the unique index.

With `citext`, `Alice` is refused once `alice` exists, and a lookup by either still uses the index.

**What it costs:** a PostgreSQL extension, which ties the schema to PostgreSQL. It already is
tied, through the provider and through the migration.

## Integration tests against a real database

The API tests host the application against a throwaway PostgreSQL container, applying the real
migration and the real seeder, and exercise it over HTTP.

**Not chosen: the in-memory provider, or SQLite.** Both are faster and neither is PostgreSQL.
`citext`, cascade behaviour and the `23505` translation are exactly the things that would go
untested, and they are exactly the things this design leans on.

**What it costs:** Docker has to be running, and the suite takes seconds rather than
milliseconds. The trade is that a green suite means something.

Time is the exception: it comes from a `TimeProvider` the test host supplies, so expiry and
cooldowns are exercised by moving a clock rather than by waiting.

## Filters and ordering in the address bar

The page number, the sort, the order and every filter are read from the query string rather than
held in component state.

**Not chosen: local state.** It is less code, until somebody wants to share a filtered list, or
presses the back button, or reloads.

**What it costs:** every change to the view is a navigation, and a state that would have been a
`useState` is now parsed and serialised. In return, a view is a link.

## Vertical slices, one issue at a time

Each slice delivers something a person can do, across the backend, the frontend, the tests and the
documentation together, in one pull request.

**Not chosen: a layer at a time — all the entities, then all the endpoints, then the interface.**
It looks tidier on a plan and produces nothing usable until the end, so nothing can be reviewed by
using it.

**What it costs:** a slice sometimes touches a file another slice is also touching, and a slice
built on an unmerged one is based on that branch rather than `main`. Continuous integration runs on
every pull request whatever it is based on, which is what makes that safe.

## Migrations applied at startup

The API brings the schema up to date when it starts.

**Not chosen: a separate migration step.** That is what a fleet needs, because several instances
starting together would otherwise migrate at once.

For one instance, applying at startup means there is no way to run the application against a stale
schema, and no second command to forget.

**What it costs:** it does not survive being scaled out. If this ever ran on more than one node,
this is the change to make alongside the session store.

## Seeded accounts with a published password

An empty database is filled with eleven accounts sharing `Password123!`, all confirmed.

**Not chosen: an empty forum.** An assessor would have to register, confirm, and write content
before there was anything to look at, which is several minutes spent on nothing.

**What it costs:** a published password. Seeding needs the Development environment *and* the
setting, so the content cannot reach a real deployment by accident — though a Production start with
the setting left on says nothing about having skipped it.

## Related

- [Architecture](architecture.md) — the shape the decisions produced
- [Security model](security-model.md) — the decisions about secrets, sessions and disclosure
- [Frontend conventions](frontend-conventions.md) — the decisions about the client
