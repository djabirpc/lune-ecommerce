# Changelog

## [2026-09-01]

### Added
- Monorepo structure: `backend/` (ASP.NET Core .NET 9) and `frontend/` (React + TypeScript + Vite).
- Backend solution `Ecommerce.slnx` with `Ecommerce.Domain`, `Ecommerce.Application`, `Ecommerce.Infrastructure`, `Ecommerce.Api`, plus test projects `Ecommerce.Application.Tests` and `Ecommerce.Api.Tests`.
- `AppDbContext` (`Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`) with custom table names.
- JWT Bearer authentication configuration (`Ecommerce.Application.Common.JwtOptions`, validated in `Program.cs`); no login endpoint yet.
- `Ecommerce.Domain.Identity.Roles` constants for the 7 roles defined in CLAUDE.md section 25.
- Swagger/Swashbuckle with a JWT bearer security scheme.
- Health checks: `GET /health` (Postgres connectivity) and `GET /api/system/ping` (liveness).
- Serilog console logging + request logging.
- CORS policy driven by `Cors:AllowedOrigins` configuration.
- Frontend: storefront route tree (`StorefrontLayout` + 11 pages) and admin route tree (`AdminLayout` + 12 pages), all business pages as placeholders, `HomePage` with static hero/trust copy.
- Frontend: `apiClient` fetch wrapper (`VITE_API_URL`), TanStack Query client, React Router v7, Tailwind CSS v4.
- Docker: `backend/Dockerfile` (multi-stage SDK build → aspnet runtime), `frontend/Dockerfile` (Vite dev server), root `docker-compose.yml` (postgres + backend + frontend), root `.env.example`, `frontend/.env.example`.
- Root `.gitignore`, `README.md`, `PROJECT_CONTEXT.md` (canonical, replaces the earlier draft `Project Context.md`).
- `dotnet-ef` local tool (pinned to 9.0.19) and initial EF Core migration `InitialIdentity`.
- Basic tests: `Entity`/`Roles` unit tests (Application.Tests), `/health` and `/api/system/ping` integration tests via `WebApplicationFactory<Program>` (Api.Tests).

### Changed
- N/A (first commit-worthy state of the repository).

### Fixed
- Removed the `Microsoft.AspNetCore.OpenApi` package reference from `Ecommerce.Api.csproj`: it pulls a `Microsoft.OpenApi` 1.x dependency that conflicts with Swashbuckle's `Microsoft.OpenApi` 2.7.5 and crashes controller discovery at runtime (`ReflectionTypeLoadException`).

### Database
- Migration `InitialIdentity` (`backend/src/Ecommerce.Infrastructure/Persistence/Migrations/`): creates the Identity schema only (`Users`, `Roles`, `UserRoles`, `UserClaims`, `UserLogins`, `RoleClaims`, `UserTokens`). No business tables yet.

### API
- `GET /health`, `GET /api/system/ping` added. No business endpoints yet.

### Frontend
- Initial storefront and admin route shells added (see PROJECT_CONTEXT.md for the full route list). No business data wired up yet.

### Notes
- This is a foundation-only bootstrap: no Products, Orders, COD, Yalidine, or ZR Express logic was implemented, per explicit scope for this step.
- Full backend test suite (`dotnet test` from `backend/`) passes: 5/5 tests.
- Full stack verified end-to-end via `docker compose up` (postgres healthy, backend healthy + migrated, frontend serving both `/` and `/admin`, CORS confirmed working from `http://localhost:5173`).
