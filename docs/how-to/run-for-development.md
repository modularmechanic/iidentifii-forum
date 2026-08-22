# Run it for development

Who this is for: anyone who wants the forum running on their own machine.
What you'll get: the API, the web client and their dependencies, all running locally.

This is the path for changing the code: both applications run from source, with hot reload on the
client. To run the whole thing in containers instead — one command, no SDKs — follow
[the tutorial](../tutorials/01-run-the-forum-and-log-in-with-2fa.md).

## Before you start

- Docker Desktop, running
- .NET SDK 10
- Node.js 24

## Steps

1. Start the dependencies.

   ```bash
   docker compose up -d db mailpit
   ```

   Both report `healthy` within a few seconds. Check with `docker compose ps`.

   PostgreSQL is published on **55432**, not the usual 5432, so it cannot collide with a
   PostgreSQL already installed on your machine.

2. Start the API.

   ```bash
   dotnet run --project backend/src/Forum.Api
   ```

   On first run it applies the migration and fills the database with sample content. It listens on
   http://localhost:5000. Open http://localhost:5000/scalar to browse the API.

3. Start the web client.

   ```bash
   npm install --prefix frontend
   npm run dev --prefix frontend
   ```

   Open http://localhost:5173. The browser only ever talks to this address; the dev server
   forwards `/api` to the API, so there is no cross-origin setup.

## What you get

| Address | What it is |
| --- | --- |
| http://localhost:5173 | The forum |
| http://localhost:5000/scalar | API reference |
| http://localhost:5000/health | Liveness check |
| http://localhost:8025 | Mailpit, holding every email the API sends |

## Sample content

Seeding runs only when the database has no users, so restarting never duplicates it.

| Account | Role |
| --- | --- |
| `alice`, `bob`, `carol` | Member, and the authors of the sample discussions |
| `dave`, `erin`, `frank`, `grace`, `heidi`, `ivan`, `judy` | Member; they supply the likes and replies |
| `mod` | Moderator |

Every seeded account uses the password `Password123!` and is already verified. These accounts
exist for development and assessment only.

## Starting over

```bash
docker compose down -v
docker compose up -d db mailpit
```

This deletes the database volume, so the next API start migrates and seeds from scratch.
