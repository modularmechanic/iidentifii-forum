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

## In continuous integration

Every pull request runs both suites. The backend job has Docker available, so the integration
tests run there too.
