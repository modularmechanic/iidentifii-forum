# iiDENTIFii Forum

A self-hosted forum where engineers, partners and internal teams post questions, share integration
knowledge, and let moderators flag content that is misleading or false.

The API is the product. The web client is one consumer of it; a third party integrating
programmatically uses the same documented endpoints.

## Quick start

You need Docker Desktop, running. Nothing else.

```bash
git clone https://github.com/modularmechanic/iidentifii-forum.git
cd iidentifii-forum
docker compose up -d --build
```

Four containers start: PostgreSQL, a mail catcher, the API and the web client. The first build
compiles both applications, so give it a few minutes; `docker compose ps` shows `web` as `healthy`
when the whole path works.

| Address | What it is |
| --- | --- |
| http://localhost:8080 | The forum |
| http://localhost:5080 | The API |
| http://localhost:5080/scalar | Browsable API reference |
| http://localhost:5080/health | Liveness check |
| http://localhost:8025 | Mailpit, holding every email the API sends |

Every port is bound to `127.0.0.1`, so nothing is reachable from another machine.

Sign in as `alice` with `Password123!`. The password alone does not sign you in: the API emails a
six-digit code, which you read at http://localhost:8025. The
[tutorial](docs/tutorials/01-run-the-forum-and-log-in-with-2fa.md) walks the whole thing.

An empty database is filled on first start with eleven accounts, twenty discussions, replies,
likes and moderator flags. Every seeded account uses `Password123!`, and the API refuses to seed
outside Development. See [test accounts](docs/reference/test-accounts.md).

Stop it with `docker compose down`, or `docker compose down -v` to throw the database away too.

## Run it without containers

Needs the .NET SDK 10 and Node.js 24 as well as Docker.

```bash
docker compose up -d db mailpit                # PostgreSQL and the mail catcher
dotnet run --project backend/src/Forum.Api     # API on http://localhost:5000
npm install --prefix frontend                  # once
npm run dev --prefix frontend                  # web on http://localhost:5173
```

PostgreSQL is published on **55432** either way, so it cannot collide with one already installed on
your machine. Full steps in [run it for development](docs/how-to/run-for-development.md).

## Test it

```bash
dotnet test backend/Forum.slnx      # 155 unit and integration tests

cd frontend
npm ci
npm run lint
npm run format:check
npm run test:run                    # 113 component tests
npm run build
```

The integration tests start throwaway PostgreSQL containers of their own, so Docker must be
running. Continuous integration runs exactly these on every pull request.

To exercise the API directly, import
[`docs/postman/`](docs/postman/iidentifii-forum.postman_collection.json) and run the collection:
82 requests including every failure case, repeatable without editing anything. See
[test the API with Postman](docs/how-to/test-the-api-with-postman.md).

## Where things are

| Path | Contains |
| --- | --- |
| `backend/` | ASP.NET Core API, PostgreSQL persistence, tests, and the API image |
| `frontend/` | React and TypeScript client, and the nginx image that serves it |
| `docs/` | Tutorials, how-to guides, reference and explanation |
| `docker-compose.yml` | The whole stack |
| `GUIDELINES.md` | Engineering conventions |
| `CLAUDE.md` | Contribution workflow |

## Where to read next

| You are | Start here |
| --- | --- |
| Seeing this for the first time | [Run the forum and log in with a one-time code](docs/tutorials/01-run-the-forum-and-log-in-with-2fa.md) |
| Presenting or assessing it | [Walkthrough](docs/walkthrough.md) |
| Integrating against the API | [API endpoints](docs/reference/api-endpoints.md), [error codes](docs/reference/error-codes.md) |
| Testing it | [QA test matrix](docs/reference/qa-test-matrix.md), [test accounts](docs/reference/test-accounts.md) |
| Building on it | [Architecture](docs/explanation/architecture.md), [engineering guidelines](GUIDELINES.md) |
| Asking why | [Decisions and trade-offs](docs/explanation/decisions-and-trade-offs.md), [security model](docs/explanation/security-model.md) |
| Changing a setting | [Configuration](docs/reference/configuration.md) |

The full index is [`docs/README.md`](docs/README.md).

## Status

Built as vertical slices, one pull request per slice. See the
[open issues](https://github.com/modularmechanic/iidentifii-forum/issues) for what is planned and
what has landed.

This is a proof of concept. What it deliberately does not do — revocable sessions, an
authenticator app, horizontal scaling — is written down with the reason and the cost in
[decisions and trade-offs](docs/explanation/decisions-and-trade-offs.md) and
[the security model](docs/explanation/security-model.md).
