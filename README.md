# Marketplace API

[![CI](https://github.com/runtimefox/Marketplace.api/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/runtimefox/Marketplace.api/actions/workflows/ci.yml)

Backend for a multi-seller marketplace built with ASP.NET Core, GraphQL and PostgreSQL.
Buyers browse the catalog, fill a cart, place orders and leave reviews; sellers run shops with their staff, manage products and ship orders.

## Features

- **Accounts** — registration, JWT in HttpOnly cookies, refresh token rotation with reuse detection, profile and password change, `Customer` / `Admin` roles
- **Shops** — sign up as a seller, shop owner and managers, seller cabinet with all shop products and orders
- **Catalog** — products and categories with paging, filtering and sorting; full-text product search by name and description
- **Cart and orders** — checkout with a delivery address creates one order per seller, `CREATED → SHIPPED → DELIVERED` status flow, cancellation returns stock, optimistic concurrency against overselling
- **Reviews** — only buyers with a delivered order can review; product and seller ratings are recalculated automatically
- **Images** — product galleries (up to 10 photos), shop logos and user avatars; uploads are validated, converted to WebP in two sizes and stored in S3-compatible storage

## Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| API | GraphQL (HotChocolate 16), REST controllers for authentication |
| Data | PostgreSQL 17, Entity Framework Core 10 (Npgsql) |
| Auth | JWT bearer tokens stored in HttpOnly cookies |
| Files | MinIO (S3-compatible) via AWSSDK.S3, SkiaSharp for image processing |
| Tests | xUnit, `WebApplicationFactory`, Testcontainers |
| CI | GitHub Actions |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)
- EF Core CLI: `dotnet tool install --global dotnet-ef --version 10.0.12`

### 1. Start PostgreSQL and MinIO

Create a `.env` file in the repository root:

```env
POSTGRES_DB=myapi
POSTGRES_USER=postgres
POSTGRES_PASSWORD=change-me
POSTGRES_PORT=5432
```

```bash
docker compose up -d
```

This starts PostgreSQL and MinIO; the one-off `minio-init` container creates the public `myapi-images` bucket. The MinIO console is at http://localhost:9001 (`minioadmin` / `minioadmin` unless `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` are set in `.env`). `appsettings.Development.json` already points the API at this MinIO.

### 2. Configure secrets

Secrets are kept in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), not in `appsettings.json`:

```bash
dotnet user-secrets set "ConnectionStrings:DbConnection" "Host=localhost;Port=5432;Database=myapi;Username=postgres;Password=change-me"
dotnet user-secrets set "Jwt:Key" "<random string, at least 32 characters>"
```

The app does not start without `Jwt:Key`.

### 3. Apply migrations and run

```bash
dotnet ef database update
dotnet run --launch-profile http
```

| URL | What |
|---|---|
| http://localhost:5086/graphql | GraphQL endpoint and Nitro IDE (schema, autocomplete) |
| http://localhost:5086/swagger | REST endpoints (Development only) |

### Run with Docker

The `Dockerfile` builds two images: `api` (the application) and `migrations` (an EF Core migrations bundle). In `docker-compose.yml` both are behind the `app` profile, so `docker compose up -d` still starts only PostgreSQL.

Add to `.env`:

```env
JWT_KEY=<random string, at least 32 characters>
API_PORT=8080
FRONTEND_ORIGIN=http://localhost:5173
```

```bash
docker compose --profile app up --build
```

Compose waits for PostgreSQL and MinIO, applies migrations in the `migrations` container and then starts the API at http://localhost:8080/graphql. Containers run in the Production environment: no demo data and no Swagger.

To apply migrations to another database with the image:

```bash
docker build --target migrations --tag myapi-migrations .
docker run --rm myapi-migrations --connection "Host=...;Port=5432;Database=...;Username=...;Password=..."
```

### Demo data

In the Development environment the database is seeded on startup. All demo accounts use the password `Password123`. Demo products have USD prices and generated photos, shops get logos and demo users get avatars; images require MinIO to be running (otherwise the API starts and logs a warning, and the images are created on the next start).

| Email | Role |
|---|---|
| `admin@myapi.dev` | Platform admin |
| `techstore@myapi.dev` | Owner of the TechStore shop |
| `homegoods@myapi.dev` | Owner of the Home Goods shop |
| `buyer@myapi.dev` | Buyer |

## API

### Authentication (REST)

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a buyer |
| POST | `/api/auth/register-seller` | Register an account together with a shop |
| POST | `/api/auth/login` | Sign in, sets `access_token` and `refresh_token` cookies |
| POST | `/api/auth/refresh` | Rotate tokens |
| POST | `/api/auth/revoke` | Sign out |
| GET / PUT | `/api/auth/me` | Current user profile |
| PUT | `/api/auth/me/password` | Change password, signs out other sessions |
| GET | `/api/user`, `/api/user/{id}` | Users (admin only) |
| PUT / DELETE | `/api/auth/me/avatar` | Upload or remove the current user's avatar |
| POST | `/api/products/{productId}/images` | Upload a product photo (shop members) |
| PUT | `/api/products/{productId}/images/order` | Reorder photos: `{ "imageIds": [...] }`, the first one is the main image |
| DELETE | `/api/products/{productId}/images/{imageId}` | Remove a product photo |
| PUT / DELETE | `/api/sellers/{sellerId}/logo` | Upload or remove a shop logo (shop owner) |

