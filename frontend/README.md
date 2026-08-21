# Web client

Who this is for: developers working on the forum interface.
What you'll get: how to run, check and test this application on its own.

The client talks to the API at `/api/v1` and never to the database. In development the Vite server
proxies that path (and `/health`) to `http://localhost:5000`, so the browser only ever sees one
origin. Start the API first, otherwise the interface loads and reports that the API is unreachable.

## Commands

```bash
npm install       # once
npm run dev       # http://localhost:5173
npm run test:run  # component tests
npm run lint      # static analysis
npm run build     # type-check and bundle
```

Conventions for this codebase are in [`../GUIDELINES.md`](../GUIDELINES.md).
