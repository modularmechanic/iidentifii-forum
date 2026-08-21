# Documentation

Who this is for: anyone running, extending or testing the forum.
What you'll get: a map of the documentation, arranged by what you are trying to do.

New here? Start with the [tutorial](tutorials/01-run-the-forum-and-log-in-with-2fa.md): the whole
forum in one command, and a signed-in session five minutes later.

Presenting it? [The walkthrough](walkthrough.md) has a demonstration order and answers to the
questions the design invites.

## By audience

### If you are using the forum

| You want to | Read |
| --- | --- |
| Run it and sign in | [Run the forum and log in with a one-time code](tutorials/01-run-the-forum-and-log-in-with-2fa.md) |
| Create an account, confirm it, reset a password | [Create an account, confirm it, and reset a forgotten password](how-to/create-account-verify-and-reset-password.md) |
| Flag something as misleading | [Moderate content](how-to/moderate-content.md) |
| Know what each screen does | The [feature pages](#feature-pages) |

### If you are building on it

| You want to | Read |
| --- | --- |
| Run both applications directly | [Run it for development](how-to/run-for-development.md) |
| Know the conventions | [Engineering guidelines](../GUIDELINES.md), [frontend conventions](explanation/frontend-conventions.md) |
| Branch, commit and open a pull request | [Branch, commit and open a pull request](how-to/branching-commits-and-pull-requests.md) |
| Understand the shape of it | [Architecture](explanation/architecture.md) |
| Know why it is like this | [Decisions and trade-offs](explanation/decisions-and-trade-offs.md) |
| Change a setting | [Configuration](reference/configuration.md) |
| Change the schema | [Data model](reference/data-model.md) |
| Configure mail or limits | [Configure email and rate limits](how-to/configure-email-and-rate-limits.md) |

### If you are testing it

| You want to | Read |
| --- | --- |
| Run both suites | [Run the tests](how-to/run-tests.md) |
| Exercise the API | [Test the API with Postman](how-to/test-the-api-with-postman.md) |
| Know what is covered, and what is not | [QA test matrix](reference/qa-test-matrix.md) |
| Sign in as somebody | [Test accounts and sample content](reference/test-accounts.md) |
| Decode a failure | [Error codes](reference/error-codes.md) |

### If you are integrating against the API

| You want to | Read |
| --- | --- |
| Call an endpoint | [API endpoints](reference/api-endpoints.md) |
| Handle a failure | [Error codes](reference/error-codes.md) |
| Know what protects an account | [Security model](explanation/security-model.md) |

## Everything, by kind

The pages follow the [Diátaxis](https://diataxis.fr) split: tutorials teach, how-to guides solve
one task, reference states facts, explanation gives reasons.

### Tutorials

| Page | Covers |
| --- | --- |
| [Run the forum and log in with a one-time code](tutorials/01-run-the-forum-and-log-in-with-2fa.md) | The whole stack in containers, and a two-step sign-in |

### How-to guides

| Page | Covers |
| --- | --- |
| [Run it for development](how-to/run-for-development.md) | Both applications, without containers |
| [Run the tests](how-to/run-tests.md) | Both suites, and what each one needs |
| [Test the API with Postman](how-to/test-the-api-with-postman.md) | The collection, top to bottom, repeatedly |
| [Create an account, confirm it, and reset a forgotten password](how-to/create-account-verify-and-reset-password.md) | The account journeys, by hand |
| [Moderate content](how-to/moderate-content.md) | Flagging, unflagging, and what a moderator cannot do |
| [Configure email and rate limits](how-to/configure-email-and-rate-limits.md) | Mail and throttling |
| [Branch, commit and open a pull request](how-to/branching-commits-and-pull-requests.md) | The contribution workflow |

### Reference

| Page | Covers |
| --- | --- |
| [API endpoints](reference/api-endpoints.md) | Every endpoint, its parameters and its statuses |
| [Error codes](reference/error-codes.md) | Every failure shape, and how to tell two apart |
| [Configuration](reference/configuration.md) | Every setting, its default, and what breaks without it |
| [Data model](reference/data-model.md) | Tables, indexes, and what the database enforces |
| [Test accounts and sample content](reference/test-accounts.md) | Who is seeded, and exactly what exists |
| [QA test matrix](reference/qa-test-matrix.md) | What is covered, by which suite, and what is not |

### Explanation

| Page | Covers |
| --- | --- |
| [Architecture](explanation/architecture.md) | The pieces, the request path, and the boundaries |
| [Decisions and trade-offs](explanation/decisions-and-trade-offs.md) | Each choice, its alternative, and its cost |
| [Security model](explanation/security-model.md) | What protects an account, what does not, and why |
| [Frontend conventions](explanation/frontend-conventions.md) | How the client is arranged, and why |

### Feature pages

One per delivered slice: what it does, how it hangs together, and how to try it.

| Page | Covers |
| --- | --- |
| [Browsing](features/browsing.md) | Reading discussions and replies without an account |
| [Filtering and sorting](features/filtering-and-sorting.md) | Narrowing and ordering the list |
| [Accounts](features/accounts.md) | Registering and confirming an email address |
| [Signing in](features/signing-in.md) | Password, emailed code, and resetting a forgotten password |
| [Contributing content](features/contributing-content.md) | Posting, replying and liking |
| [Moderation and ownership](features/moderation.md) | Flagging, editing and deleting |

### Elsewhere in the repository

| Page | Covers |
| --- | --- |
| [`README.md`](../README.md) | What this is, and how to start it |
| [`GUIDELINES.md`](../GUIDELINES.md) | The conventions a change is reviewed against |
| [`CLAUDE.md`](../CLAUDE.md) | The workflow a change follows |
| [`frontend/README.md`](../frontend/README.md) | Running the client on its own |
| [`docs/postman/`](postman/) | The collection and its environments |
| [`docs/screenshots/`](screenshots/) | The images the feature pages use |
