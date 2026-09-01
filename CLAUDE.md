# CLAUDE.md

# LUNA — CUSTOM ALGERIAN E-COMMERCE PLATFORM

## 1. PROJECT OVERVIEW

This repository contains a complete custom e-commerce platform for a fashion business operating in Algeria.

Brand name for the prototype:

**Luna.**

The platform is designed specifically for the Algerian e-commerce market.

The primary payment method is:

**COD — Cash On Delivery / Paiement à la livraison**

Customers do not pay online.

The complete business workflow is:

Customer visits website
→ selects product
→ selects size/color
→ checkout
→ enters phone/wilaya/commune/address
→ creates COD order
→ order requires phone confirmation
→ order is prepared
→ shipment is created
→ package is delivered
→ customer pays
→ order completed

---

# 2. CRITICAL RULE — THIS IS NOT SHOPIFY

This project is a completely custom e-commerce application.

DO NOT introduce:

- Shopify
- WooCommerce
- Shopify Checkout
- Shopify Storefront API
- Shopify Liquid
- Stripe
- PayPal
- external e-commerce platforms

Our application owns:

- Products
- Categories
- Product variants
- Inventory
- Customers
- Cart
- Orders
- COD
- Promotions
- Shipping
- Tracking
- Marketing attribution
- Analytics
- Administration

The backend is the source of truth.

---

# 3. TECHNOLOGY STACK

## Backend

- ASP.NET Core .NET 9
- C#
- Entity Framework Core
- PostgreSQL
- Npgsql
- ASP.NET Core Identity
- JWT authentication
- FluentValidation
- Serilog
- Swagger / OpenAPI

## Frontend

- React
- TypeScript
- Vite
- Tailwind CSS
- React Router
- TanStack Query
- React Hook Form
- Zod

## Infrastructure

- Docker
- Docker Compose
- PostgreSQL

---

# 4. ARCHITECTURE

Use a pragmatic modular architecture.

Backend:

```text
backend/
└── src/
    ├── Ecommerce.Api
    ├── Ecommerce.Application
    ├── Ecommerce.Domain
    └── Ecommerce.Infrastructure
```

Responsibilities:

### Domain

Business entities and business rules.

### Application

Use cases, services, DTOs, validators and interfaces.

### Infrastructure

EF Core, PostgreSQL, shipping providers, external APIs, file storage, etc.

### API

HTTP endpoints, authentication, middleware and API configuration.

---

# 5. FRONTEND ARCHITECTURE

The React application has two major areas.

## Storefront

Customer-facing application.

```text
/
 /categories
 /category/:slug
 /product/:slug
 /promotions
 /cart
 /checkout
 /order-confirmation/:orderNumber
 /track-order
 /orders
 /account
```

## Admin

Private back-office application.

```text
/admin
/admin/dashboard
/admin/orders
/admin/orders/:id
/admin/orders/confirmation
/admin/products
/admin/inventory
/admin/promotions
/admin/customers
/admin/shipping
/admin/marketing
/admin/users
/admin/settings
```

The storefront and admin must have separate layouts and navigation.

---

# 6. MOBILE FIRST

This is a mobile-first application.

The majority of customers will arrive from:

- Facebook
- Instagram
- TikTok

Therefore:

**Mobile UX has priority over desktop UX.**

Primary target:

390px width.

Also test:

- 375px
- 414px
- tablet
- desktop

Do not design desktop first and simply shrink it.

The mobile experience must be intentionally designed.

---

# 7. STOREFRONT DESIGN

The approved visual direction is:

- premium
- feminine
- modern
- minimal
- elegant
- clean
- fashion-oriented

Primary visual identity:

- white
- cream
- black
- dark gray
- pink/coral accent

Product photography is the main visual element.

Do not overload the UI.

---

# 8. HOMEPAGE

Homepage sections:

1. Announcement bar

"Livraison partout en Algérie • Paiement à la livraison"

2. Header

3. Hero

