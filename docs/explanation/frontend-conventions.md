# Frontend conventions

Who this is for: anyone reading or writing code in `frontend/`.
What you'll get: why the client is arranged the way it is, with the cost of each choice.

The rules themselves are in [`GUIDELINES.md`](../../GUIDELINES.md). This page is the reasoning
behind them. They are applied consistently, so any file reads the same way as the last one — which
is the whole point, and also the cost, because consistency means occasionally writing something a
longer way than you would have chosen alone.

## Every file has the same skeleton

```text
/***** Constants *****/   values that do not change
/***** Types *****/       the props interface and anything local
/***** Components *****/  the exported component first, its children below it
/***** Functions *****/   helpers, prefixed with _ when private to the file
/***** Export default *****/
```

Reading top to bottom goes from what a file needs, to what it shows, to how it does it. You know
where to look before you open the file.

**What it costs:** banner comments in every file, and a rule to remember. It buys not having to
scan a file to find where the component actually starts.

## Components are declared, not assigned

```tsx
function PostsList(props: IProps) { … }
```

not `const PostsList = (props: IProps) => …`.

Declarations hoist, so the parent can be written above the children it uses and the file reads
top-down: the thing you came for is first, and the details are underneath. They also name
themselves in a stack trace, where an arrow assigned to a constant may not.

Arrow functions are for callbacks written inline in markup, and nothing else.

**What it costs:** nothing but the habit.

## Containers hold data, presenters take props

`PostsContainer` reads the criteria from the address, calls `PostService`, and hands the result to
`PostsList`. `PostsList` takes an array and renders it.

A presenter can be tested with nothing but an object — no network, no router, no query client.
That is why the component suite runs in milliseconds and why a failing component test means the
component changed rather than a URL moving.

**What it costs:** two files where one would do, and a props interface to keep in step.

## Components never call the network

A container calls a service; the service is the only place that knows a URL exists. `PostService`,
`CommentService`, `UserService` and `HealthService` each own their endpoints and their shapes.

Underneath, `infra/http/setup-http.ts` is the one place `fetch` is called. It attaches the session
header, reads a problem document out of a failed response, and throws an `HttpError` carrying the
status and any field errors. Every component that shows an error is showing the same type.

**What it costs:** a hop. Reading a component tells you *what* it asks for and not *where* from,
and you follow one import to find out.

## No `enum`

```ts
export const PostSorts = { CreatedAt: 'CreatedAt', LikeCount: 'LikeCount' } as const;
export type PostSort = (typeof PostSorts)[keyof typeof PostSorts];
```

A constant object with `as const` and a union derived from it gives the same thing, survives
compilation to plain JavaScript, and works under the compiler settings this project uses, which
forbid the alternative.

**What it costs:** two lines instead of one, and the pattern has to be recognised.

## State that belongs in the address bar lives in the address bar

`useListCriteria` reads the page, the sort, the order and every filter from the query string and
writes them back. Nothing about the visible list is held in component state.

So a filtered list is a link, the back button steps back through what you were looking at, and a
reload lands where you were.

**What it costs:** every change of view is a navigation, and values that would have been a
`useState` are parsed and serialised on each render. `useListCriteria` carries fourteen of the
frontend's tests, because that parsing is exactly where a mistake would hide.

## The session is confirmed, not trusted

`AuthProvider` restores a token from `localStorage` and then asks `/auth/me` whether it is still
good. A tampered or expired token fails there and the reader is signed out.

Reloading the page does not sign anybody out, which is what everybody expects from a forum. What
it costs, and why the token is not in an `HttpOnly` cookie instead, is in
[the security model](security-model.md).

## Colours come from one place

`index.css` declares a theme of named roles — `canvas`, `surface`, `ink`, `muted`, `line`,
`accent`, `danger`, `success` — and redeclares them under `prefers-color-scheme: dark`. Components
refer to the role: `text-muted`, `border-line`, `bg-canvas`.

A colour is therefore never written into a component, and dark mode follows from one block rather
than from a conditional in every file.

**What it costs:** naming a role before you can use a colour, which is harder than typing a hex
value and is the reason it works.

## Tailwind utilities only

No component stylesheets, no CSS modules, no styled components. The markup carries the styling, so
there is never a second file to open to find out what a class does.

**What it costs:** long `className` strings. The alternative is short markup and a stylesheet
nobody can safely delete from.

## What the linter and formatter settle

Single quotes in TypeScript, double quotes in markup attributes, and everything else oxlint and
Prettier decide. Neither is argued with in review, which keeps review about behaviour.

```bash
npm run lint
npm run format:check
```

Both run in continuous integration, so a formatting difference fails the build rather than filling
a review.

## Related

- [Engineering guidelines](../../GUIDELINES.md) — the rules, stated plainly
- [Architecture](architecture.md) — where the client sits
- [`frontend/README.md`](../../frontend/README.md) — how to run it
