# Test the API with Postman

Who this is for: testers and anyone integrating against the API rather than the web client.
What you'll get: the whole collection running top to bottom, repeatedly, without editing a thing.

## Before you start

The API and the mail catcher must both be running. Either path works:

```bash
docker compose up -d              # everything in containers; API on http://localhost:5080
```

```bash
docker compose up -d db mailpit   # dependencies only
dotnet run --project backend/src/Forum.Api   # API on http://localhost:5000
```

The collection reads emails out of Mailpit, so Mailpit is not optional.

## Import it

Three files live in [`docs/postman/`](../postman/):

| File | What it is |
| --- | --- |
| `iidentifii-forum.postman_collection.json` | 82 requests in eight folders, one per feature |
| `containers.postman_environment.json` | For `docker compose up`: API on 5080 |
| `local.postman_environment.json` | For `dotnet run`: API on 5000 |

Import the collection and whichever environment matches how you started the API, then select it as
the active environment. The two differ only in `baseUrl`; Mailpit is on 8025 either way.

## Run it

Open the collection, choose **Run**, leave every folder selected, and start the run. Nothing needs
filling in first.

Every request asserts its own outcome. A green run means each of the following held:

| Folder | What it proves |
| --- | --- |
| Service | The API is up and says which environment it is in |
| Browsing | Discussions and replies page correctly, and a second page repeats nothing from the first |
| Filtering and sorting | Each filter narrows, each ordering orders, and the two combine |
| Accounts | Registration, the emailed link, and that the same link is refused twice |
| Signing in | Password, emailed code, session, reset, and the failures around each |
| Contributing content | Posting, replying, liking, and the rules that refuse each |
| Moderation and ownership | A moderator flags, an author edits, and everyone else is refused |
| Failure cases | Bad input, unknown identifiers and unknown values, each with its own status |

## Why it can be run again

Nothing in the collection depends on state a previous run left behind.

- Registration generates a fresh username and address each time, so it never collides.
- The confirmation link, the sign-in code and the reset link are read out of Mailpit during the
  run. No secret is copied by hand.
- Identifiers are captured from responses into collection variables, so the requests that follow
  address whatever the earlier ones created.
- Anything the run creates, it either removes or leaves harmlessly behind: the discussion it
  starts is deleted by the ownership folder.

The one thing a run consumes is a slot in the seeded moderator's mailbox, which Mailpit keeps
until you clear it.

## When a run goes red

| Symptom | Likely cause |
| --- | --- |
| Every request fails to connect | The API is not running, or the environment names the other port |
| The Mailpit requests fail | Mailpit is not running, or `mailpitUrl` is wrong |
| A 429 appears partway through | The API is running in Production, whose limits are ten authentication requests a minute. Development raises them |
| The sign-in code request finds nothing | Mailpit was cleared between the login request and the search |

## Running it from the command line

The collection is a standard Postman v2.1 export, so `newman` runs it without modification:

```bash
npx newman run docs/postman/iidentifii-forum.postman_collection.json \
  -e docs/postman/containers.postman_environment.json
```

That pulls `newman` from npm. It is not a dependency of this repository, and nothing in
continuous integration uses it — the same ground is covered there by the integration tests, which
need no running API of their own.

## Related

- [Endpoint reference](../reference/api-endpoints.md)
- [Error codes](../reference/error-codes.md)
- [Test accounts](../reference/test-accounts.md)