"La mode qui vous ressemble."

Buttons:

"Découvrir la collection"

"Voir les promotions"

4. Promotion banner

Example:

"Soldes d'été"

"Jusqu'à -40%"

5. Trust section

- Livraison partout en Algérie
- Paiement à la livraison
- Échange facile
- Satisfait ou remboursé

6. Categories

7. Nouveautés

8. Best sellers

9. Flash sale

10. Promotion banners

11. Instagram / TikTok

12. Newsletter

13. Footer

---

# 9. PRODUCT SYSTEM

Products have variants.

Example:

Robe longue fleurie

Beige / S
Beige / M
Beige / L
Noir / S
Noir / M
Noir / L

Stock MUST be managed per variant.

Never manage clothing inventory only at product level.

---

# 10. INVENTORY

Inventory must support:

- Available quantity
- Reserved quantity
- Sold quantity
- Returned quantity
- Damaged quantity

Create inventory transactions.

Possible transaction types:

```text
RESERVE
RELEASE
SALE
RETURN
RESTOCK
ADJUSTMENT
```

Never allow overselling.

---

# 11. COD

COD is the primary payment method.

Do NOT add online payment unless explicitly requested later.

Checkout fields:

- Nom et prénom
- Numéro de téléphone
- Wilaya
- Commune
- Adresse
- Delivery type

Delivery types:

- Home delivery
- Stop Desk

Guest checkout is mandatory.

A customer does NOT need an account to place an order.

---

# 12. ORDER WORKFLOW

Main workflow:

```text
PENDING_CONFIRMATION
        ↓
CONFIRMED
        ↓
PREPARING
        ↓
READY_TO_SHIP
        ↓
SHIPPED
        ↓
OUT_FOR_DELIVERY
        ↓
DELIVERED
```

Other states:

```text
CANCELLED
CUSTOMER_UNREACHABLE
DELIVERY_FAILED
REFUSED
RETURNED
```

Do not allow arbitrary status changes.

Implement valid transitions.

Every status transition must be recorded in history.

---

# 13. ORDER CONFIRMATION

Create a dedicated confirmation workflow.

Orders waiting for confirmation:

```text
PENDING_CONFIRMATION
CUSTOMER_UNREACHABLE
```

The agent can:

- Confirm
- Cancel
- Mark no answer
- Schedule callback

Record every phone attempt.

Entity:

```text
OrderCallAttempt
```

Fields should include:

- OrderId
- AgentUserId
- AttemptNumber
- Result
- Notes
- CalledAt
- NextCallAt

---

# 14. ORDER HISTORY

Every important order event must be auditable.

Create:

```text
OrderStatusHistory
```

Track:

- old status
- new status
- user
- reason
- date

Never silently change important business state.

---

# 15. SHIPPING ARCHITECTURE

Shipping providers MUST be abstracted.

Create:

```text
IShippingProvider
```

Do NOT put Yalidine logic directly into OrderService.

Architecture:

```text
Order
   ↓
ShippingService
   ↓
IShippingProvider
   ├── YalidineShippingProvider
   ├── ZRExpressShippingProvider
   └── FakeShippingProvider
```

The FakeShippingProvider is mandatory for development and testing.

---

# 16. YALIDINE

Yalidine must be implemented as an adapter.

Never put API credentials in React.

Credentials must come from environment variables / secure configuration.

Possible configuration:

```text
YALIDINE_BASE_URL
YALIDINE_API_ID
YALIDINE_API_TOKEN
```

The exact configuration must follow the actual merchant API contract.

Implement only documented functionality.

Potential capabilities:

- locations
- shipping rates
- shipment creation
- tracking
- shipment status
- label/bordereau
- cancellation if supported

IMPORTANT:

Never invent API endpoints.

Never guess authentication.

If official documentation or credentials are unavailable:

Create the adapter structure and clearly document what is missing.

---

# 17. ZR EXPRESS

Implement:

```text
ZRExpressShippingProvider
```

