# Marketplace API

[![CI](https://github.com/runtimefox/Marketplace.api/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/runtimefox/Marketplace.api/actions/workflows/ci.yml)

Backend for a multi-seller marketplace built with ASP.NET Core, GraphQL and PostgreSQL.
Buyers browse the catalog, fill a cart, place orders and leave reviews; sellers run shops with their staff, manage products and ship orders.

## Features

- **Accounts** — registration, JWT in HttpOnly cookies, refresh token rotation with reuse detection, profile and password change, `Customer` / `Admin` roles
- **Shops** — sign up as a seller, shop owner and managers, seller cabinet with all shop products and orders
- **Catalog** — products and categories with paging, filtering and sorting
- **Cart and orders** — checkout creates one order per seller, `CREATED → SHIPPED → DELIVERED` status flow, cancellation returns stock, optimistic concurrency against overselling
- **Reviews** — only buyers with a delivered order can review; product and seller ratings are recalculated automatically

## Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| API | GraphQL (HotChocolate 16), REST controllers for authentication |
| Data | PostgreSQL 17, Entity Framework Core 10 (Npgsql) |
| Auth | JWT bearer tokens stored in HttpOnly cookies |
| Tests | xUnit, `WebApplicationFactory`, Testcontainers |
| CI | GitHub Actions |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)
- EF Core CLI: `dotnet tool install --global dotnet-ef --version 10.0.12`

### 1. Start PostgreSQL

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

Compose waits for PostgreSQL, applies migrations in the `migrations` container and then starts the API at http://localhost:8080/graphql. Containers run in the Production environment: no demo data and no Swagger.

To apply migrations to another database with the image:

```bash
docker build --target migrations --tag myapi-migrations .
docker run --rm myapi-migrations --connection "Host=...;Port=5432;Database=...;Username=...;Password=..."
```

### Demo data

In the Development environment the database is seeded on startup. All demo accounts use the password `Password123`.

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

Tokens are returned only in cookies. Sign in once through Swagger or `MyApi.http`, and the cookies are sent with the following REST and GraphQL requests.

Registration, sign-in and password change are limited to 10 requests per minute per IP (`RateLimiting:Auth`); extra requests get `429 Too Many Requests` with a `Retry-After` header.

### Frontend

- Browser origins allowed by CORS are set in `Frontend:AllowedOrigins` (Development: `http://localhost:3000`, `http://localhost:5173`). Send requests with credentials, e.g. `fetch(url, { credentials: "include" })`, so the auth cookies are included.
- The GraphQL schema is committed as [`schema.graphql`](schema.graphql) for code generation (for example GraphQL Code Generator). A test fails when the file is outdated; regenerate it with `UPDATE_SCHEMA=1 dotnet test tests/MyApi.Tests --filter SchemaSnapshotTests`.

### GraphQL

| Area | Queries | Mutations |
|---|---|---|
| Catalog | `products`, `productById`, `categories`, `categoryById` | `createCategory`, `updateCategory`, `deleteCategory` (admin) |
| Shops | `sellers`, `sellerById`, `mySellers`, `sellerMembers`, `sellerProducts` | `createSeller`, `updateSeller`, `deleteSeller`, `addSellerManager`, `removeSellerMember`, `createProduct`, `updateProduct`, `activateProduct`, `deleteProduct` |
| Cart and orders | `myCart`, `myOrders`, `sellerOrders`, `orderById` | `addToCart`, `updateCartItem`, `removeFromCart`, `clearCart`, `checkout`, `shipOrder`, `confirmOrderDelivery`, `cancelOrder` |
| Reviews | `productReviews`, `myReviews`, `canReviewProduct` | `createReview`, `updateReview`, `deleteReview` |
| Users | `users` (admin) | — |

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
