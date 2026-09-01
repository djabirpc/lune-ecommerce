# Luna — Custom Algerian E-Commerce Platform

Luna is a fully custom e-commerce platform for a fashion business operating in Algeria, built around Cash On Delivery (COD). It is not built on Shopify, WooCommerce, or any external e-commerce platform — see [`CLAUDE.md`](./CLAUDE.md) for the full product/technical brief and [`PROJECT_CONTEXT.md`](./PROJECT_CONTEXT.md) for the current state of the project.

## Stack

- **Backend**: ASP.NET Core .NET 9, EF Core + Npgsql, ASP.NET Core Identity, JWT, FluentValidation, Serilog, Swagger.
- **Frontend**: React 19 + TypeScript + Vite, Tailwind CSS v4, React Router, TanStack Query, React Hook Form + Zod.
- **Database**: PostgreSQL 16.
- **Infrastructure**: Docker + Docker Compose.

## Repository layout

```text
backend/
  Ecommerce.slnx              # solution file (new .slnx format, not .sln)
  src/
    Ecommerce.Domain/         # entities, constants — no framework dependencies
    Ecommerce.Application/    # use cases, DTOs, validators
    Ecommerce.Infrastructure/ # EF Core, Identity, external integrations
    Ecommerce.Api/            # controllers, composition root (Program.cs)
  tests/
    Ecommerce.Application.Tests/
    Ecommerce.Api.Tests/
frontend/
  src/
    storefront/               # customer-facing app ("/")
    admin/                    # back-office app ("/admin")
    lib/                      # api client, query client, shared components
    app/router.tsx            # single router covering both route trees
docker-compose.yml
.env.example
```

## Getting started

### Option A — Docker Compose (recommended)

```bash
cp .env.example .env      # adjust values, especially JWT_KEY/POSTGRES_PASSWORD before any real use
docker compose up --build
```

- Frontend: http://localhost:5173
- Backend: http://localhost:5000 (Swagger at `/swagger`, health at `/health`)
- Postgres: localhost:5432

The backend applies EF Core migrations automatically on startup when `ApplyMigrationsOnStartup=true` (default in `.env.example`, dev-only behavior).

### Option B — Run locally without Docker

Requires: .NET 9 SDK, Node.js 20+, a local PostgreSQL instance.

```bash
# Backend
cd backend
dotnet restore
dotnet ef database update --project src/Ecommerce.Infrastructure --startup-project src/Ecommerce.Api
dotnet run --project src/Ecommerce.Api
```

`dotnet run` uses `appsettings.Development.json`, which points at `localhost:5432` with dev-only placeholder credentials — adjust if your local Postgres differs.

```bash
# Frontend
cd frontend
cp .env.example .env
npm install
npm run dev
```

## Testing

```bash
cd backend
dotnet test
```

```bash
cd frontend
npm run build   # runs tsc -b then vite build
```

## Documentation

- [`CLAUDE.md`](./CLAUDE.md) — full product/technical specification and working rules for this repository.
- [`PROJECT_CONTEXT.md`](./PROJECT_CONTEXT.md) — living project memory: what's implemented, decisions made, known issues, next steps. Read this first when picking up work.
- [`CHANGELOG.md`](./CHANGELOG.md) — dated log of meaningful changes.
