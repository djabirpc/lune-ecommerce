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
- TanStack Query v5 (catalog reads)
- React Hook Form + Zod + `@hookform/resolvers` (checkout + order-tracking forms)
- No frontend state library beyond React Context — cart is a single `CartContext` + localStorage

Infrastructure:
- Docker + Docker Compose (postgres, backend, frontend)
- PostgreSQL 16 (alpine image)

## Current Architecture

Backend follows a 4-project modular architecture under `backend/src/`:

```text
Ecommerce.Domain          — framework-agnostic entities/constants (Entity base class, Roles, Catalog/*, Inventory/*, Orders/*)
Ecommerce.Application     — use cases, DTOs, validators, service interfaces (IAuthService/ITokenService/ICategoryService/IProductService/IInventoryService/IOrderService), AppException hierarchy
Ecommerce.Infrastructure  — EF Core, Identity (ApplicationUser/ApplicationRole/RefreshToken), Catalog/Inventory/Orders service implementations, AppDbContext, IdentitySeeder, AddInfrastructure()
Ecommerce.Api             — controllers (Auth/Categories/Products/Inventory/Orders), Program.cs composition root, Swagger, JWT, health checks, global exception middleware
```

Auth-related namespaces worth knowing:
- `Ecommerce.Application.Auth` — `IAuthService`, `ITokenService`, `Dtos/` (LoginRequest, RefreshTokenRequest, AuthResponse, CurrentUserResponse), `Validators/`
- `Ecommerce.Application.Common.Exceptions` — `AppException` + `UnauthorizedAppException`/`ValidationAppException`/`NotFoundAppException`/`ConflictAppException`, all mapped to the `{ success, error }` shape by `Ecommerce.Api.Middleware.ExceptionHandlingExtensions.UseAppExceptionHandling()`
- `Ecommerce.Infrastructure.Identity` — `ApplicationUser`, `ApplicationRole`, `RefreshToken`, `TokenService`, `AuthService`
- `Ecommerce.Infrastructure.Persistence.IdentitySeeder` — seeds roles + optional bootstrap SUPER_ADMIN, called from `Program.cs` alongside the dev-only auto-migration

Catalog/Inventory namespaces worth knowing:
- `Ecommerce.Domain.Catalog` — `Category`, `Product`, `ProductVariant`, `ProductImage` (plain entities, no framework dependency)
- `Ecommerce.Domain.Inventory` — `InventoryRecord` (named to avoid a type-equals-namespace clash with `Ecommerce.Domain.Inventory`), `InventoryTransaction`, `InventoryTransactionType` enum (Reserve/Release/Sale/Return/Restock/Adjustment, matches CLAUDE.md section 10 exactly, stored as string in DB)
- `Ecommerce.Application.Catalog` / `Ecommerce.Application.Inventory` — `ICategoryService`/`IProductService`/`IInventoryService`, DTOs, FluentValidation validators (including a shared `SlugValidationRule.MustBeAValidSlug()` extension)
- `Ecommerce.Infrastructure.Catalog.{CategoryService,ProductService}` / `Ecommerce.Infrastructure.Inventory.InventoryService` — EF Core-backed implementations. `InventoryService` uses `ExecuteUpdateAsync` with a guard predicate in the `Where` clause (e.g. `AvailableQuantity >= quantity`) for every stock mutation — this is an atomic, race-condition-safe SQL `UPDATE ... WHERE` (no read-then-write), which is how "never allow overselling" (CLAUDE.md section 10) is actually enforced under concurrent requests. If you add new stock-mutating logic, follow this exact pattern rather than loading the entity and saving it back.
- `Ecommerce.Domain.Identity.Roles.CatalogManagers` — comma-joined `SUPER_ADMIN,ADMIN,STOCK_MANAGER` constant for `[Authorize(Roles = ...)]` on catalog/inventory write endpoints

