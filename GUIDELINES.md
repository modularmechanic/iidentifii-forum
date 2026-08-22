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

### Naming

- Components are `PascalCase`, and the file is named after the component it holds: `PostsList.tsx`.
- Files that are not components are `kebab-case`: `format-date.ts`, `setup-http.ts`.
- A module that groups related functions is an object named after itself: `PostService.ts` exports
  `PostService`.
- A function that performs input or output is named `fetch*`. A function that only computes is
  named `get*`. A function that answers yes or no is named `is*` or `has*`.
- Booleans read as a statement: `isOpen`, `hasFilters`, not `open` or `filters`.
- Constants that never change are `UPPER_SNAKE_CASE` at the top of the file.

### File layout

Every file is laid out in the same order, with a banner comment before each part that is present:

```text
/***** Constants *****/   values that do not change
/***** Types *****/       the props interface and anything local
/***** Components *****/  the exported component first, its children below it
/***** Functions *****/   helpers, prefixed with _ when private to the file
/***** Export default *****/
```

Reading top to bottom therefore goes from what a file needs, to what it shows, to how it does it.

### Components

- Declared with `function`, not assigned as an arrow constant. Declarations hoist, so a parent can
  be written above the children it uses, and they name themselves in a stack trace.
- The parent component comes first; the smaller components it uses are declared beneath it in the
  same file, so the file reads top-down.
- No return type annotation: a component always returns what React can render, so stating it adds
  nothing.
- The default export sits at the bottom, and its comment starts `Default component:`.
- No giant `return`. When a block of markup earns a name, it becomes a child component rather than
  a variable holding markup.
- Values that do not depend on props or state live outside the component. Inside, they would be
  rebuilt on every render.
- Arrow functions are for callbacks written inline in markup, and nothing else.

### Props

- Always typed, as an `interface` named `IProps` in the file's `Types` region. A child component's
  props take its own name: `IRowProps`.
- Taken as a single `props` parameter and destructured on the first line of the body, so the
  signature stays short and every value has one obvious place it appears.

### State and data

- A container holds the data and the state; a presenter takes props and renders. A presenter can
  be tested with nothing but an object.
- Components never call the network. A container calls a service, and the service is the only
  place that knows a URL exists.
- State that belongs in the address bar lives in the address bar. Filters, ordering and the page
  number are read from the query string, so a view can be shared and the back button works.
- `useState` is fine for one or two values. Beyond that, the state is one object, so every part of
  it changes together.

### Types

- No `enum`. A constant object with `as const` and a union derived from it gives the same thing,
  survives compilation to plain JavaScript, and the compiler settings this project uses forbid the
  alternative.
- `import type` for anything used only as a type, so the import disappears at build time.
- Nothing is typed `any`. Where a type genuinely is not known, it is `unknown` and narrowed.

### Styling

Tailwind utilities only. Colours come from the theme block in `index.css` and are referred to by
role — `text-muted`, `border-line`, `bg-canvas` — so a colour is never written into a component and
both light and dark follow from one place.

### Formatting

Single quotes in TypeScript, double quotes in markup attributes. The linter and formatter settle
everything else; neither is argued with in review.

## Testing

Test the rule where it lives: invariants as unit tests against the domain, behaviour across the wire as integration tests against the real database, and rendering as component tests. Every bug fixed gains a test that would have caught it. A test that passes whatever the code does is worse than no test.

## Git

- Branches: `feature/clem/<issue>-<feature>-<short-description>`.
- Commits: imperative subject under seventy-two characters, `type(scope): what changed`, with a body listing each change.
- One issue per slice, one pull request per issue, and nothing merges into `main` directly.
- A pull request merges when the checks pass, review comments are resolved or answered in the description, and the slice has been used by hand.

## Documentation

Pages follow the [Diátaxis](https://diataxis.fr) split: tutorials teach, how-to guides solve one task, reference states facts, explanation gives reasons. Every page opens with who it is for and what the reader will get. Plain language, active voice, short sentences, examples before theory.