Tokens are returned only in cookies. Sign in once through Swagger or `MyApi.http`, and the cookies are sent with the following REST and GraphQL requests.

Registration, sign-in and password change are limited to 10 requests per minute per IP (`RateLimiting:Auth`); extra requests get `429 Too Many Requests` with a `Retry-After` header.

### Frontend

- Browser origins allowed by CORS are set in `Frontend:AllowedOrigins` (Development: `http://localhost:3000`, `http://localhost:5173`). Send requests with credentials, e.g. `fetch(url, { credentials: "include" })`, so the auth cookies are included.
- Behind a reverse proxy (Next.js rewrites, nginx, a load balancer) the API reads the client IP and scheme from `X-Forwarded-For` / `X-Forwarded-Proto`, but only from trusted proxies: localhost is trusted by default, others are listed in `TrustedProxies:Proxies` (IP addresses) or `TrustedProxies:Networks` (CIDR, e.g. `172.18.0.0/16` for a Docker network). Without it, rate limiting would treat all users as one client.
- The GraphQL schema is committed as [`schema.graphql`](schema.graphql) for code generation (for example GraphQL Code Generator). A test fails when the file is outdated; regenerate it with `UPDATE_SCHEMA=1 dotnet test tests/MyApi.Tests --filter SchemaSnapshotTests`.

### Images

- Files are uploaded through REST as `multipart/form-data` with a single `file` field (JPEG, PNG or WebP, up to 10 MB).
- Every upload is checked by its real content, rotated according to EXIF, stripped of metadata and stored as WebP in two sizes: products 320 / 1280 px, shop logos 128 / 512 px, avatars 96 / 512 px (the longer side, never upscaled).
- GraphQL exposes URLs only: `images { id position smallUrl largeUrl }` and `mainImage` on products, `logo` on shops, `avatar` on users and review authors.
- A product has up to 10 photos. Replaced and deleted images are removed from storage.

### GraphQL

| Area | Queries | Mutations |
|---|---|---|
| Catalog | `products`, `productById`, `categories`, `categoryById` | `createCategory`, `updateCategory`, `deleteCategory` (admin) |
| Shops | `sellers`, `sellerById`, `mySellers`, `sellerMembers`, `sellerProducts` | `createSeller`, `updateSeller`, `deleteSeller`, `addSellerManager`, `removeSellerMember`, `createProduct`, `updateProduct`, `activateProduct`, `deleteProduct` |
| Cart and orders | `myCart`, `myOrders`, `sellerOrders`, `orderById` | `addToCart`, `updateCartItem`, `removeFromCart`, `clearCart`, `checkout`, `updateOrderDeliveryAddress`, `shipOrder`, `confirmOrderDelivery`, `cancelOrder` |
| Reviews | `productReviews`, `myReviews`, `canReviewProduct` | `createReview`, `updateReview`, `deleteReview` |
| Users | `me`, `users` (admin) | — |

```graphql
{
  products(first: 5, order: { price: DESC }, where: { rating: { gte: 4 } }) {
    totalCount
    nodes {
      name
      price
      rating
      reviewCount
      category { name }
      seller { name rating }
    }
  }
}
```

Product search is available on `products` and `sellerProducts` through the `search` argument. It uses PostgreSQL full-text search over the name and description: every word matches by prefix and word forms (`wirel head` finds "Wireless Headphones"), an exact SKU also matches, and results are ordered by relevance unless `order` is given. Search can be combined with `where` and paging.

```graphql
{
  products(search: "wireless headphones", first: 10, where: { price: { lte: 100 } }) {
    totalCount
    nodes { name price }
  }
}
```

Errors from business rules come back with `extensions.code`: `INVALID_INPUT`, `FORBIDDEN`, `AUTH_NOT_AUTHENTICATED` or `AUTH_NOT_AUTHORIZED`.

Ready-to-use REST and GraphQL requests are in [`MyApi.http`](MyApi.http).

## Tests

```bash
dotnet test tests/MyApi.Tests
dotnet test tests/MyApi.Tests --filter "FullyQualifiedName~MyApi.Tests.Unit"
```

- **Integration tests** start the whole application against a PostgreSQL container (Testcontainers) with real migrations and cover auth, access rules, cart, orders, stock concurrency and reviews. Docker must be running.
- **Unit tests** cover entity invariants and do not need Docker.

## Project structure

```
Controllers/        REST endpoints (auth, users)
GraphQL/            GraphQL queries and mutations grouped by feature
Models/Entities/    Domain entities with their invariants
Models/Dtos/        Request and response DTOs grouped by feature
Services/           Business logic behind interfaces in Services/Interfaces
Shared/             DbContext, seed data, auth helpers, configuration
Migrations/         EF Core migrations
tests/MyApi.Tests/  Integration and unit tests
```

## Development workflow

- Create feature branches from `dev` and open pull requests into `dev`; `dev` is released to `main` through a pull request.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/): `feat`, `fix`, `refactor`, `test`, `ci`, `chore`, `docs`.
- CI builds the project, fails when a model change has no migration and runs all tests. Pull requests into `dev` and `main` can be merged only when CI passes.
