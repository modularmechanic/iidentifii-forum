# Configuration

Who this is for: anyone running the API, in containers or directly.
What you'll get: every setting the API reads, what it defaults to, and what happens when it is
wrong.

Settings come from `appsettings.json`, then `appsettings.{Environment}.json`, then environment
variables. The last one wins.

In an environment variable, a colon becomes a double underscore: `Jwt:SigningKey` is
`Jwt__SigningKey`.

## Environment

| Variable | Default | Notes |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `dotnet run` uses `Development` through `launchSettings.json`; the compose file sets it explicitly |
| `ASPNETCORE_HTTP_PORTS` | `8080` in the image | Which port the API listens on inside the container |

Development changes three things: the database may be seeded, the Scalar reference is mapped at
`/scalar`, and `appsettings.Development.json` raises the request limits.

## Database

| Setting | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:Forum` | `Host=localhost;Port=55432;Database=forum;Username=forum;Password=forum` | Missing entirely, the API refuses to start |
| `Database:SeedOnStartup` | `false` | Sample content, and only into an empty database |

The migration is applied on every start, whatever the environment. That suits one instance; a
fleet would apply migrations as a separate step instead.

Seeding needs **both** `Database:SeedOnStartup` and the Development environment, because every
seeded account shares one published password. The two are combined at the call site in
`Program.cs`, so setting the flag in Production seeds nothing — and says nothing about having
skipped it. `InitialiseDatabaseAsync` carries a second guard that logs the refusal, but nothing in
the running application reaches it; a test calls the method directly to exercise it.

## Where the forum is reachable

| Setting | Default | Notes |
| --- | --- | --- |
| `App:PublicUrl` | `http://localhost:5173` | Required, and must be a URL |

This is where a person's browser reaches the forum, which is not always where the API listens. The
confirmation and password-reset emails build their links from it, so a wrong value produces links
that go nowhere. The compose file sets it to `http://localhost:8080`, the address the web
container serves.

## Email

| Setting | Default | Notes |
| --- | --- | --- |
| `Email:Enabled` | `true` | `false` writes each message to the log instead of sending it |
| `Email:Host` | `localhost` | Required |
| `Email:Port` | `1025` | 1 to 65535 |
| `Email:FromAddress` | `no-reply@forum.local` | Required, and must be an address |
| `Email:FromName` | `iiDENTIFii Forum` | Required |
| `Email:Username` | none | Left empty for a local mail catcher |
| `Email:Password` | none | |
| `Email:UseStartTls` | `false` | |

`Email:Enabled=false` is the way to run with no mail server at all: links and codes appear in the
API log, which is enough to complete every journey by hand.

## Secrets

| Setting | Default | Notes |
| --- | --- | --- |
| `Jwt:SigningKey` | none | **Required**, at least 32 characters |
| `Jwt:Issuer` | `iidentifii-forum` | Required |
| `Jwt:Audience` | `iidentifii-forum` | Required |
| `Jwt:Lifetime` | `08:00:00` | How long a session lasts |
| `Tokens:Pepper` | none | **Required**, at least 32 characters |

Both are validated when the application starts, not when a request first needs them. Start the API
in Production without a signing key and it stops immediately with
`OptionsValidationException: ... 'SigningKey' with the error: 'The SigningKey field is required.'`
rather than running with a weak one.

`appsettings.Development.json` carries a published placeholder for each, so a developer can start
without setting anything up. Those values are in the Development file only: they are not loaded in
any other environment, which is why the application there has nothing until you supply it.

Anyone holding the signing key can mint a session for any member, and anyone holding the pepper
can forge a one-time token given the database. Both belong in a secret store.

To supply your own to the containerised stack:

```bash
FORUM_JWT_SIGNING_KEY='...at least 32 characters...' \
FORUM_TOKEN_PEPPER='...at least 32 characters...' \
docker compose up -d
```

The compose file reads those two variables and falls back to the published development values when
they are not set.

## One-time secrets

| Setting | Default | Notes |
| --- | --- | --- |
| `Tokens:LinkLifetime` | `01:00:00` | Confirmation and password-reset links |
| `Tokens:CodeLifetime` | `00:10:00` | Emailed sign-in codes |
| `Tokens:ResendCooldown` | `00:01:00` | Between two emails of the same kind to the same person |

Five wrong sign-in codes spend the challenge. That limit is a constant on the entity, not a
setting.

## Rate limits

| Setting | Default | Development | Notes |
| --- | --- | --- | --- |
| `RateLimiting:GlobalPermitsPerMinute` | `120` | `600` | Every request, partitioned by caller address |
| `RateLimiting:AuthenticationPermitsPerMinute` | `10` | `100` | The `/api/v1/auth` routes, which each check a secret or send mail |

`GET /api/v1/auth/me` opts out of both limits: it is a session check the client makes on every
page load, and counting it would spend the account budget on nothing.

A refusal is 429 with `Retry-After`. Development raises both so a Postman run is not throttled by
its own traffic; the production values, and the 429 they produce, are covered by `RateLimitTests`.

## Ports

| Path | Web | API | PostgreSQL | Mailpit |
| --- | --- | --- | --- | --- |
| `docker compose up` | 8080 | 5080 | 55432 | 8025 |
| Run directly | 5173 | 5000 | 55432 | 8025 |

PostgreSQL is published on 55432 so it cannot collide with one already installed on the host.
Every published port is bound to `127.0.0.1`, so nothing is reachable from another machine.

## The web container

The web image is nginx serving the built client. It reads no configuration of its own: the API
address is fixed in `frontend/nginx.conf`, which forwards `/api` and `/health` to `api:8080` on the
compose network. Changing where the API lives means changing that file and rebuilding the image.

Every response from it carries a content security policy, `X-Content-Type-Options`,
`X-Frame-Options`, `Referrer-Policy` and `Permissions-Policy`. `Strict-Transport-Security` is
deliberately absent: this listens on plain HTTP behind whatever terminates TLS, and the header
belongs there.
