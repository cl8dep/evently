# Evently

Event tracker for company networking. ASP.NET Core 10 minimal API (`src/Evently.Api`), React 19 + Vite + MUI SPA (`client`), PostgreSQL. See README.md for features and setup.

## Commands

- Database: `docker compose up -d db` (host port 5433)
- API: `dotnet run --project src/Evently.Api --launch-profile http` (port 5158)
- SPA: `cd client && npm run dev` (port 5173, proxies `/api`)
- Tests: `dotnet test` (needs the db container)
- Client checks: `cd client && npx tsc -b && npm run lint`
- Full stack in Docker: `docker compose up -d --build` (port 8080)

## Conventions

- All code, comments, docs and commit messages in English. The UI is English only.
- One class, record, enum or interface per file. Files live in the folder for their role (Domain, Contracts, Endpoints, ...).
- Every C# type uses `namespace Evently.Api` whatever its folder. Changing namespaces renames entities in the EF snapshot and breaks migrations.
- Minimal comments, only for non-obvious why. No dead code.
- Prefer integration tests against the real database over mocks.
- No em-dashes in text. Commit messages are one sentence.

## Rules that are easy to break

- Migrations run on every startup and abort startup on failure. Add one with `dotnet ef migrations add <Name>` from `src/Evently.Api`, never edit an applied one.
- The server Google key (`GoogleMaps:ApiKey`) must never be sent to the browser. Only `GoogleMaps:BrowserKey` is.
- Never commit `.env`, API keys or user-secrets. Tests blank the Google key so they never call Google.
- Mutating routes require the `Editor` policy and `/api/users` requires `Admin`. A new route needs an explicit choice.
- The favicon fetcher downloads user-supplied URLs. Keep the public-address check on every connection.
- New event creation sends an `Idempotency-Key`; keep that when touching the form.
