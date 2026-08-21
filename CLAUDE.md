# Working agreements

Instructions for automated contributors working in this repository. Read `GUIDELINES.md` first: it holds the conventions. This file holds the workflow.

## Ground rules

- Never commit to `main`. Every change reaches `main` through a pull request.
- One issue per slice, one branch per issue, named `feature/clem/<issue>-<feature>-<short-description>`.
- A slice is vertical: backend, frontend, tests and documentation land together, and the result runs.
- Do not mention automated tooling in commit messages, pull requests or code comments.

## Before opening a pull request

1. `dotnet test backend/Forum.slnx` passes.
2. `npm run lint && npm run test -- --run && npm run build` passes in `frontend/`.
3. Run the local review pass and fix what it finds.
4. Check nothing secret is staged: keys, connection strings, tokens.

## Pull request flow

1. Open the pull request with `Closes #<issue>` and fill in the template.
2. Wait for continuous integration and the automated review.
3. Address review comments by changing the code and pushing. Do not reply in the comment threads; let the reviewer resolve them.
4. Anything deliberately not applied goes in the pull request description with a reason.
5. Wait for the maintainer to test the slice by hand and respond.
6. Merge only once the checks pass, comments are resolved or answered, and the maintainer has approved.

## Definition of done

A slice is done when someone else can run it from the documentation, exercise it in the browser and in Postman, see it covered by tests, and read why it was built that way.

## Local commands

```bash
docker compose up -d db mailpit     # dependencies
dotnet run --project backend/src/Forum.Api   # API on http://localhost:5000
npm run dev --prefix frontend                # web on http://localhost:5173
```

Mail sent by the API is captured by Mailpit at http://localhost:8025.
