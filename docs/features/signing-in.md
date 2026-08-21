# Signing in

Who this is for: developers and testers working on sign-in, two-factor codes and password resets.
What you'll get: the two steps of signing in, what each refusal means, and why the design is
shaped this way.

## What it does

Signing in takes two steps. The password identifies the account and causes a six-digit code to be
emailed; the code is what actually returns a session. A password on its own never signs anybody
in, so a stolen password is not enough without the inbox behind it.

The address must be confirmed first. An account that has registered but not confirmed is refused
with 403 and pointed back at the confirmation email, which is a different problem from a wrong
password and says so.

A forgotten password is repaired by a link, not a code, because the reader is already in their
inbox and a link is one click rather than six digits typed twice.

## The journey

1. **Password.** `POST /auth/login` with a username and password. The reply carries a challenge
   identifier, the masked address the code went to (`b**@forum.local`) and when the code expires.
2. **Code.** `POST /auth/verify-2fa` with the challenge identifier and the six digits. The reply
   carries the session token, when it expires, and who the reader is now signed in as.
3. **Afterwards.** `GET /auth/me` answers who the caller is, so a page reloaded with a stored
   token can confirm the session is still good rather than assuming it.

## Endpoints

| Method | Path | Returns |
| --- | --- | --- |
| POST | `/api/v1/auth/login` | 200 with a challenge, and emails a code |
| POST | `/api/v1/auth/verify-2fa` | 200 with a session token |
| POST | `/api/v1/auth/forgot-password` | 202, always |
| POST | `/api/v1/auth/reset-password` | 200 when the link is good |
| GET | `/api/v1/auth/me` | 200 with the signed-in member |

## Timings and limits

| Thing | Value |
| --- | --- |
| Sign-in code | Six digits, good for 10 minutes |
| Reset link | Good for 1 hour |
| Wrong codes allowed | 5, after which the challenge is spent |
| Resend cooldown | 60 seconds |
| Session token | 8 hours |

## Decisions worth knowing

**A wrong username and a wrong password are the same answer.** Both return 401 with identical
wording. They also take about the same time: when no account matches, a hash is still verified
against a fixed value, so the reply cannot be timed to discover which usernames exist.

**Five wrong codes spend the challenge.** Six digits is a small space, and without a limit it
could simply be walked. Once the limit is reached the challenge is dead even if the next guess
would have been right — the test asserts exactly that, by submitting the correct code afterwards
and expecting a refusal.

**Resetting a password retires every outstanding token.** A confirmation link, a sign-in code and
another reset link all stop working the moment a new password is set. Somebody recovering from a
compromise should not leave a half-finished sign-in usable behind them.

**A session already issued survives a reset.** Session tokens are signed rather than stored, so
nothing is looked up to check them and nothing can be struck off. A token issued before the reset
stays valid until it expires. The trade-off is written up in
[the security model](../explanation/security-model.md).

**The masked address is confirmation, not disclosure.** `b**@forum.local` tells the reader which
inbox to open without publishing the address to anybody who guessed a password.

## Trying it by hand

1. Start the dependencies and both applications as in
   [run for development](../how-to/run-for-development.md).
2. Sign in at http://localhost:5173/login as `bob` with `Password123!`.
3. Read the code at http://localhost:8025 and type it in. The header now shows the member.
4. Reload the page. The session survives, because it is restored and then confirmed against
   `/auth/me`.
5. Sign out; the header returns to its signed-out state and the cached data is dropped.
6. Try `/forgot-password` for the same address, follow the emailed link, set a new password, and
   watch the old one stop working.
