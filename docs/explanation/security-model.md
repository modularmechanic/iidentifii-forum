# Security model

Who this is for: developers and reviewers who want to know what protects an account here, and
what deliberately does not.
What you'll get: the choices made about secrets, sessions and disclosure, each with the reason
and the cost.

This is a proof of concept. The point of writing the model down is that the gaps are chosen
rather than overlooked.

## Secrets at rest

**Passwords** are hashed with `PasswordHasher<User>`, which is PBKDF2 with a per-password salt and
a work factor. It is deliberately slow, because a password is short and memorable and would
otherwise be guessable offline.

**One-time secrets** — confirmation links, sign-in codes, reset links — are stored as a keyed hash
(HMAC-SHA256) under a pepper held in configuration, and compared with
`CryptographicOperations.FixedTimeEquals`. The database on its own cannot be used to forge one,
because the key is not in it.

A keyed hash is right here where a slow hash is right for passwords: these secrets are long or
short-lived and randomly generated, so there is nothing worth attacking offline. Spending PBKDF2
on every code check would only slow the application down.

The pepper and the signing key are required at startup: without them the application refuses to
start rather than running weakened.

Neither is in the repository. A secret committed to a public repository is a secret everybody
has, and a signing key in particular would let anyone mint a session for any deployment that
loaded it. In Development the application generates both on the way up, so `dotnet run` needs no
setup; they last for that run only, which means restarting the API signs everybody out and
invalidates any confirmation or reset link not yet used. Set `Jwt:SigningKey` and `Tokens:Pepper`
through user secrets or the environment to keep them steady.

Any other environment generates nothing. It must be given both, and stops with
`OptionsValidationException` naming the missing field if it is not.

## Sessions

A session is a JSON Web Token, HS256, valid for eight hours, carrying the member's identifier,
username and role. It is signed rather than stored: nothing is looked up to check it.

**This means a session cannot be revoked.** Setting a new password retires every outstanding
one-time token, but a session token already issued keeps working until it expires. Somebody who
took a token still holds it for up to eight hours.

The alternative is a stored session, or refresh tokens with a short-lived access token and a
revocation list — a database read on every request, or a second moving part. For a proof of
concept the stateless token is the honest choice, and the cost is written here rather than
implied. In something carrying real accounts, this is the first thing to change.

## Where the token is kept in the browser

The token is kept in `localStorage`. Reloading the page therefore does not sign the reader out,
which is what anybody expects from a forum.

The cost is that any script running on the page can read it. What limits that today is that the
application loads no third-party scripts at all: everything it runs is built from this repository.
The web container answers every request with `script-src 'self'`, which narrows where a script may
come from to this origin, and names only the two hosts Google Fonts serves styles and fonts from.

That is defence in depth, not a guarantee. A policy about *origins* says nothing about what a
script already running on this origin may do, so anything that managed to get itself served from
here could still read the token. The header is added by nginx and so covers the built client; the
development server sends no such header.

A cookie marked `HttpOnly` and `SameSite` would place the token out of reach of scripts entirely,
at the price of a cross-site request forgery defence on every write and a sign-in flow that no
longer works from Postman the way it does now.

For a proof of concept examined through both a browser and an API client, the readable token is
the useful trade. It is also why the session is confirmed against `/auth/me` on load rather than
trusted: a stale or tampered token fails there and the reader is signed out.

## What the API refuses to disclose

**Whether an address is registered.** `resend-verification` and `forgot-password` answer 202
whatever address they are given, and apply their cooldown silently. A caller cannot tell a
registered address from an unregistered one by the status, the body or the timing.

The timing took work. An address with an account causes a message; one without causes nothing —
and waiting for a mail server is long enough to measure, which would have given the answer away
however carefully the response was worded. Delivery is therefore handed to a queue and happens
after the response, so both answers are returned at the same speed.

Registration is the deliberate exception: a taken *username* returns 409, because the person
typing it has to pick a different one. Usernames are shown on every post anyway, so nothing is
disclosed that browsing does not already show. Addresses are never shown.

**Which half of a sign-in was wrong.** An unknown username and a wrong password return the same
401 and the same wording. A hash is verified even when no account matches, so the two cannot be
told apart by how long the reply takes.

**What went wrong inside.** An unexpected failure returns a fixed sentence and a trace identifier.
The detail stays in the log, where the reader of the log is trusted and the caller is not.

## Limits

Every `/auth/*` route sits behind a tighter rate limit than the rest of the API, and the whole API
sits behind a per-address limit. `GET /auth/me` carries the ordinary allowance rather than the
tighter one the other `/auth/*` routes use: a page restoring a session calls it on every load, so
the sign-in budget would be spent on reading a session rather than starting one. It is metered all
the same, because it reaches the database, and no route that does should be free. Both answer 429 with `Retry-After`. Emails have their own
cooldown on top, so the limit on sending is not merely the limit on asking.

Guessing is bounded separately from rate limiting: five wrong codes spend a sign-in challenge
outright, because six digits is a small enough space that a slow walk through it would otherwise
succeed.

## Deliberately out of scope

| Not built | Why | What it would cost |
| --- | --- | --- |
| Refresh tokens and revocation | A session cannot be revoked; eight hours bounds it | A session store, and a read on every request |
| Authenticator apps or SMS | Email two-factor already demonstrates the gate | Another channel to enrol, verify and recover |
| Sign-in from another provider | The assessment asks for accounts held here | An external dependency for a proof of concept |
| Account lockout on repeated passwords | Rate limiting bounds the attempt rate | A way for anybody to lock anybody else out |
| Audit log | Nothing here needs to be reconstructed after the fact | A table, and a decision about retention |
