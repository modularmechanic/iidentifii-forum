# QA test matrix

Who this is for: testers, and reviewers deciding whether a slice is covered.
What you'll get: what is tested, by which suite, and what is deliberately left to a person.

Counts below are what the suites report today: **155 backend tests**, **113 frontend tests** and
**82 Postman requests**. Every one of them passes on a clean checkout.

## Where a rule is tested

A rule is tested where it lives, once. It is not restated in three suites.

| Suite | Command | Tests what |
| --- | --- | --- |
| Domain unit | `dotnet test backend/Forum.slnx` | Invariants on the entities, with nothing external |
| Infrastructure | same | Seeding, against a real database |
| API integration | same | Behaviour over HTTP, against a throwaway PostgreSQL |
| Component | `npm run test:run --prefix frontend` | Rendering and interaction, with the service layer replaced |
| Postman | see [the how-to](../how-to/test-the-api-with-postman.md) | The API as an integrator meets it, including every failure |

The integration tests start throwaway PostgreSQL containers of their own and remove them
afterwards. They do not touch the database from `docker compose`, so a test run cannot spoil what
you were looking at in the browser.

## Backend

| Area | File | Tests |
| --- | --- | --- |
| Discussion rules | `Domain/PostTests.cs` | 13 |
| Reply rules | `Domain/CommentTests.cs` | 4 |
| Account rules | `Domain/UserTests.cs` | 6 |
| One-time token rules | `Domain/UserTokenTests.cs` | 6 |
| Seeding | `Infrastructure/SeedingTests.cs` | 6 |
| Reading and paging | `Api/BrowsingTests.cs` | 15 |
| Filtering and sorting | `Api/FilteringAndSortingTests.cs` | 21 |
| Posting, replying, liking | `Api/ContentTests.cs` | 21 |
| Registration | `Api/RegistrationTests.cs` | 10 |
| Email confirmation | `Api/EmailVerificationTests.cs` | 11 |
| Signing in and two-factor | `Api/SignInTests.cs` | 14 |
| Password reset | `Api/PasswordResetTests.cs` | 8 |
| Moderation and ownership | `Api/ModerationTests.cs` | 14 |
| Problem details | `Api/ExceptionHandlingTests.cs` | 2 |
| Rate limiting | `Api/RateLimitTests.cs` | 2 |
| Health | `Api/HealthEndpointTests.cs` | 2 |
| | **Total** | **155** |

Time is supplied by the host rather than read from the clock, so expiry, cooldowns and attempt
limits are exercised in milliseconds instead of waited out.

## Frontend

| Area | File | Tests |
| --- | --- | --- |
| Discussion list | `Home/PostsList.test.tsx`, `Home/PostsContainer.test.tsx` | 15 |
| Replies shown inline | `Home/InlineReplies.test.tsx` | 5 |
| Filters and ordering in the address bar | `hooks/useListCriteria.test.tsx`, `md/SortTabs.test.tsx` | 18 |
| Liking | `hooks/useLike.test.tsx`, `md/LikeButton.test.tsx` | 8 |
| Registration | `Auth/Register/Register.test.tsx` | 5 |
| Email confirmation | `Auth/VerifyEmail/VerifyEmail.test.tsx` | 3 |
| Signing in | `Auth/Login/Login.test.tsx`, `sm/CodeInput.test.tsx` | 8 |
| Session handling | `auth/AuthProvider.test.tsx`, `auth/session-storage.test.ts` | 9 |
| Starting a discussion | `Discussions/New/NewDiscussion.test.tsx` | 3 |
| Replying and editing a reply | `View/ReplyComposer.test.tsx`, `View/ReplyActions.test.tsx`, `View/RepliesList.test.tsx` | 12 |
| Moderator controls and the flag | `View/ModeratorTools.test.tsx`, `View/FlagBanner.test.tsx` | 7 |
| Owner edit and delete | `View/OwnerActions.test.tsx` | 5 |
| Switch control | `sm/Switch.test.tsx` | 2 |
| Dates | `utils/format-date.test.ts` | 6 |
| Page metadata | `test/site-metadata.node.test.ts` | 7 |
| | **Total** | **113** |

Component tests replace the service layer, not the network. A failing component test therefore
means the component changed, not that a URL moved.

## Postman

| Folder | Requests | Covers |
| --- | --- | --- |
| Service | 1 | Health |
| Browsing | 4 | Paging, a discussion, its replies, and that page two repeats nothing |
| Filtering and sorting | 9 | Each filter, each ordering, and the two combined |
| Accounts | 7 | Registration, the emailed link, single use, resend, and non-disclosure |
| Signing in | 19 | Password, code, session, both wrong halves, reset, and the old password failing |
| Contributing content | 13 | Posting, replying, liking, and the rules that refuse each |
| Moderation and ownership | 16 | Flagging, unflagging, editing, deleting, and every refusal |
| Failure cases | 13 | Bad paging, unknown identifiers, unknown values, bad registrations |
| | **82** | |

## What a person still has to check

Nothing below is automated. Each is quick, and each is where an automated suite is weakest.

| Check | How |
| --- | --- |
| The emails read well | Register, then read the message in Mailpit as a person would |
| The flag cannot be missed | Open a flagged discussion and see whether your eye lands on it |
| Light and dark both work | Switch your operating system theme with the forum open |
| The keyboard reaches everything | Tab through the sign-in form and the code boxes |
| Narrow screens | Resize to a phone width and read a discussion |
| A shared link restores the view | Filter and sort, copy the address, open it in a new window |
| The back button behaves | Filter, open a discussion, go back |
| Errors say something useful | Stop the API and use the forum |

## What is not tested at all

| Not covered | Why | What it would take |
| --- | --- | --- |
| End-to-end browser tests | Component tests plus API integration tests meet in the middle, and the journeys are short enough to walk by hand | A browser driver, a running stack in continuous integration, and the flakiness that comes with both |
| Load and performance | Nothing here has a performance requirement to fail | A load tool, a target, and an environment worth measuring |
| Accessibility audit | Checked by hand against the list above, not by a tool | An automated audit in the frontend job |
| Dependency and secret scanning | Not set up for a proof of concept | A scanner in continuous integration |
| The container images | They are built and run by hand, and the stack is brought up before a pull request | A job that builds both images and runs the Postman collection against them |

## Running everything

```bash
dotnet test backend/Forum.slnx

cd frontend
npm ci
npm run lint
npm run format:check
npm run test:run
npm run build
```

Continuous integration runs exactly these on every pull request. See
[run the tests](../how-to/run-tests.md) for what each one needs.
