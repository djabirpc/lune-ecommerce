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
- **Authentication**: `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me`. JWT access tokens (HMAC-SHA256, `System.IdentityModel.Tokens.Jwt`) plus rotating, SHA-256-hashed refresh tokens persisted in a new `RefreshTokens` table (`Ecommerce.Infrastructure.Identity.RefreshToken`).
- `IdentitySeeder` (`Ecommerce.Infrastructure.Persistence`): seeds the 7 roles from `Ecommerce.Domain.Identity.Roles` and an optional initial `SUPER_ADMIN` user from the `InitialAdmin` config section (skipped if not configured); runs alongside the existing dev-only auto-migration.
- Standardized error handling: `AppException` hierarchy (`UnauthorizedAppException`, `ValidationAppException`, `NotFoundAppException`, `ConflictAppException`) in `Ecommerce.Application.Common.Exceptions`, plus `app.UseAppExceptionHandling()` middleware (`Ecommerce.Api.Middleware`) producing the `{ success, error: { code, message } }` shape from CLAUDE.md section 27 for both `AppException` and FluentValidation `ValidationException`.
- FluentValidation validators for `LoginRequest`/`RefreshTokenRequest`, invoked manually inside `AuthService` (no mediator pipeline yet).
- Migration `AddRefreshTokens`.
- Testcontainers-based integration tests (`AuthEndpointsTests`, `AuthWebApplicationFactory`) exercising the full login → me → refresh (rotation) → logout → refresh-reuse-rejected flow against a real, ephemeral PostgreSQL container (requires Docker to run this test class).
- `InitialAdmin` config section + `INITIAL_ADMIN_EMAIL`/`INITIAL_ADMIN_PASSWORD`/`INITIAL_ADMIN_FIRST_NAME`/`INITIAL_ADMIN_LAST_NAME` env vars (`.env.example`, `docker-compose.yml`); dev-only defaults added to `appsettings.Development.json` and `launchSettings.json` (`ApplyMigrationsOnStartup=true`) so `dotnet run` seeds a working admin locally.
- **Catalog & Inventory** (CLAUDE.md sections 9–10): `Category`/`Product`/`ProductVariant`/`ProductImage` domain entities (`Ecommerce.Domain.Catalog`) and `InventoryRecord`/`InventoryTransaction`/`InventoryTransactionType` (`Ecommerce.Domain.Inventory`, all 6 transaction types: RESERVE/RELEASE/SALE/RETURN/RESTOCK/ADJUSTMENT).
- `CategoriesController`/`ProductsController`: public `GET` (list/detail by slug, paged + category-filterable for products) and `CatalogManagers`-role-protected `POST`/`PUT` for both, plus `POST /api/products/{id}/variants`. Creating a product with variants auto-creates one `InventoryRecord` per variant and an initial `RESTOCK` transaction when quantity > 0.
- `InventoryController` (admin-only): `GET /api/inventory/{variantId}` (current stock), `GET /api/inventory/{variantId}/transactions` (audit log), `POST /api/inventory/restock`, `POST /api/inventory/adjust`.
- `IInventoryService.{ReserveAsync,ReleaseAsync,RecordSaleAsync,RecordReturnAsync}`: implemented and tested but not yet exposed via HTTP — ready for the upcoming Order feature to call.
- `Ecommerce.Domain.Identity.Roles.CatalogManagers` constant (`SUPER_ADMIN,ADMIN,STOCK_MANAGER`) for `[Authorize(Roles = ...)]` on catalog/inventory endpoints.
- Migration `AddCatalogAndInventory`.
- FluentValidation validators for all new request DTOs, including a shared `SlugValidationRule.MustBeAValidSlug()` rule and a duplicate-SKU check across a product's variants.
- `CatalogEndpointsTests` (Testcontainers, reuses `AuthWebApplicationFactory`): create category → create product with variant (asserts auto-created inventory) → restock → adjust → over-adjust rejected (409) → public list/detail still work → unauthenticated write rejected (401); plus a dedicated test resolving `IInventoryService` directly to prove `ReserveAsync` throws once stock is exhausted rather than going negative. Unit tests for `CreateProductRequestValidator` and `AdjustInventoryRequestValidator`.
- **Orders & COD checkout** (CLAUDE.md sections 11–12): `Order`/`OrderItem`/`OrderStatusHistory` domain entities (`Ecommerce.Domain.Orders`), `OrderStatus`/`DeliveryType`/`PaymentStatus` enums. `IOrderService`/`OrderService`: `CreateAsync` snapshots price/name per line (never trusts client-supplied prices), reserves stock for every line inside one DB transaction (all-or-nothing — a single out-of-stock item fails and rolls back the whole order and every prior reservation), and generates a human-readable unique order number (`LUNA-YYMMDD-NNNN`). `ChangeStatusAsync` enforces a hardcoded valid-transitions map (CLAUDE.md section 12: no arbitrary status changes) and triggers the matching `IInventoryService` call: `Cancelled`/`Refused` → `ReleaseAsync`, `Delivered` → `RecordSaleAsync` + `PaymentStatus = Collected`, `Returned` → `RecordReturnAsync`. Every transition is recorded in `OrderStatusHistory`.
- `OrdersController`: `POST /api/orders` (public, guest checkout), `GET /api/orders/track?orderNumber=&phone=` (public, phone-verified guest tracking — prevents order-number enumeration), `GET /api/orders` / `GET /api/orders/{id}` / `POST /api/orders/{id}/status` (`OrderManagers` role: SUPER_ADMIN/ADMIN/ORDER_MANAGER/CONFIRMATION_AGENT).
- `Ecommerce.Domain.Identity.Roles.OrderManagers` constant.
- Global `JsonStringEnumConverter` added to the MVC JSON options (`Program.cs`) so API enums serialize as PascalCase strings (e.g. `"PendingConfirmation"`) instead of raw numbers.
- Migration `AddOrders`.
- `OrderWorkflowTests` (Testcontainers): the two exact scenarios required by CLAUDE.md section 29 (`Create order → Reserve stock → Cancel → Release stock`; `Create order → Reserve stock → Confirm → Prepare → Ship → Deliver`, verifying `PaymentStatus` becomes `Collected` and stock moves to Sold on delivery), an invalid-transition-rejected (409) case, a multi-item-checkout-with-one-out-of-stock-item rollback case, and guest tracking with correct/incorrect phone. `CreateOrderRequestValidatorTests` (Algerian phone format, duplicate variant lines, empty items).

