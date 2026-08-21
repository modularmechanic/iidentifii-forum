# iiDENTIFii Forum

A self-hosted forum where engineers, partners and internal teams post questions, share integration
knowledge, and let moderators flag content that is misleading or false.

The API is the product. The web client is one consumer of it; a third party integrating
programmatically uses the same documented endpoints.

## What is here

| Path | Contains |
| --- | --- |
| `backend/` | ASP.NET Core API, PostgreSQL persistence, tests |
| `frontend/` | React and TypeScript web client |
| `docs/` | Tutorials, how-to guides, reference and explanation |
| `GUIDELINES.md` | Engineering conventions |
| `CLAUDE.md` | Contribution workflow |

Full documentation is in [`docs/`](docs/README.md).

## Prerequisites

- Docker Desktop, running
- .NET SDK 10
- Node.js 24

## Run it

```bash
docker compose up -d db mailpit                # PostgreSQL and a local mail server
dotnet run --project backend/src/Forum.Api     # API on http://localhost:5000
npm install --prefix frontend                  # once
npm run dev --prefix frontend                  # web on http://localhost:5173
```

The API fills an empty database with sample content on first run: three members, one moderator and
twenty discussions with replies, likes and moderator flags. Every seeded account uses the password
`Password123!`. See [run it for development](docs/how-to/run-for-development.md).

| Address | What it is |
| --- | --- |
| http://localhost:5173 | The forum |
| http://localhost:5000/health | Liveness check |
| http://localhost:5000/scalar | API reference, in Development |
| http://localhost:8025 | Mailpit, holding every email the API sends |

PostgreSQL is published on **55432** so it cannot collide with one already installed on your machine.

## Test it

```bash
dotnet test backend/Forum.slnx     # unit and integration tests
npm run test:run --prefix frontend # component tests
npm run lint --prefix frontend     # static analysis
```

## Test the API directly

Import [`docs/postman/iidentifii-forum.postman_collection.json`](docs/postman/iidentifii-forum.postman_collection.json) and the
environment beside it, then run the collection. Every request asserts its own outcome, including
the failure cases, and it can be run repeatedly without editing anything.

## Status

Being built as vertical slices, one pull request per slice. See the
[open issues](https://github.com/modularmechanic/iidentifii-forum/issues) for what is planned and
what has landed.