Do not assume ZR Express uses the same authentication or endpoints as Yalidine.

Use the actual API contract supplied by the merchant.

Never invent undocumented endpoints.

Credentials must remain server-side.

---

# 18. SHIPPING STATUS NORMALIZATION

External carriers can use different status names.

Normalize them internally.

Example:

```text
CREATED
PICKED_UP
IN_TRANSIT
AT_DESTINATION
OUT_FOR_DELIVERY
DELIVERED
FAILED
REFUSED
RETURNED
CANCELLED
UNKNOWN
```

Store both:

- provider status
- normalized status

---

# 19. SHIPPING SYNCHRONIZATION

If a carrier supports webhooks, implement secure webhooks.

Otherwise use a background synchronization service.

Do not continuously poll carrier APIs aggressively.

Use configurable intervals.

---

# 20. PROMOTIONS

Promotions must be a real backend feature.

Do not hardcode promotional prices in React.

Support:

```text
PRODUCT_DISCOUNT
CATEGORY_DISCOUNT
FLASH_SALE
PERCENTAGE_DISCOUNT
FIXED_AMOUNT_DISCOUNT
BUY_X_GET_Y
FREE_SHIPPING
COUPON
```

Promotions must support:

- start date
- end date
- active/inactive
- priority
- products
- categories
- discount rules

---

# 21. MARKETING

The storefront must support:

Meta Pixel

TikTok Pixel

Google Analytics later if required.

Track:

```text
PAGE_VIEW
VIEW_CONTENT
ADD_TO_CART
INITIATE_CHECKOUT
ORDER_CREATED
ORDER_CONFIRMED
ORDER_SHIPPED
ORDER_DELIVERED
ORDER_CANCELLED
```

Capture:

```text
utm_source
utm_medium
utm_campaign
utm_content
utm_term
fbclid
ttclid
referrer
landingPage
```

Marketing attribution must be stored with the order.

Example:

```text
Source: Facebook
Campaign: Summer_2026
Ad: robe_video_01
```

---

# 22. ADMIN DASHBOARD

Admin dashboard should provide:

- Revenue
- Orders
- Orders awaiting confirmation
- Confirmed orders
- Shipped orders
- Delivered orders
- Returns
- Confirmation rate
- Cancellation rate
- Delivery rate
- Top products
- Top wilayas
- Marketing sources

---

# 23. ADMIN ORDER MANAGEMENT

Admin must be able to:

- search orders
- filter orders
- confirm
- cancel
- call customer
- prepare
- create shipment
- choose carrier
- view tracking
- print/download label
- mark returned
- add notes

Order detail must show:

- customer
- phone
- address
- products
- variants
- prices
- discounts
- shipping
- COD total
- call history
- order history
- shipment
- tracking
- marketing attribution

---

# 24. SECURITY

Implement:

- JWT
- Refresh tokens
- Role-based authorization
- Input validation
- Rate limiting
- CORS
- Secure headers
- Audit logs
- Secret management

Never expose:

- JWT secret
- database password
- carrier credentials
- Meta secrets
- TikTok secrets

---

# 25. ROLES

Initial roles:

```text
SUPER_ADMIN
ADMIN
ORDER_MANAGER
CONFIRMATION_AGENT
STOCK_MANAGER
MARKETING_MANAGER
VIEWER
```

Authorization must be enforced in the backend.

Never rely only on frontend role checks.

---

# 26. DATABASE

Use PostgreSQL.

Use EF Core migrations.

Do not use SQLite.

Do not use an in-memory database for the real application.

Create appropriate indexes.

Avoid N+1 queries.

---

# 27. API DESIGN

Use REST APIs.

Keep API contracts explicit.

Use DTOs.

Do not expose EF entities directly.

Validate requests.

Return consistent error responses.

Example:

```json
{
  "success": false,
  "error": {
    "code": "ORDER_NOT_CONFIRMABLE",
    "message": "Cette commande ne peut pas être confirmée."
  }
}
```