Orders namespaces worth knowing:
- `Ecommerce.Domain.Orders` — `Order`, `OrderItem` (price/name **snapshotted** at order time, never re-read from the product later), `OrderStatusHistory`, `OrderStatus`/`DeliveryType`/`PaymentStatus` enums
- `Ecommerce.Application.Orders.IOrderService` / `Ecommerce.Infrastructure.Orders.OrderService` — `CreateAsync` (validates → reserves stock for every line inside one DB transaction, all-or-nothing) and `ChangeStatusAsync` (validates the transition against a hardcoded `AllowedTransitions` map before doing anything, then applies the matching inventory side effect: `Cancelled`/`Refused` → `ReleaseAsync`, `Delivered` → `RecordSaleAsync` + `PaymentStatus = Collected`, `Returned` → `RecordReturnAsync`, everything else is a no-op on inventory)
- `Ecommerce.Domain.Identity.Roles.OrderManagers` — `SUPER_ADMIN,ADMIN,ORDER_MANAGER,CONFIRMATION_AGENT` for order admin endpoints
- Program.cs registers a global `JsonStringEnumConverter` on the MVC JSON options, so `OrderStatus`/`DeliveryType`/`PaymentStatus` (and any future API-facing enum) (de)serialize as PascalCase strings (e.g. `"PendingConfirmation"`), matching the CLAUDE.md section 45 code-naming example — **not** the SCREAMING_SNAKE_CASE used in the workflow diagram in section 12, which is diagram notation, not a literal contract. Test HTTP clients must configure a matching `JsonSerializerOptions` with the same converter (see `OrderWorkflowTests.JsonOptions`) — `HttpContent.ReadFromJsonAsync`/`PostAsJsonAsync` use the .NET default options otherwise and will throw trying to parse a string into an enum.

Solution file is `backend/Ecommerce.slnx` (new XML solution format used by .NET 10 SDK tooling — NOT `.sln`; keep this in mind when referencing it from Dockerfiles or scripts).

Tests live under `backend/tests/`:
- `Ecommerce.Application.Tests` (xUnit, references Domain/Application) — includes `Auth/*ValidatorTests`, `Catalog/CreateProductRequestValidatorTests`, `Inventory/AdjustInventoryRequestValidatorTests`, `Orders/CreateOrderRequestValidatorTests`
- `Ecommerce.Api.Tests` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`) — `HealthCheckTests` (lenient, no real DB needed), `AuthEndpointsTests`, `CatalogEndpointsTests`, and `OrderWorkflowTests` (all three feature test classes share `AuthWebApplicationFactory`, which spins up a real ephemeral Postgres via **Testcontainers** and seeds a SUPER_ADMIN — requires Docker to be running, `dotnet test` will fail/hang without it). `CatalogEndpointsTests` includes a test that resolves `IInventoryService` directly from `factory.Services` to exercise `ReserveAsync`/`ReleaseAsync` (not exposed via HTTP yet) and assert the oversell guard throws `ConflictAppException`. `OrderWorkflowTests` covers the two exact scenarios required by CLAUDE.md section 29 (`Create order → Reserve stock → Cancel → Release stock` and `Create order → Reserve stock → Confirm → Prepare → Ship → Deliver`), an invalid-transition-rejected case, a guest phone-verified tracking case, and a multi-item checkout where one item is out of stock — asserting the **whole order** (and the other item's reservation) rolls back, not just the failing line.

Frontend is a single Vite app with two route trees sharing one React app (`frontend/src/app/router.tsx`):

```text
frontend/src/
  storefront/   — customer-facing layout + pages (StorefrontLayout wraps "/")
  admin/        — back-office layout + pages (AdminLayout wraps "/admin") — still all PagePlaceholder
  lib/api/      — fetch-based apiClient (reads VITE_API_URL) + TanStack QueryClient + types.ts (hand-written, mirrors backend DTOs) + catalog.ts/orders.ts (API functions)
  lib/cart/     — CartContext (React Context + localStorage, key "luna-cart")
  lib/format/   — formatPrice (DZD), ORDER_STATUS_LABELS/DELIVERY_TYPE_LABELS (French labels for backend enums)
  lib/components/ — PagePlaceholder, ProductCard, OrderDetailsCard (shared across storefront pages)
  app/router.tsx  — single createBrowserRouter with both route trees
