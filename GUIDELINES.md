# Engineering guidelines

Who this is for: anyone writing code in this repository.
What you'll get: the conventions a change is reviewed against, and the reasoning behind them.

## Principles

Keep it simple, don't repeat yourself, and don't build what nothing asks for yet. An abstraction earns its place when a second caller needs it or a test has to replace it. Prefer deleting code to adding it.

## Backend (C#, ASP.NET Core)

### Layering

Dependencies point inwards, and the compiler enforces it.

| Project | Knows about | Holds |
| --- | --- | --- |
| `Forum.Domain` | nothing | Entities and the rules that must always hold |
| `Forum.Application` | Domain | Services, DTOs, and the seams the outer layers implement |
| `Forum.Infrastructure` | Application, Domain | Entity Framework, email, token signing |
| `Forum.Api` | all of the above | Controllers, pipeline, configuration |

Rules that must always hold live on the entity, not in a service. A service orchestrates; it does not restate an invariant. Controllers stay thin: bind, call one service, return.

### Style

- File-scoped namespaces, one public type per file, `sealed` unless designed for inheritance.
- Records for DTOs and requests; primary constructors for dependencies.
- `async` all the way down, and every method touching the database takes a `CancellationToken`.
- Nullable reference types on; warnings are errors.
- No reflection in the request or response path: payload types belong to `ForumJsonContext`.
- An interface needs a reason: a layer boundary, or a second implementation. `IEmailSender` has both.

### Errors

Failures are RFC 7807 problem details. Domain rule violations map to status codes in one place. Unexpected exceptions return a trace identifier; the detail stays in the logs.

### Data access

One query per request path. Projections are shared between list and detail so a change cannot drift. Anything filtered or sorted has an index behind it. Uniqueness is enforced by the database, and the violation is translated to a conflict rather than caught by a prior read.

### Comments

Explain intent, not mechanics. A summary belongs on anything whose reason is not obvious from its name. Delete a comment that only restates the next line.

## Frontend (React, TypeScript)

The conventions below are applied consistently across the client, so any file reads the same way
as the last one.

### Structure

- `components/pages/` mirrors the routes; `components/common/ui/` holds shared elements.
- `domains/` holds models, services and operations per subject area. Components never call the network directly: a container calls a service.
- `infra/` holds the fetch wrapper and session handling.

### Components

- Function declarations, not arrow constants, so they hoist and read top-down.
- Parent first, children below it in the same file. Default export at the bottom.
- Props typed as an interface in the file's `Types` region and destructured in the body.
- Containers hold data and state; presenters take props and render.
- Static values live outside the component, otherwise they are rebuilt on every render.

### Types

- No `enum`: a constant object plus a derived union, which also satisfies the compiler settings this project uses.
- `import type` for type-only imports.
- `fetch*` names perform input and output; `get*` names do not; `is*` names return a predicate.

### Styling

Tailwind utilities only. The palette lives in the theme block in `index.css`, so a colour is never hard-coded in a component.

## Testing

Test the rule where it lives: invariants as unit tests against the domain, behaviour across the wire as integration tests against the real database, and rendering as component tests. Every bug fixed gains a test that would have caught it. A test that passes whatever the code does is worse than no test.

## Git

- Branches: `feature/clem/<issue>-<feature>-<short-description>`.
- Commits: imperative subject under seventy-two characters, `type(scope): what changed`, with a body listing each change.
- One issue per slice, one pull request per issue, and nothing merges into `main` directly.
- A pull request merges when the checks pass, review comments are resolved or answered in the description, and the slice has been used by hand.

## Documentation

Pages follow the [Diátaxis](https://diataxis.fr) split: tutorials teach, how-to guides solve one task, reference states facts, explanation gives reasons. Every page opens with who it is for and what the reader will get. Plain language, active voice, short sentences, examples before theory.