---

# 28. FILE STORAGE

Do not store large images directly in PostgreSQL.

Create:

```text
IFileStorageService
```

Allow future providers:

- local storage
- S3
- Cloudinary
- Azure Blob

---

# 29. TESTING

Important workflows must have automated tests.

At minimum:

- authentication
- product
- inventory
- cart
- COD order
- order state transitions
- stock reservation
- stock release
- promotions
- shipping
- tracking
- returns

Especially test:

```text
Create order
→ Reserve stock
→ Cancel
→ Release stock
```

and:

```text
Create order
→ Reserve stock
→ Confirm
→ Prepare
→ Ship
→ Deliver
```

---

# 30. DEVELOPMENT RULE

Before changing code:

1. Read CLAUDE.md.
2. Read PROJECT_CONTEXT.md.
3. Inspect the existing implementation.
4. Understand the current architecture.
5. Do not rewrite working functionality unnecessarily.
6. Make the smallest clean change required.
7. Run relevant tests/build.
8. Update project documentation.

Never assume that a feature does not exist before inspecting the repository.

---

# 31. PROJECT MEMORY — VERY IMPORTANT

The project must maintain a persistent development memory.

The main file is:

```text
PROJECT_CONTEXT.md
```

This file is mandatory.

After EVERY meaningful modification, update PROJECT_CONTEXT.md.

The purpose is:

If the current Claude conversation is lost, a new Claude Code agent must be able to open the repository and immediately understand:

- what the project is
- current architecture
- what has been implemented
- what is currently being developed
- what remains
- important technical decisions
- known issues
- integrations
- database changes
- API changes
- frontend changes
- next recommended steps

Do NOT rely on conversation history.

The repository must contain the project memory.

---

# 32. CHANGELOG

Maintain:

```text
CHANGELOG.md
```

Record meaningful changes.

Format:

```text
## [YYYY-MM-DD]

### Added
- ...

### Changed
- ...

### Fixed
- ...

### Database
- ...

### API
- ...

### Frontend
- ...

### Notes
- ...
```

Do not add meaningless entries for every tiny formatting change.

---

# 33. PROJECT_CONTEXT.md UPDATE FORMAT

Keep PROJECT_CONTEXT.md concise but useful.

It should contain:

```text
# Project Context

## Project
Short description.

## Current Stack
Backend:
Frontend:
Database:
Infrastructure:

## Current Architecture
Short explanation.

## Implemented Features
- ...
- ...

## Current Feature
What is currently being worked on.

## Last Completed Work
What was just completed.

## Database
Important entities and recent migrations.

## API
Important endpoints and integrations.

## Frontend
Important pages/components.

## Shipping
Yalidine:
ZR Express:
Fake provider:

## COD Workflow
Current order workflow.

## Marketing
Current tracking implementation.

## Known Issues
- ...

## Pending Work
- ...

## Important Decisions
- ...

## Next Recommended Steps
1. ...
2. ...
3. ...
```

Always update the relevant section instead of endlessly appending text.

---

# 34. SESSION HANDOFF

At the end of a meaningful development session, update:

```text
PROJECT_CONTEXT.md
```

with:

### What was done

### What works

### What remains

### Known problems

### Exact next step

The next agent should be able to continue without asking:

"What were we doing?"

---

# 35. WHEN USER ASKS FOR A FEATURE

Before implementing:

1. Understand the requirement.
2. Inspect existing code.
3. Check PROJECT_CONTEXT.md.
4. Identify affected modules.
5. Explain the implementation plan briefly.
6. Implement.
7. Test.
8. Update PROJECT_CONTEXT.md.
9. Update CHANGELOG.md if the change is meaningful.

Do not immediately generate random files.

---

# 36. DATABASE CHANGES

Whenever the database model changes:

1. Update entity/model.
2. Create EF Core migration.
3. Verify migration.
4. Update PROJECT_CONTEXT.md.
5. Mention migration name in CHANGELOG.md.

