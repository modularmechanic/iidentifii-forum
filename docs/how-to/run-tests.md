# Run the tests

Who this is for: developers and testers.
What you'll get: both test suites running, and what to do when they cannot.

## Backend

```bash
dotnet test backend/Forum.slnx
```

Unit tests cover the rules on the entities and need nothing external.

Integration tests start a throwaway PostgreSQL container, apply the migration and seed it, then
exercise the API over HTTP. **Docker must be running**, or these tests fail to start. They do not
touch the database from `docker compose`; each run gets its own container and removes it
afterwards.

## Frontend

```bash
npm run test:run --prefix frontend   # component tests
npm run lint --prefix frontend       # static analysis
npm run format:check --prefix frontend
npm run build --prefix frontend      # type check and bundle
```

Component tests render components with the same providers the application uses, and replace the
service layer rather than the network, so a test failing means the component changed.

## Testing the API by hand

Start the API and Mailpit first: two requests go looking for the confirmation email, and without
Mailpit awake they fail rather than skip. Then import
`docs/postman/iidentifii-forum.postman_collection.json` and run the collection. It creates an
account with a generated name, reads the confirmation email out of Mailpit, follows the link, and
checks that the same link is refused the second time, so it can be run repeatedly without editing
anything.

Folders share what they create: the registration checks reuse the name the Accounts folder
registered, so run the whole collection rather than that folder alone.

Development raises the request limits so the collection is not throttled by its own traffic. The
production values, and the 429 they produce, are covered by `RateLimitTests`.

## In continuous integration

Every pull request runs both suites. The backend job has Docker available, so the integration
tests run there too.