### Changed
- N/A (first commit-worthy state of the repository).

### Fixed
- Removed the `Microsoft.AspNetCore.OpenApi` package reference from `Ecommerce.Api.csproj`: it pulls a `Microsoft.OpenApi` 1.x dependency that conflicts with Swashbuckle's `Microsoft.OpenApi` 2.7.5 and crashes controller discovery at runtime (`ReflectionTypeLoadException`).
- **Config bug**: `AddInfrastructure`, the health check registration, and the JWT bearer setup all read `IConfiguration` **eagerly** (`var x = configuration.GetConnectionString(...)`) before `WebApplicationBuilder.Build()`. This silently ignored any configuration added after that point — including `WebApplicationFactory` test overrides — and would equally ignore any config source composed late in a real deployment. Fixed by resolving configuration **lazily** via DI: `AddDbContext<AppDbContext>((sp, options) => ...)`, `AddNpgSql(sp => ...)`, and `AddOptions<JwtBearerOptions>().Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) => ...)` combined with `AddOptions<JwtOptions>().Bind(...).Validate(...).ValidateOnStart()` for fail-fast startup validation that still sees the final merged configuration.

### Database
- Migration `InitialIdentity` (`backend/src/Ecommerce.Infrastructure/Persistence/Migrations/`): creates the Identity schema only (`Users`, `Roles`, `UserRoles`, `UserClaims`, `UserLogins`, `RoleClaims`, `UserTokens`). No business tables yet.
- Migration `AddRefreshTokens`: adds the `RefreshTokens` table (hashed token, expiry, revocation, rotation chain via `ReplacedByTokenHash`).
- Migration `AddCatalogAndInventory`: adds `Categories`, `Products`, `ProductVariants`, `ProductImages`, `Inventory` (one row per variant, unique index on `ProductVariantId`), `InventoryTransactions`.
- Migration `AddOrders`: adds `Orders`, `OrderItems`, `OrderStatusHistories`.

### API
- `GET /health`, `GET /api/system/ping` added. No business endpoints yet.
- `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me` added.
- `GET/POST/PUT /api/categories[/{id|slug}]`, `GET/POST/PUT /api/products[/{id|slug}]`, `POST /api/products/{id}/variants`, `GET/POST /api/inventory/...` added.
- `POST /api/orders`, `GET /api/orders/track`, `GET /api/orders[/{id}]`, `POST /api/orders/{id}/status` added.

### Frontend
- Initial storefront and admin route shells added (see PROJECT_CONTEXT.md for the full route list). No business data wired up yet. Not yet wired to the new auth, catalog, or order endpoints — this is now the primary gap (backend feature-complete for auth/catalog/COD-order-workflow, frontend still all placeholders).

### Notes
- This started as a foundation-only bootstrap (no Products, Orders, COD, Yalidine, or ZR Express) and now has authentication, a Products/Categories/Inventory catalog, and a full COD order workflow with stock reservation; Cart (deliberately not built — see PROJECT_CONTEXT.md Important Decisions), Yalidine/ZR Express/Promotions/Marketing remain out of scope.
- Full backend test suite (`dotnet test` from `backend/`) passes: 43/43 tests (28 Application.Tests, 15 Api.Tests — the latter requires Docker for the Testcontainers-based auth/catalog/order tests).
- Full stack verified end-to-end via `docker compose up` (postgres healthy, backend healthy + migrated, frontend serving both `/` and `/admin`, CORS confirmed working from `http://localhost:5173`); a real login/me/refresh/logout cycle, a real category→product→restock→adjust→oversell-rejected cycle, and a real guest-checkout→inventory-reserved→track→cancel→inventory-released cycle were all exercised via curl against the containerized backend.