Never modify production database structure manually without a migration.

---

# 37. API CHANGES

Whenever an API endpoint changes:

Update:

- DTO
- validation
- controller
- service
- tests
- Swagger if necessary
- PROJECT_CONTEXT.md

Document important endpoint changes.

---

# 38. FRONTEND CHANGES

Whenever a major frontend feature is added:

Update:

- route
- component
- API integration
- loading state
- error state
- mobile layout
- responsive behavior

Do not implement desktop-only features.

Always verify mobile.

---

# 39. UX RULE

For customer-facing pages:

Prefer:

- fewer fields
- clear CTA
- large product images
- readable typography
- fast checkout
- obvious COD information
- clear delivery information

The main conversion path is:

```text
Advertisement
↓
Product
↓
Acheter maintenant
↓
COD checkout
↓
Commande
```

Minimize friction.

---

# 40. COD BUSINESS RULE

A customer order is NOT considered paid when created.

Payment method:

```text
COD
```

Payment status should reflect reality.

Possible:

```text
PENDING
COLLECTED
FAILED
REFUNDED
```

The order amount is the amount the carrier should collect.

---

# 41. BUSINESS DATA INTEGRITY

Never trust the frontend for:

- prices
- discounts
- stock
- shipping price
- order total
- order status
- permissions

The backend recalculates and validates all important business data.

---

# 42. NO FAKE SUCCESS

Do not create fake successful API calls for features that are supposed to work.

If an external integration is unavailable:

Use:

```text
FakeShippingProvider
```

or a clearly marked development implementation.

Never make the UI pretend that Yalidine or ZR Express successfully created a real shipment when it did not.

---

# 43. ERROR HANDLING

Every external integration must handle:

- timeout
- authentication failure
- invalid request
- provider error
- network failure
- unknown response

Do not crash the application.

Log technical details server-side.

Show friendly French messages to users.

---

# 44. CODE QUALITY

Prefer:

- readable code
- small services
- clear names
- typed DTOs
- dependency injection
- interfaces around external services
- testable business logic

Avoid:

- huge controllers
- duplicated business logic
- magic strings
- hardcoded business rules
- secrets in source code
- unnecessary abstractions
- unnecessary microservices

---

# 45. LANGUAGE

Customer-facing UI:

French.

Technical code:

English naming conventions.

Database:

English entity/property names.

Business terminology may use French in UI only.

Example:

Code:

```text
PendingConfirmation
```

UI:

```text
En attente de confirmation
```

---

# 46. DOCKER

The project must be runnable through Docker Compose.

Development services:

```text
postgres
backend
frontend
```

Use:

```text
.env.example
```

Never commit real credentials.

---

# 47. GIT

Keep commits focused.

Recommended format:

```text
feat: add COD checkout
feat: add Yalidine shipping adapter
feat: add order confirmation workflow
fix: release reserved stock on cancellation
refactor: extract shipping provider abstraction
docs: update project context
```

Do not mix unrelated features in one commit.

---

# 48. IMPORTANT WORKING PRINCIPLE

Do not destroy existing working functionality to implement a new feature.

Before modifying a module:

Understand it.

If a better architecture is needed:

Refactor incrementally.

Preserve working behavior unless the requirement explicitly changes it.

---

# 49. STARTING A NEW SESSION

At the beginning of every new Claude Code session:

1. Read CLAUDE.md.
2. Read PROJECT_CONTEXT.md.
3. Read the latest relevant CHANGELOG.md entries.
4. Inspect git status.
5. Inspect recent commits if necessary.
6. Understand current implementation.
7. Then proceed with the user's request.

Never assume the previous conversation is available.

---

# 50. FINAL RULE

The repository itself is the source of project memory.

The conversation is temporary.

Code + migrations + documentation + PROJECT_CONTEXT.md must contain enough information for a completely new developer or Claude Code agent to continue the project.

Whenever you finish meaningful work, leave the repository in a state where another agent can continue immediately.