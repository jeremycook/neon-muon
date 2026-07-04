# AGENTS.md

## Project Overview

NeonMuon is a multi-tenant PostgreSQL database management tool. .NET 8 backend + vanilla TypeScript/Vite frontend.

## Commands

```bash
# Backend (from repo root)
dotnet build NeonMuon.sln
dotnet run --project WebApp        # starts all tenants

# Frontend (from neonmoun-ui/)
npm install
npm run dev      # Vite dev server on :52236, proxies /api to :42236
npm run build    # tsc && vite build
```

No test suite, no linter, no CI, no Docker.

## Setup

Copy `WebApp/appsettings.Example.json` into user secrets (not a file in the repo):
- Linux: `~/.microsoft/usersecrets/NeonMuon/secrets.json`
- Windows: `%APPDATA%\Microsoft\UserSecrets\NeonMuon\secrets.json`

The `UserSecretsId` is `NeonMuon` (set in `WebApp/WebApp.csproj`).

## Architecture

### Two .NET projects in one solution

- `NeonMuon/` — core library (OutputType=Library). All controllers, data access, auth, tenancy logic.
- `WebApp/` — thin host. `Program.cs` is 3 lines: calls `MultiTenantHost.RunAsync<Program>(args)`.

### Multi-tenant host

`NeonMuon/Tenancy/MultiTenantHost.cs` reads the `Tenants` config array and spawns one Kestrel instance per tenant. Each tenant gets its own URL(s), content root, and web root. In development, `appsettings.Development.json` defines a single tenant on `http://127.0.0.1:58319`.

### Custom DI via attributes

Instead of manual `services.Add*` calls, services are registered by attribute:
- `[Settings]` — binds from config section matching the class name, registered as singleton
- `[Singleton]` — `AddSingleton`
- `[Scoped]` — `AddScoped`

Registration happens in `ConfigurationHelpers.BuildFromTypes()` called from `Starter.Main()`.

### Database

- PostgreSQL via **Npgsql** (raw ADO.NET) + **LinqToDB** (for some setup).
- Connection strings are built dynamically from `DataServers` and `MaintenanceCredentials` config sections.
- `DB` class (`NeonMuon/DataAccess/DB.cs`) is the central data access point (registered as singleton).
- SQL is written as raw strings, not LINQ queries. See `QueryController` and `NodeController` for examples.
- `Npgsql.EnableSqlRewriting` is disabled in `DB` static constructor.

### Authentication

Cookie-based. On login, a temporary PostgreSQL role is created with a time-limited credential. The credential is serialized into a `dc:{ServerName}` claim on the cookie principal. `CurrentUser` (scoped) extracts credentials from claims.

### API conventions

- Route template: `api/[controller]/[action]` (defined in `MvcConstants.StandardApiRoute`)
- URLs are dashified via `CustomOutboundParameterTransformer` (e.g., `AuthController.Login` → `api/auth/login`)
- All controllers require authorization by default (`AuthorizeFilter` in global filters)
- JSON: `System.Text.Json` with `JsonStringEnumConverter`, trailing commas, case-insensitive, comments skipped
- `QueryController`: PUT = preview (always rolls back), POST = apply (commits)

### Frontend (neonmoun-ui/)

Note the typo: directory is `neonmoun-ui`, not `neonmuon-ui`.

- No framework. Vanilla TypeScript with custom DOM helpers in `src/utils/html.ts`.
- Custom reactive system: `src/utils/pubSub.ts` with `Sig`/`Val` classes (WeakRef-based subscriptions).
- Custom client-side router: `src/utils/routed.ts` intercepts link clicks and dispatches `locationchange` events.
- Routes defined in `src/routes.ts` (keys must be lowercase).
- API calls use `jsonGet`/`jsonPost`/`jsonPut` from `src/utils/http.ts`.
- Vite proxies `/api` to `https://127.0.0.1:42236` during dev (configure port in `vite.config.js`).
- The backend serves the frontend as a fallback: `app.MapFallbackToFile("/_content/NeonMuon/index.html")`.

### Static logging

`NeonMuon/Log.cs` is a static wrapper around `ILoggerFactory`. Set `Log.Factory` once at startup. Used throughout instead of injected `ILogger<T>`.

## Gotchas

- `Starter.Main()` is invoked via reflection (`mainMethod.Invoke`), not direct call.
- The `Starter` class name is resolved by convention; custom starters use `[Starter]` attribute.
- Tenant content/web root paths are resolved with `string.Format(tenantOptions.ContentRoot, tenantOptions.Id)`.
- No `global.json` — uses whatever .NET SDK is on PATH.
- No `.editorconfig` or `Directory.Build.props`.
