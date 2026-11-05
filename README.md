# Evently

Track the events your company may attend for networking: what they are, where, when, what they cost and who is going to look at them.

ASP.NET Core 10 API + React SPA, PostgreSQL, email and password sign-in with roles.

## Features

- Events with title, description, category, topics, website, estimated ticket cost, dates and a structured location.
- Categories and topics managed by users. An event has one category and any number of topics.
- Place search with Google Places (served by the backend, the key never reaches the browser) and an interactive map with a pin.
- The event website's best declared favicon is fetched and used as the event logo. The page description fills an empty description.
- CSV export of the filtered list (UTF-8 with BOM, safe against spreadsheet formula injection).
- CSV import of the same format. Missing categories and topics are created, events already in the list (same title and start date) are skipped, and one bad row rejects the whole file. Up to 1000 rows. Topic names must not contain commas, and website icons are fetched later with Refresh.
- Users with first and last name and a role:

| Role   | Can do                                                    |
| ------ | --------------------------------------------------------- |
| Admin  | Everything, including managing users                      |
| Editor | Create, edit and delete events, categories and topics     |
| Viewer | Read-only, can export the CSV                             |

Sign-up is closed after the first account. That account becomes Admin and adds everyone else from **Users**.

## Run it with Docker

```sh
cp .env.example .env     # optional: only needed for Google keys or a different DB password
docker compose up -d --build
```

Open http://localhost:8080 and create the first account on the sign-in screen.

Migrations run on startup. If one fails the app does not start.

## Deploy on Railway

1. Create a project from this repo. `railway.toml` selects the Dockerfile and the `/api/health` check.
2. Add a **PostgreSQL** service to the project.
3. On the app service, set these variables (the `Postgres` names are Railway references):
   - `ConnectionStrings__Default` = `Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}`
   - `DataProtection__Keys` = `/keys`
   - `RAILWAY_RUN_UID` = `0` (Railway mounts volumes as root)
   - `GoogleMaps__ApiKey` and `GoogleMaps__BrowserKey` (optional)
4. Attach a **volume** to the app service at `/keys` so sessions survive redeploys.
5. Generate a public domain, open it and create the first account (it becomes Admin).

The app listens on Railway's `PORT` and applies migrations on every start. Add the Railway domain to the HTTP referrer list of the browser key.

## Develop locally

Needs the .NET 10 SDK, Node 24 and Docker (for PostgreSQL only).

```sh
docker compose up -d db
dotnet run --project src/Evently.Api --launch-profile http    # API on :5158
cd client && npm install && npm run dev                       # SPA with HMR on :5173, proxies /api
```

Run the tests (they need the `db` container, each run uses its own throwaway database):

```sh
dotnet test
```

## Configuration

| Setting                        | Docker `.env` variable      | Purpose                                                        |
| ------------------------------ | --------------------------- | -------------------------------------------------------------- |
| `ConnectionStrings:Default`    | `POSTGRES_PASSWORD`         | PostgreSQL connection                                          |
| `GoogleMaps:ApiKey`            | `GOOGLE_MAPS_API_KEY`       | Server key, Places API (New). Without it place search is off   |
| `GoogleMaps:BrowserKey`        | `GOOGLE_MAPS_BROWSER_KEY`   | Browser key, Maps JavaScript API. Without it the map is hidden |
| `DataProtection:Keys`          | set by compose              | Folder for cookie keys so sessions survive restarts            |

For local development keep the keys out of the repo with user-secrets:

```sh
dotnet user-secrets set "GoogleMaps:ApiKey" "..." --project src/Evently.Api
dotnet user-secrets set "GoogleMaps:BrowserKey" "..." --project src/Evently.Api
```

Google Cloud: enable **Places API (New)** and **Maps JavaScript API**. Allow Places API (New) on the server key and Maps JavaScript API on the browser key. Restrict the browser key by HTTP referrer.

## Layout

```
src/Evently.Api/
  Domain/       entities
  Contracts/    request records and filters
  Data/         DbContext and seed data (countries, currencies)
  Endpoints/    one file per API area
  Events/       validation, queries and website enrichment
  Favicons/     website icon fetching (SSRF-safe)
  Places/       Google Places client
  Export/       CSV export and import
  Security/     roles
  Migrations/   EF Core migrations
client/         React + Vite + MUI
tests/          integration tests against a real PostgreSQL
```

## Security notes

- The server downloads user-supplied website URLs. Connections to private, loopback and link-local addresses are blocked, redirects included. Served icons carry a sandboxing CSP.
- Create operations accept an `Idempotency-Key` header, so a retried request returns the same event.
- A role change or password reset applies on the user's next request.