```

`types.ts` is hand-written, not generated from the backend — if a backend DTO shape changes, this file must be updated manually (no codegen pipeline exists yet).

## Implemented Features

Yalidine, ZR Express, Promotions, and Marketing are intentionally not started. Auth, Products/Categories/Inventory, and now COD Orders exist (see below). No server-side Cart — checkout is a single-step guest submission (see Important Decisions).

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
- Admin route shell (`/admin/*`) with separate layout/navigation — still all `PagePlaceholder`, no admin login UI yet (see Pending Work).
- Docker Compose stack (postgres + backend + frontend) verified working end-to-end, including automatic EF Core migration + identity seeding on backend startup (dev only, gated by `ApplyMigrationsOnStartup` config key, not `ASPNETCORE_ENVIRONMENT`). Full login → me → refresh → logout cycle verified via curl against the containerized backend.
- **Catalog**: `Category`, `Product`, `ProductVariant` (color/size/SKU, mandatory per CLAUDE.md section 9 — stock is never tracked at product level), `ProductImage`. Public read endpoints (list/detail by slug) + admin write endpoints (`CatalogManagers` role: SUPER_ADMIN/ADMIN/STOCK_MANAGER). Creating a product with variants auto-creates one `InventoryRecord` per variant and logs an initial `RESTOCK` transaction if the initial quantity is > 0.
- **Inventory**: `InventoryRecord` (Available/Reserved/Sold/Returned/Damaged quantities per variant) + `InventoryTransaction` audit log (all 6 CLAUDE.md section 10 transaction types modeled). `Restock`/`Adjust` exposed via `POST /api/inventory/{restock,adjust}` (admin only). `Reserve`/`Release`/`RecordSale`/`RecordReturn` implemented and tested at the service level, and **now actually called** by the Order feature below. All stock mutations use EF Core's `ExecuteUpdateAsync` with a guard predicate (atomic `UPDATE ... WHERE available >= quantity`), which is what actually prevents overselling under concurrent requests.
- **Orders / COD checkout**: `POST /api/orders` (guest, public) validates the request, snapshots each variant's current price (never trusts a client-supplied price, per CLAUDE.md section 41), and reserves stock for every line **inside a single DB transaction** — if any line is out of stock, the entire order (and every reservation already made for earlier lines) rolls back, no partial order is ever created. `GET /api/orders/track?orderNumber=&phone=` lets a guest check status without an account, requiring the phone to match (prevents order-number enumeration). Admin: `GET /api/orders` (paged, filterable by status), `GET /api/orders/{id}`, `POST /api/orders/{id}/status` — the last one enforces a hardcoded state-transition map (CLAUDE.md section 12: *"Do not allow arbitrary status changes"*) and triggers the matching inventory effect (release on Cancelled/Refused, finalize-as-sold on Delivered, record-return on Returned). Every transition is appended to `OrderStatusHistory` (old/new status, acting user if any, reason). Order numbers are human-readable (`LUNA-YYMMDD-NNNN`) with a uniqueness retry loop, not a real invoice sequence yet.
- **Storefront purchase journey (frontend, real data)**: `/categories`, `/category/:slug`, `/product/:slug` (color→size variant picker, live stock, add-to-cart), `/cart`, `/checkout` (React Hook Form + Zod, same validation rules as the backend), `/order-confirmation/:orderNumber` (shows the just-placed order via router state, with a graceful fallback if the page is reloaded), `/track-order` (phone-verified lookup). Cart is `CartContext` (React Context + localStorage), submitted as a flat item list directly to `POST /api/orders` at checkout — matches the "no server-side Cart" decision. Verified with a real headless-browser run against the live Docker stack (see Notes in CHANGELOG). `HomePage` and admin pages are unchanged (still placeholders/static).

## Current Feature

None in progress. Auth, Catalog, Inventory, core Order/COD workflow, and the full customer-facing storefront purchase journey are complete. Next up: admin UI (login, product/order management) — see Next Recommended Steps.

## Last Completed Work

2026-09-01 — Storefront frontend wired to the real backend: category/product browsing with live stock, a client-side cart, a checkout form that posts directly to `POST /api/orders`, order confirmation, and phone-verified order tracking. Verified end-to-end with a real headless-Chromium run (Playwright) against the Dockerized backend + Vite dev frontend — not just `npm run build`: the full click-through (browse → select variant → add to cart → checkout → confirm → cart clears → track by phone → wrong-phone correctly rejected) was exercised and screenshotted. No backend changes in this pass. `HomePage` and all `/admin` pages remain as before (untouched).

Earlier same-date milestones: Orders & COD checkout (state machine + inventory integration, both CLAUDE.md section 29 test scenarios), Catalog & Inventory (`Category`/`Product`/`ProductVariant`/`ProductImage`, `InventoryRecord`/`InventoryTransaction`, atomic never-oversell stock mutations), authentication (JWT login/refresh/logout/me, role + bootstrap SUPER_ADMIN seeding, standardized error responses — and a real eager-`IConfiguration`-read bug fixed along the way), and the original technical bootstrap (monorepo scaffold, backend/frontend skeletons, Docker Compose stack). See `CHANGELOG.md` for full bullet lists of each.

## Database

PostgreSQL via Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4.

Entities that currently exist (Identity only, via `AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`):
- `ApplicationUser` (`Users` table) — extends `IdentityUser<Guid>` with `FirstName`, `LastName`, `IsActive`, `CreatedAtUtc`
- `ApplicationRole` (`Roles` table) — extends `IdentityRole<Guid>`
- Standard Identity join tables: `UserRoles`, `UserClaims`, `UserLogins`, `RoleClaims`, `UserTokens`
- `RefreshToken` (`RefreshTokens` table) — `UserId`, `TokenHash` (SHA-256, unique), `ExpiresAtUtc`, `RevokedAtUtc`, `ReplacedByTokenHash`; `IsActive` computed as not-revoked-and-not-expired

Catalog & Inventory entities (`Ecommerce.Domain.Catalog` / `Ecommerce.Domain.Inventory`):
- `Category` (`Categories` table) — `Name`, `Slug` (unique), `Description`, `IsActive`, `DisplayOrder`
- `Product` (`Products` table) — `CategoryId` (FK, `Restrict` on delete), `Name`, `Slug` (unique), `Description`, `Price` (numeric(10,2)), `IsActive`
- `ProductVariant` (`ProductVariants` table) — `ProductId` (FK, `Cascade`), `Color`, `Size`, `Sku` (unique across the whole catalog), `PriceOverride` (nullable numeric(10,2)), `IsActive`
- `ProductImage` (`ProductImages` table) — `ProductId` (FK, `Cascade`), `Url`, `AltText`, `DisplayOrder`, `IsPrimary`
- `InventoryRecord` (`Inventory` table) — one row per `ProductVariantId` (unique index, FK `Cascade`), `AvailableQuantity`/`ReservedQuantity`/`SoldQuantity`/`ReturnedQuantity`/`DamagedQuantity`
- `InventoryTransaction` (`InventoryTransactions` table) — `ProductVariantId` (FK `Restrict` — history must survive even if a variant is later restricted from deletion), `Type` (string-converted enum: RESERVE/RELEASE/SALE/RETURN/RESTOCK/ADJUSTMENT), `Quantity`, `Reason`

Order entities (`Ecommerce.Domain.Orders`):
- `Order` (`Orders` table) — `OrderNumber` (unique, `LUNA-YYMMDD-NNNN`), `Status` (string-converted enum), customer/delivery fields (`FirstName`/`LastName`/`Phone`/`Wilaya`/`Commune`/`Address`/`DeliveryType`/`Notes`), `PaymentMethod` (fixed `"COD"`), `PaymentStatus`, `Subtotal`/`ShippingCost`/`Total` (all numeric(10,2); `ShippingCost` is currently always 0 — no shipping/carrier integration yet)
- `OrderItem` (`OrderItems` table) — `OrderId` (FK `Cascade`), `ProductVariantId` (no FK constraint — items must survive even if the catalog changes later), **snapshotted** `ProductName`/`Color`/`Size`/`Sku`/`UnitPrice` (never re-read from the live product) plus `Quantity`/`LineTotal`
- `OrderStatusHistory` (`OrderStatusHistories` table) — `OrderId` (FK `Cascade`), `OldStatus`/`NewStatus` (string-converted enums), `ChangedByUserId` (nullable Guid — null for system/guest-triggered creation), `Reason`

Migrations: `backend/src/Ecommerce.Infrastructure/Persistence/Migrations/` — `InitialIdentity`, `AddRefreshTokens`, `AddCatalogAndInventory`, `AddOrders` (all applied and verified against a live Postgres container).

Planned entities (not yet created): Customer, CustomerAddress, OrderCallAttempt (confirmation-center call log, deferred — see Important Decisions), Promotion, Shipment, TrackingEvent, ShippingRate, MarketingEvent, AuditLog. No Cart/CartItem entity is planned — see Important Decisions.

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

Catalog (public reads, `CatalogManagers` role = SUPER_ADMIN/ADMIN/STOCK_MANAGER for writes):
- `GET /api/categories` / `GET /api/categories/{slug}` — public; `POST /api/categories` / `PUT /api/categories/{id}` — admin
- `GET /api/products?category={slug}&page=&pageSize=` (paged, active only) / `GET /api/products/{slug}` (full detail incl. variants + live stock) — public; `POST /api/products` (with initial variants) / `PUT /api/products/{id}` / `POST /api/products/{id}/variants` — admin
- `GET /api/inventory/{variantId}` / `GET /api/inventory/{variantId}/transactions` / `POST /api/inventory/restock` / `POST /api/inventory/adjust` — admin only (whole controller is `[Authorize(Roles = Roles.CatalogManagers)]`)

Orders (guest checkout is public; admin endpoints need `OrderManagers` role = SUPER_ADMIN/ADMIN/ORDER_MANAGER/CONFIRMATION_AGENT):
- `POST /api/orders` — public, creates a COD order from `{ firstName, lastName, phone, wilaya, commune, address, deliveryType, notes?, items: [{ productVariantId, quantity }] }`. `409 CONFLICT` (not `400`) if any item is out of stock — the whole order fails atomically.
- `GET /api/orders/track?orderNumber=&phone=` — public, guest order lookup; `404` unless both match (no order-number enumeration).
- `GET /api/orders?status=&page=&pageSize=`, `GET /api/orders/{id}` — admin.
- `POST /api/orders/{id}/status` — admin, `{ newStatus, reason? }`. `409 CONFLICT` if the transition isn't in the allowed map for the order's current status.

Errors follow `{ success: false, error: { code, message } }` (CLAUDE.md section 27) — codes seen so far: `UNAUTHORIZED`, `VALIDATION_ERROR`, `INTERNAL_ERROR`, `NOT_FOUND`, `CONFLICT` (see `Ecommerce.Application.Common.Exceptions.AppException` for the full set).

No shipping provider integrations exist yet (Yalidine/ZR Express are explicitly out of scope for this step per CLAUDE.md section 16/17 — do not invent endpoints).

## Frontend

Routing is centralized in `frontend/src/app/router.tsx` (React Router v7 `createBrowserRouter`), two top-level route trees:

Storefront (`StorefrontLayout` at `/`): `/`, `/categories`, `/category/:slug`, `/product/:slug`, `/promotions`, `/cart`, `/checkout`, `/order-confirmation/:orderNumber`, `/track-order`, `/orders`, `/account`.

Admin (`AdminLayout` at `/admin`): `/admin` (+ `/admin/dashboard`), `/admin/orders`, `/admin/orders/confirmation`, `/admin/orders/:id`, `/admin/products`, `/admin/inventory`, `/admin/promotions`, `/admin/customers`, `/admin/shipping`, `/admin/marketing`, `/admin/users`, `/admin/settings`.

**Storefront status**: `HomePage` (static hero/trust copy), `CategoriesPage`, `CategoryPage`, `ProductPage`, `CartPage`, `CheckoutPage`, `OrderConfirmationPage`, `TrackOrderPage` all use real backend data via TanStack Query / direct `apiClient` calls. `PromotionsPage`, `OrdersPage` (storefront "my orders" — not meaningful without customer accounts), and `AccountPage` remain `PagePlaceholder` (no backend feature to back them: Promotions isn't built, and there's no customer login).

**Admin status**: every `/admin/*` page is still `PagePlaceholder`. No admin login UI exists yet even though the backend `/api/auth/*` endpoints are ready.

`apiClient` (`frontend/src/lib/api/client.ts`) is a thin `fetch` wrapper reading `VITE_API_URL`, expects the backend's `{ success, error: { code, message } }` error shape (CLAUDE.md section 27) and throws `ApiError`. It does not currently attach an `Authorization` header — fine for the storefront (every endpoint it calls is public), but the admin UI work will need to extend it with token attachment + refresh-on-401 retry.

`ProductPage`'s variant picker groups `ProductDetailDto.variants` client-side into color → size (the backend returns a flat list, one entry per SKU) — there's no variant-matrix endpoint, this is pure frontend grouping logic.

## Shipping

- `IShippingProvider` abstraction: **not started**.
- Yalidine: **not started** — no credentials/docs available yet; do not invent endpoints when this is picked up (CLAUDE.md section 16).
- ZR Express: **not started** — same caveat (CLAUDE.md section 17).
- FakeShippingProvider: **not started** (mandatory once shipping work begins, per CLAUDE.md section 15).

## COD Workflow

Implemented: `Order`/`OrderItem`/`OrderStatusHistory`, the full CLAUDE.md section 12 status machine (`PendingConfirmation → Confirmed → Preparing → ReadyToShip → Shipped → OutForDelivery → Delivered`, plus `Cancelled`/`CustomerUnreachable`/`DeliveryFailed`/`Refused`/`Returned`), and every transition recorded in `OrderStatusHistory`. Payment is COD-only (`PaymentStatus` moves `Pending → Collected` on `Delivered`).

**Not implemented**: the CLAUDE.md section 13 confirmation-center workflow — `OrderCallAttempt` entity, "schedule callback", call-attempt logging. An admin can currently move an order to `Confirmed`/`CustomerUnreachable`/`Cancelled` directly, but there's no structured record of *why* (which phone attempt, agent notes). Deferred deliberately — see Important Decisions.

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
7. ~~Implement products/categories.~~ ✅
8. ~~Implement inventory.~~ ✅ — Restock/Adjust exposed via API; Reserve/Release/Sale/Return implemented and tested at service level, wiring to HTTP deferred to the Order feature that will actually call them.
9. ~~Implement storefront (real data, not placeholders).~~ ✅ — the purchase journey (categories/product/cart/checkout/confirmation/tracking) is real; `PromotionsPage`/storefront `OrdersPage`/`AccountPage` remain placeholders because there's no Promotions feature or customer accounts to back them.
10. ~~Implement COD checkout.~~ ✅
11. ~~Implement order workflow.~~ ✅ — core state machine + inventory integration done; confirmation-center (`OrderCallAttempt`) deferred, see Important Decisions.
12. Implement admin (real data, not placeholders) — still fully pending, including admin login UI.
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
3. ~~Inventory is managed at product-variant level (not yet built).~~ Built: one `InventoryRecord` per `ProductVariant`, never at `Product` level.
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
14. Overselling is prevented at the database-query level, not in application code: every `InventoryService` stock mutation is a single `ExecuteUpdateAsync` call whose `Where` clause encodes the invariant (e.g. `AvailableQuantity >= quantity`) so the guard and the write happen atomically in one SQL statement. Loading the entity, checking a condition in C#, then calling `SaveChangesAsync` would have a race window under concurrent requests — deliberately avoided.
15. `InventoryTransaction.Quantity` is always a positive magnitude for Reserve/Release/Sale/Return/Restock (the bucket transition is implied by `Type`), but can be **negative** for Adjustment (a manual correction can go either direction) — this is the one type where the sign is meaningful and must be preserved for the audit trail.
16. `Reserve`/`Release`/`RecordSale`/`RecordReturn` on `IInventoryService` are implemented and unit/integration-tested now, but intentionally have no HTTP endpoint yet — there is no real caller (Cart/Order) for them yet, and CLAUDE.md section 44 discourages building unused public API surface. They're ready for the Order feature to call directly.
17. Product `Slug` and `ProductVariant` `Sku` are supplied by the admin/caller (validated via a shared regex rule), not auto-generated from the name — kept simple for this pass; revisit if UX feedback asks for auto-slugging.
18. **No server-side Cart/CartItem entity.** Since guest checkout is mandatory and there are no customer accounts (CLAUDE.md section 11), there is no logged-in customer to sync a cart across devices for — persisting it server-side would add a full CRUD surface for zero real benefit right now. The frontend cart lives in browser state (e.g. localStorage) and is submitted as a flat `items: [{ productVariantId, quantity }]` list directly to `POST /api/orders` at checkout. Revisit only if a real requirement emerges (e.g. cart abandonment recovery emails).
19. `OrderCallAttempt` / confirmation-center workflow (CLAUDE.md section 13) is deliberately deferred. The core order state machine already supports moving an order to `Confirmed`/`CustomerUnreachable`/`Cancelled`; what's missing is the structured *call log* (attempt number, agent, result, next callback time) that the admin confirmation-center UI would need. Building it now, before there's an admin UI to consume it, would be exactly the kind of unused-surface CLAUDE.md section 44 warns against.
20. Order status transitions are enforced via a hardcoded `Dictionary<OrderStatus, OrderStatus[]>` in `OrderService`, not a database-driven workflow engine — intentionally simple and exhaustive (12 states, ~20 edges), matches CLAUDE.md's explicit list of states, and is trivial to unit-test. Revisit only if the workflow needs to become configurable per-tenant or per-carrier.
21. `Order.ShippingCost` is hardcoded to 0 — there is no shipping-cost calculation yet since `IShippingProvider` doesn't exist. `Order.Total` will need recalculating once shipping rates are wired up; this is a known gap, not an oversight.
22. `OrderItem.ProductVariantId` has no FK constraint to `ProductVariants` (unlike `InventoryTransaction`, which does). Order history must remain intact and queryable even if a product/variant is later deleted from the catalog — the snapshot fields (`ProductName`, `Sku`, etc.) are what matters for a placed order, not a live join to the catalog.
23. Frontend `types.ts` DTOs are hand-written, not generated from the backend (no OpenAPI-codegen pipeline set up). Acceptable at the current size; revisit if the two start drifting often.
24. `OrderConfirmationPage` reads the just-created order from React Router navigation `state`, not a fresh API call — it doesn't have the customer's phone number handy to call the (phone-verified) track endpoint, and re-fetching without that verification would defeat the anti-enumeration protection on `GET /api/orders/track`. On a page reload (state lost), it shows a fallback pointing to `/track-order` instead of silently failing.
25. `apiClient` has no auth-token attachment yet — deliberately, since every endpoint the storefront currently calls is public (categories, products, order create/track). This will need to be added when the admin UI (which needs `Authorization: Bearer`) is built, not before.

## Next Recommended Steps

1. Admin UI in `/admin`: login page (needs `apiClient` to gain token attachment + refresh-on-401), then product/category/inventory management and an order list/detail/status-change screen, now that all the backend endpoints exist. This is now the biggest gap — the customer-facing storefront is real end-to-end, but staff have no UI to manage any of it (only curl/Swagger).
2. `OrderCallAttempt` + confirmation-center workflow (CLAUDE.md section 13), once the admin order UI exists to actually use it (see Important Decisions #19).
3. Promotions (CLAUDE.md section 20) — no dependency on anything unbuilt, could be picked up independently of the shipping/marketing work below.
4. Shipping: `IShippingProvider` + `FakeShippingProvider` abstraction (CLAUDE.md section 15), needed before `Order.ShippingCost` can be anything other than 0, and before Yalidine/ZR Express integration (both still blocked on real API docs/credentials per CLAUDE.md sections 16–17 — do not invent endpoints when picking this up).
5. Marketing tracking (pixels, UTM capture, attribution) — lowest priority until there's checkout traffic to actually track.
6. Smaller, can be done anytime: real product photography/images (`ProductImage`) — every product created so far has none, so the storefront always shows the "Pas d'image" placeholder box; not a bug, just no image upload flow or seeded image URLs yet.
