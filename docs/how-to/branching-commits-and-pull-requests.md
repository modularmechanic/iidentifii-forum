# Branch, commit and open a pull request

Who this is for: anyone contributing a change to this repository.
What you'll get: the workflow a change is expected to follow, from branch name to merge.

The conventions a change is judged against are in [`GUIDELINES.md`](../../GUIDELINES.md). This page
is the sequence.

## One issue, one branch, one pull request

Work arrives as an issue describing a vertical slice: something a person can do, delivered across
the backend, the frontend, the tests and the documentation together. A slice that only reaches
halfway across is not a slice.

```bash
git switch main
git pull
git switch -c feature/clem/12-search-filter-by-keyword
```

The branch name is `feature/clem/<issue>-<feature>-<short-description>`. The issue number first,
so a branch says what it belongs to without being read.

Nothing is committed to `main`. Every change reaches it through a pull request.

## Commit as you go

```
type(scope): what changed

- each change, one line
- and the next
```

- The subject is imperative and under seventy-two characters: `feat(api): flag a discussion`, not
  `flagged a discussion` or `flagging`.
- `type` is one of `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `chore`.
- `scope` is the part of the system that moved: `api`, `web`, `db`, `docs`.
- The body lists what changed, one bullet each. It is read in a review, so it is written for one.

Keep a commit to one idea. A commit that renames a file *and* changes what it does is two commits
that have been stapled together.

## Before you open it

Run everything a reviewer would:

```bash
dotnet test backend/Forum.slnx

cd frontend
npm run lint
npm run format:check
npm run test -- --run
npm run build
```

Then check nothing secret is staged — keys, connection strings, tokens — and read your own diff
once, top to bottom.

## Open it

```bash
git push -u origin feature/clem/12-search-filter-by-keyword
gh pr create --fill
```

The template asks for a summary, the changes, how to test it, and a checklist. Fill it in; the
description is where a reviewer starts, and an empty one costs them the time you saved.

Put `Closes #12` in the body so the issue closes when the pull request merges.

A slice built on top of an unmerged one is based on that branch rather than `main`, and says so:

```bash
gh pr create --base feature/clem/11-search-backend
```

Continuous integration runs on every pull request whatever it is based on, so a stacked branch is
checked as thoroughly as one off `main`.

## While it is open

- Continuous integration runs both suites. Both must be green.
- Address review comments by changing the code and pushing. Let the reviewer resolve the threads.
- Anything you deliberately did not apply goes in the description with the reason, in the
  **Review findings not applied** table.
- The slice is tried by hand — in the browser and in Postman — before it merges.

## Merge

Merge when the checks pass, the comments are resolved or answered, and somebody has used the
slice. Then delete the branch.

## What continuous integration actually runs

| Job | Steps |
| --- | --- |
| Backend | Restore, build in Release, `dotnet test` |
| Frontend | `npm ci`, `npm run lint`, `npm run format:check`, `npm run test:run`, `npm run build` |
| Verify | Fails unless both jobs succeeded |

The backend job has Docker available, so the integration tests — which start a throwaway
PostgreSQL — run there too. The `verify` job exists because a *skipped* check satisfies a branch
protection rule while a *failed* one does not; it runs whatever happened and fails loudly.
