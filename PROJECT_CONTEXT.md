# Project Context

## Project

Luna is a custom full-stack e-commerce platform for a fashion business in Algeria.

The platform is built specifically around Algerian COD (Cash On Delivery) commerce. It is NOT Shopify/WooCommerce and must remain a fully independent, custom application (see CLAUDE.md section 2).

## Current Stack

Backend:
- ASP.NET Core .NET 9 (C#)
- Entity Framework Core 9 + Npgsql (PostgreSQL)
- ASP.NET Core Identity (custom `ApplicationUser`/`ApplicationRole`, Guid keys)
- JWT Bearer authentication (access tokens via `System.IdentityModel.Tokens.Jwt`, rotating refresh tokens hashed in DB)
- FluentValidation (wired, validators exist for Auth DTOs, invoked manually from `AuthService`)
- Serilog (console sink)
- Swashbuckle/Swagger with JWT bearer scheme
- Health checks (`/health`, Npgsql check)
- Testcontainers.PostgreSql (test-only, for real-DB integration tests — requires Docker running)

Frontend:
- React 19 + TypeScript + Vite 8
- Tailwind CSS v4 (via `@tailwindcss/vite`, no separate config file)
- React Router v7 (`createBrowserRouter`)
- TanStack Query v5
- React Hook Form + Zod + `@hookform/resolvers` (installed, not yet wired to a form)

Infrastructure:
- Docker + Docker Compose (postgres, backend, frontend)
- PostgreSQL 16 (alpine image)

## Current Architecture

Backend follows a 4-project modular architecture under `backend/src/`:

```text
Ecommerce.Domain          — framework-agnostic entities/constants (Entity base class, Roles)
Ecommerce.Application     — use cases, DTOs, validators, Auth interfaces (IAuthService/ITokenService), AppException hierarchy
Ecommerce.Infrastructure  — EF Core, Identity (ApplicationUser/ApplicationRole/RefreshToken), AppDbContext, AuthService/TokenService, IdentitySeeder, AddInfrastructure()
Ecommerce.Api             — controllers (incl. AuthController), Program.cs composition root, Swagger, JWT, health checks, global exception middleware
```

Auth-related namespaces worth knowing:
- `Ecommerce.Application.Auth` — `IAuthService`, `ITokenService`, `Dtos/` (LoginRequest, RefreshTokenRequest, AuthResponse, CurrentUserResponse), `Validators/`
- `Ecommerce.Application.Common.Exceptions` — `AppException` + `UnauthorizedAppException`/`ValidationAppException`/`NotFoundAppException`/`ConflictAppException`, all mapped to the `{ success, error }` shape by `Ecommerce.Api.Middleware.ExceptionHandlingExtensions.UseAppExceptionHandling()`
- `Ecommerce.Infrastructure.Identity` — `ApplicationUser`, `ApplicationRole`, `RefreshToken`, `TokenService`, `AuthService`
- `Ecommerce.Infrastructure.Persistence.IdentitySeeder` — seeds roles + optional bootstrap SUPER_ADMIN, called from `Program.cs` alongside the dev-only auto-migration

Solution file is `backend/Ecommerce.slnx` (new XML solution format used by .NET 10 SDK tooling — NOT `.sln`; keep this in mind when referencing it from Dockerfiles or scripts).

Tests live under `backend/tests/`:
- `Ecommerce.Application.Tests` (xUnit, references Domain/Application) — includes `Auth/LoginRequestValidatorTests`, `Auth/RefreshTokenRequestValidatorTests`
- `Ecommerce.Api.Tests` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`) — `HealthCheckTests` (lenient, no real DB needed) and `AuthEndpointsTests` (uses `AuthWebApplicationFactory`, which spins up a real ephemeral Postgres via **Testcontainers** — this test class requires Docker to be running; `dotnet test` will fail/hang without it)

Frontend is a single Vite app with two route trees sharing one React app (`frontend/src/app/router.tsx`):

```text
frontend/src/
  storefront/   — customer-facing layout + pages (StorefrontLayout wraps "/")
  admin/        — back-office layout + pages (AdminLayout wraps "/admin")
  lib/api/      — fetch-based apiClient (reads VITE_API_URL) + TanStack QueryClient
  lib/components/ — PagePlaceholder (shared placeholder used by all not-yet-built pages)
  app/router.tsx  — single createBrowserRouter with both route trees
```

## Implemented Features

Business features (Products, Orders, COD, Yalidine, ZR Express, Promotions, Marketing) are intentionally not started.

What IS implemented:
- Monorepo structure (`backend/`, `frontend/`)
- Backend solution builds and runs; Swagger UI available at `/swagger` in Development
- `AppDbContext` (IdentityDbContext, Guid keys) with custom table names (Users, Roles, UserRoles, RefreshTokens, etc.)
- **Authentication**: JWT login/refresh/logout/me. `POST /api/auth/login`, `POST /api/auth/refresh` (rotates the refresh token), `POST /api/auth/logout` (revokes it), `GET /api/auth/me` (`[Authorize]`, reads claims). Refresh tokens are opaque random strings, stored **hashed** (SHA-256) server-side, with rotation (old token revoked + linked to its replacement on every refresh).
- `IdentitySeeder` seeds the 7 roles from `Ecommerce.Domain.Identity.Roles` and an optional bootstrap `SUPER_ADMIN` user from the `InitialAdmin` config section, idempotently, on every startup where `ApplyMigrationsOnStartup=true`.
- Standardized API error responses (`{ success: false, error: { code, message } }`) for both application-level exceptions (`AppException` subclasses) and FluentValidation failures, via global exception-handling middleware.
- `Ecommerce.Domain.Identity.Roles` constants matching CLAUDE.md section 25 (SUPER_ADMIN, ADMIN, ORDER_MANAGER, CONFIRMATION_AGENT, STOCK_MANAGER, MARKETING_MANAGER, VIEWER) — now seeded into the database.
- `GET /health` (Npgsql connectivity check) and `GET /api/system/ping` (no-dependency smoke endpoint)
- EF Core migrations `InitialIdentity` and `AddRefreshTokens`
- CORS configured via `Cors:AllowedOrigins` config (env var `Cors__AllowedOrigins__0` in docker-compose)
- Frontend storefront/admin route shells with separate layouts/navigation, all pages are placeholders — **not yet wired to the auth endpoints** (no login page, no token storage/refresh logic in the frontend yet)
- Docker Compose stack (postgres + backend + frontend) verified working end-to-end, including automatic EF Core migration + identity seeding on backend startup (dev only, gated by `ApplyMigrationsOnStartup` config key, not `ASPNETCORE_ENVIRONMENT`). Full login → me → refresh → logout cycle verified via curl against the containerized backend.

## Current Feature

None in progress. Authentication foundation is complete; awaiting direction on the next feature (see Next Recommended Steps — Products/Categories is the natural next step).

## Last Completed Work

2026-09-01 — Authentication feature: JWT login/refresh/logout/me, refresh token rotation with hashed storage, role + bootstrap SUPER_ADMIN seeding, standardized `{success,error}` API error responses, and Testcontainers-backed integration tests for the full auth flow. Also fixed a real bug uncovered by those tests: several places (`AddInfrastructure`, health check registration, JWT bearer setup) read `IConfiguration` eagerly before `WebApplicationBuilder.Build()`, silently ignoring configuration composed later (test overrides, and potentially any late-bound config source in a real deployment) — fixed by resolving configuration lazily via DI everywhere. Full details in `CHANGELOG.md` under `[2026-09-01]`.

Previous milestone (same date): full technical bootstrap of the Luna platform (monorepo scaffold, backend/frontend skeletons, Docker Compose stack, documentation set) — see `CHANGELOG.md` for the original bullet list.

## Database

PostgreSQL via Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4.

Entities that currently exist (Identity only, via `AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`):
- `ApplicationUser` (`Users` table) — extends `IdentityUser<Guid>` with `FirstName`, `LastName`, `IsActive`, `CreatedAtUtc`
- `ApplicationRole` (`Roles` table) — extends `IdentityRole<Guid>`
- Standard Identity join tables: `UserRoles`, `UserClaims`, `UserLogins`, `RoleClaims`, `UserTokens`
- `RefreshToken` (`RefreshTokens` table) — `UserId`, `TokenHash` (SHA-256, unique), `ExpiresAtUtc`, `RevokedAtUtc`, `ReplacedByTokenHash`; `IsActive` computed as not-revoked-and-not-expired

Migrations: `backend/src/Ecommerce.Infrastructure/Persistence/Migrations/` — `InitialIdentity`, `AddRefreshTokens` (both applied and verified against a live Postgres container).

Planned entities (not yet created — see CLAUDE.md section 33 template / original scope notes): Customer, CustomerAddress, Category, Product, ProductVariant, ProductImage, Inventory, InventoryTransaction, Cart, CartItem, Order, OrderItem, OrderStatusHistory, OrderCallAttempt, Promotion, Shipment, TrackingEvent, ShippingRate, MarketingEvent, AuditLog.

`dotnet-ef` is installed as a **local tool** (see `backend/dotnet-tools.json`, pinned to 9.0.19 to match the EF Core package version — the SDK's global `dotnet-ef` would default to 10.x and can misbehave against 9.x packages). Run it as `dotnet tool run dotnet-ef ...` or `dotnet ef ...` from `backend/` (local tools are on PATH within a restored tool manifest context).

## API

Base URL (local/dev): `http://localhost:5000` (docker-compose maps container port 8080 → host `BACKEND_PORT`, default 5000).

Endpoints that exist today:
- `GET /health` — Npgsql-backed health check
- `GET /api/system/ping` — trivial liveness endpoint, no dependencies
- `GET /swagger` — Swagger UI (Development only)
- `POST /api/auth/login` — `{ email, password }` → `AuthResponse` (access token, refresh token, expiries, `CurrentUserResponse`). Generic `401 UNAUTHORIZED` for both unknown email and wrong password (no user enumeration).
- `POST /api/auth/refresh` — `{ refreshToken }` → new `AuthResponse`; rotates the refresh token (old one revoked, reuse returns `401`).
- `POST /api/auth/logout` — `{ refreshToken }` → `204`; revokes the token (idempotent, no error if already revoked/unknown).
- `GET /api/auth/me` — `[Authorize]`, reads claims from the validated JWT, returns `CurrentUserResponse`.

Errors follow `{ success: false, error: { code, message } }` (CLAUDE.md section 27) — codes seen so far: `UNAUTHORIZED`, `VALIDATION_ERROR`, `INTERNAL_ERROR` (see `Ecommerce.Application.Common.Exceptions.AppException` for the full set including `NOT_FOUND`/`CONFLICT`, not yet used by any endpoint).

No business endpoints yet.

No shipping provider integrations exist yet (Yalidine/ZR Express are explicitly out of scope for this step per CLAUDE.md section 16/17 — do not invent endpoints).

## Frontend

Routing is centralized in `frontend/src/app/router.tsx` (React Router v7 `createBrowserRouter`), two top-level route trees:

Storefront (`StorefrontLayout` at `/`): `/`, `/categories`, `/category/:slug`, `/product/:slug`, `/promotions`, `/cart`, `/checkout`, `/order-confirmation/:orderNumber`, `/track-order`, `/orders`, `/account`.

Admin (`AdminLayout` at `/admin`): `/admin` (+ `/admin/dashboard`), `/admin/orders`, `/admin/orders/confirmation`, `/admin/orders/:id`, `/admin/products`, `/admin/inventory`, `/admin/promotions`, `/admin/customers`, `/admin/shipping`, `/admin/marketing`, `/admin/users`, `/admin/settings`.

All pages except `HomePage` currently render `<PagePlaceholder />`. `HomePage` has static hero copy + trust section (no data fetching yet — matches CLAUDE.md section 8 copy).

`apiClient` (`frontend/src/lib/api/client.ts`) is a thin `fetch` wrapper reading `VITE_API_URL`, expects the backend's `{ success, error: { code, message } }` error shape (CLAUDE.md section 27) and throws `ApiError`.

## Shipping

- `IShippingProvider` abstraction: **not started**.
- Yalidine: **not started** — no credentials/docs available yet; do not invent endpoints when this is picked up (CLAUDE.md section 16).
- ZR Express: **not started** — same caveat (CLAUDE.md section 17).
- FakeShippingProvider: **not started** (mandatory once shipping work begins, per CLAUDE.md section 15).

## COD Workflow

Not implemented yet. Order state machine, `OrderStatusHistory`, `OrderCallAttempt`, and the confirmation workflow described in CLAUDE.md sections 12–13 are all pending — no `Order` entity exists.

## Marketing

Not implemented yet. No Meta/TikTok pixel wiring, no UTM capture, no `MarketingEvent` entity.

## Known Issues

- Swashbuckle.AspNetCore 10.2.3 pulls in `Microsoft.OpenApi` 2.7.5, whose API (`Microsoft.OpenApi` namespace, `OpenApiSecuritySchemeReference`, factory-based `AddSecurityRequirement`) differs substantially from the older `Microsoft.OpenApi.Models` namespace used by most online examples/older Swashbuckle docs — if you copy JWT/Swagger setup snippets from older sources they will not compile as-is. See `Program.cs` for the working pattern.
- The ASP.NET Core built-in `Microsoft.AspNetCore.OpenApi` package (used for the default `AddOpenApi()`/`MapOpenApi()` minimal-API template code) must NOT be referenced alongside Swashbuckle — it pulls a conflicting `Microsoft.OpenApi` 1.x, which causes a `ReflectionTypeLoadException` at runtime when the app tries to build the controller list. This package reference was deliberately removed from `Ecommerce.Api.csproj`.
- `dotnet new sln` on this machine (SDK 10.0.301 is the resolved default) generates the new `.slnx` format, not `.sln`. The Dockerfile and any tooling must reference `Ecommerce.slnx`.
- The dev-only auto-migration on backend startup is gated by the `ApplyMigrationsOnStartup` config key (env var `ApplyMigrationsOnStartup` / `ApplyMigrationsOnStartup=true` in docker-compose), deliberately NOT tied to `ASPNETCORE_ENVIRONMENT=Development`, because `WebApplicationFactory` in tests also runs in the Development environment and would otherwise try to migrate a non-existent test database on every test run.
- `appsettings.Development.json` contains a **dev-only** dummy Postgres password and JWT signing key (clearly non-production values) so `dotnet run` works out of the box against a locally-running Postgres without Docker. These must never be reused in staging/production — real secrets must come from environment variables / secret management per CLAUDE.md section 24.
- **This dev machine has a native Windows PostgreSQL service already running on port 5432** (`postgres.exe`, Windows service, unrelated to this project). It does NOT block `docker compose`'s own `postgres` container from also publishing to host port 5432 (empirically verified working), but it DOES mean: (a) `appsettings.Development.json`'s `luna`/`luna_dev_password` credentials will hit whichever Postgres instance is currently bound to 5432 and fail auth if it's the native one and not the intended one — if `dotnet run` (non-Docker) can't connect, check `netstat -ano | grep 5432` / `tasklist` first; (b) this is exactly what caused the bug described below to surface. If this becomes a recurring problem, consider changing the local dev Postgres port in `appsettings.Development.json` and `docker-compose.yml`.
- **(Fixed, but worth knowing)** `AddInfrastructure`, the health check registration, and the original JWT bearer setup all read `IConfiguration` **eagerly** — evaluating `configuration.GetConnectionString(...)` / `configuration.GetSection(...).Get<T>()` into a local variable at service-registration time, before `WebApplicationBuilder.Build()`. Any configuration source composed later (in tests: `WebApplicationFactory`'s `ConfigureAppConfiguration` overrides) was silently ignored, and the app fell back to whatever `appsettings.{Environment}.json` already had — which, combined with the native Postgres above, produced a confusing `password authentication failed for user "luna"` error instead of an obvious "wrong config" signal. **Rule of thumb going forward: never read `IConfiguration` into a local variable outside of a `Configure<T>`/`AddOptions<T>`/`(IServiceProvider, options) =>` callback in `Program.cs` or `AddInfrastructure`** — always resolve it lazily via DI so both tests and any future runtime config changes are respected.

## Pending Work

1. ~~Initialize repository.~~ ✅ (git initialized, no commits made yet — awaiting user instruction to commit)
2. ~~Create backend solution.~~ ✅
3. ~~Create React frontend.~~ ✅
4. ~~Configure PostgreSQL.~~ ✅
5. ~~Configure Docker.~~ ✅
6. ~~Implement authentication (login/refresh endpoints, role seeding).~~ ✅ — login/refresh/logout/me implemented and tested; still pending: frontend login UI/token storage, and an admin-facing user-management endpoint (currently the only way to create staff accounts is the `InitialAdmin` bootstrap seed — no `POST /api/admin/users` yet).
7. Implement products/categories.
8. Implement inventory.
9. Implement storefront (real data, not placeholders).
10. Implement COD checkout.
11. Implement order workflow.
12. Implement admin (real data, not placeholders).
13. Implement promotions.
14. Implement shipping abstraction (`IShippingProvider` + `FakeShippingProvider`).
15. Integrate Yalidine (needs real API docs/credentials first).
16. Integrate ZR Express (needs real API docs/credentials first).
17. Implement marketing tracking (pixels, UTM capture, attribution).
18. Expand automated test coverage as business features land (CLAUDE.md section 29 lists the required minimum workflows).
19. Production deployment.

## Important Decisions

1. The project is a custom e-commerce platform and must not depend on Shopify or WooCommerce.
2. COD is the primary payment method.
3. Inventory is managed at product-variant level (not yet built).
4. Shipping providers are accessed through `IShippingProvider` (not yet built).
5. Mobile-first design is mandatory (Tailwind v4 chosen partly for its low-overhead setup; no desktop-first patterns introduced).
6. The backend is the source of truth for business logic.
7. `PROJECT_CONTEXT.md` (this file, at the repo root, exact filename) is the persistent project memory — supersedes the earlier draft `Project Context.md`, which has been removed to avoid two diverging copies.
8. Identity uses `Guid` keys and custom table names (`Users`, `Roles`, ...) instead of the AspNet* defaults, decided at bootstrap time to keep the schema clean from the start — changing this later would require a destructive migration.
9. `dotnet-ef` is pinned as a local tool at 9.0.19 (matching the EF Core package version) rather than relying on the machine's global tool, to avoid SDK-version drift breaking migrations for other contributors.
10. Auto-migration on backend startup is opt-in via `ApplyMigrationsOnStartup`, not automatic in Development, specifically to keep the test suite (which runs in the Development environment) independent of a live database.
11. Refresh tokens are opaque random strings stored **hashed** (SHA-256) server-side, never plaintext — same rationale as password hashing. Rotation on every refresh (old token revoked and linked to its replacement) so token reuse after refresh is detectable/rejected.
12. No public self-registration endpoint. Guest checkout means customers never need accounts (CLAUDE.md section 11); staff/admin accounts (the only users of this Identity system so far) are provisioned via the `InitialAdmin` bootstrap seed for now. A proper admin-side user-management endpoint is deferred to the `/admin/users` feature (CLAUDE.md section 22/23), not built as part of core auth.
13. All `IConfiguration` reads in service-registration code must be lazy (resolved via DI at first use), never eager local-variable reads — see Known Issues for the bug this caused and the fix pattern to follow for all future config-dependent registrations (`AddDbContext`, health checks, options binding, etc.).

## Next Recommended Steps

1. Design and implement the Product/Category/ProductVariant domain model (CLAUDE.md section 9) plus the corresponding EF Core migration — this unblocks both storefront product pages and admin product management. This is the natural next step now that auth exists to protect the future admin-only product endpoints.
2. Design the Inventory model (CLAUDE.md section 10) alongside Products, since variant-level stock must exist before Cart/Order work can safely reserve stock.
3. Once Products + Inventory exist, tackle Cart → COD Checkout → Order creation with stock reservation (CLAUDE.md sections 11–12), including the `Create order → Reserve stock → Cancel → Release stock` and `Create order → ... → Deliver` test scenarios required by CLAUDE.md section 29.
4. Smaller, can be done anytime: wire the frontend to the new auth endpoints (login page, token storage/refresh interceptor in `apiClient`) — currently the API works but nothing in `frontend/` calls it yet.
5. Smaller, can be done anytime: add role-based `[Authorize(Roles = ...)]` guards once the first admin-only endpoint exists (Products/Orders), using the `Ecommerce.Domain.Identity.Roles` constants.
