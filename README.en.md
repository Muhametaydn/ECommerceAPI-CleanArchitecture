<div align="center">

[![Dil: Türkçe](https://img.shields.io/badge/Dil-Türkçe-red?style=for-the-badge)](README.md)
[![Language: English](https://img.shields.io/badge/Language-English-blue?style=for-the-badge)](README.en.md)

# 🛒 ECommerce API

A production-ready e-commerce RESTful API built with **.NET 8**, **Clean Architecture**, and **CQRS** patterns.

[![CI/CD](https://github.com/Muhametaydn/ECommerceAPI/actions/workflows/ci.yml/badge.svg)](https://github.com/Muhametaydn/ECommerceAPI/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![Elasticsearch](https://img.shields.io/badge/Elasticsearch-8.13-005571?logo=elasticsearch)](https://www.elastic.co/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

</div>

---

## 📖 About the Project

This project is a comprehensive e-commerce backend API that brings together modern software-development practices. It is designed to cover real-world scenarios and includes:

- **Clean, maintainable code** — each layer has a single responsibility through Clean Architecture
- **Scalable architecture** — reads and writes are independent with CQRS
- **Reliable messaging** — events are never lost with the Outbox Pattern
- **High performance** — optimized with Redis cache, ETags, and Elasticsearch
- **Production readiness** — ready to deploy with Docker, CI/CD, and Serilog logging

---

## 🏗️ Architecture

```mermaid
graph TB
    Client(["🌐 Client"])

    subgraph API ["ECommerce.API"]
        Controllers["Controllers\nAuth · Product · Order · Cart\nCategory · Coupon · Payment · Review · Address"]
        Middleware["Middleware\nGlobalException · ETag · Serilog Request Logging\nRate Limiting (Built-in Policy)"]
    end

    subgraph Application ["ECommerce.Application"]
        CQRS["CQRS — MediatR\nCommands & Queries"]
        Behaviors["Pipeline Behaviors\nValidation · Caching"]
        Validators["FluentValidation"]
    end

    subgraph Infrastructure ["ECommerce.Infrastructure"]
        Auth["JWT Auth\nRefresh Token Rotation"]
        Cache["Redis\nCart + Response Cache"]
        Search["Elasticsearch\nFuzzy Search"]
        MQ["RabbitMQ + MassTransit\nOrderCreated · PaymentProcessed\nLowStockAlert · OrderStatusChanged"]
        Jobs["Hangfire\nOutbox Processor"]
        Email["FluentEmail + MailKit\nSMTP Notifications (local: MailHog)"]
    end

    subgraph Persistence ["ECommerce.Persistence"]
        Repo["Generic Repository\n+ Unit of Work"]
        Spec["Specification Pattern"]
        Outbox[("Outbox Table\nOutboxRepository")]
        DB[("PostgreSQL 16\nEF Core 8")]
    end

    subgraph Domain ["ECommerce.Domain"]
        Entities["Rich Domain Model\nProduct · Order · Cart · Payment\nCategory · Coupon · Review"]
        Events["Domain Events\nIDomainEvent"]
    end

    Client --> Controllers
    Controllers --> Middleware
    Controllers --> CQRS
    CQRS --> Behaviors
    Behaviors --> Validators
    CQRS --> Auth
    CQRS --> Cache
    CQRS --> Search
    CQRS --> Repo
    Repo --> Spec
    Repo --> DB
    CQRS --> Events
    Events --> Outbox
    Outbox --> Jobs
    Jobs --> MQ
    MQ --> Email
    Domain --> Entities
    Domain --> Events
```

---

## 🛠️ Tech Stack

| Category | Technology | Why? |
|---|---|---|
| Framework | .NET 8, ASP.NET Core Web API | LTS release, high performance |
| ORM | Entity Framework Core 8 | Code-first migrations, LINQ support |
| Database | PostgreSQL 16 | Reliable, ACID-compliant relational DB |
| Cache | Redis 7 | Low latency, cart + response cache |
| Search | Elasticsearch 8.13 | Fuzzy search, language analysis |
| Messaging | RabbitMQ 3.13 + MassTransit 8 | Reliable asynchronous event handling |
| Background Jobs | Hangfire | Outbox-job scheduling and retries |
| Auth | ASP.NET Identity + JWT | Industry standard, token rotation |
| Email | FluentEmail + MailKit | HTML-template support |
| Logging | Serilog | Structured logging, sink support |
| API Docs | Swagger / OpenAPI | Automatic documentation |
| Testing | xUnit, Moq, FluentAssertions, NetArchTest | Unit, integration, and architecture tests |
| DevOps | Docker, GitHub Actions | Containers and CI/CD automation |

---

## ✨ Features

### 🏛️ Clean Architecture
The project is split into four layers: **Domain → Application → Infrastructure/Persistence → API**. Each layer depends only on the layer beneath it. This keeps infrastructure changes from affecting the domain and makes testing easier.

### ⚡ CQRS + MediatR
Read (Query) and write (Command) operations are managed in fully separate handlers. MediatR pipeline behaviors automatically apply validation, caching, and logging to every request.

### 📨 Domain Events + Outbox Pattern
A domain event is raised when an order is created. The Outbox Pattern saves the event to the database; Hangfire processes it every minute and sends it to RabbitMQ. This ensures **no event is lost** — even a service crash does not compromise reliability.

### 🔐 JWT + Refresh Token Rotation
Access tokens are valid for 15 minutes and refresh tokens for 7 days. Every refresh revokes the old token and issues a new one (rotation), so even a stolen token becomes invalid quickly.

### 🚀 Redis Cache + ETag
Frequently used queries, such as the product list and category tree, are cached in Redis. When a client requests unchanged data again, the ETag middleware returns `304 Not Modified`, saving bandwidth.

### 🔍 Elasticsearch Fuzzy Search
Product searches run through Elasticsearch. Typographical errors are tolerated through fuzzy matching, and results prioritize product names.

### 📊 Policy-Based Authorization
The `Admin`, `Seller`, and `Customer` roles are defined along with the `AdminOnly`, `SellerOrAdmin`, `CustomerOnly`, and `Authenticated` policies. Every endpoint clearly specifies the roles that can access it.

### 🛡️ Rate Limiting
Auth endpoints are limited to 5 requests/minute, API endpoints to 30 requests/minute, and the global limit is 60 requests/minute per IP address. This provides DDoS and brute-force protection.

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker & Docker Compose](https://www.docker.com/get-started)

### 1. Clone the repository

```bash
git clone https://github.com/Muhametaydn/ECommerceAPI-CleanArchitecture.git
cd ECommerceAPI-CleanArchitecture
```

### 2. Start infrastructure services

```bash
docker compose up -d postgres redis rabbitmq mailhog elasticsearch
```

### 3. Set local secrets (once)

Passwords and the JWT key are never committed; .NET user-secrets keeps them on your machine only.
The values match the local services started with `docker compose`.

```bash
cd src/ECommerce.API
dotnet user-secrets set "JwtSettings:SecretKey" "$(openssl rand -hex 32)"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ECommerceDB;Username=postgres;Password=root"
dotnet user-secrets set "RabbitMQ:Password" "guest"
```

> The API **refuses to start** without a JWT key (empty, shorter than 32 bytes or placeholder keys are rejected).

### 4. Run the API

```bash
dotnet run
```

Swagger UI: **http://localhost:5000/swagger**

### 5. Start the full stack with Docker (optional)

```bash
docker compose up -d
```

API: **http://localhost:8080** | Swagger: **http://localhost:8080/swagger**

---

## ⚙️ Environment Variables

| Variable | Description | Default |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;...` |
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379` |
| `JwtSettings__SecretKey` | JWT signing key (≥32 random bytes). `JWT_SECRET_KEY` in compose | — (required) |
| `ReverseProxy__KnownProxies__0` / `ReverseProxy__KnownNetworks__0` | Proxy IP / network (CIDR) whose `X-Forwarded-For` is trusted. Set it to the frontend server | loopback only |
| `FraudDetection__Enabled` | Fraud service integration (`ECommerceFraud` repo). While off, the order flow is unchanged | `false` |
| `FRAUD_DETECTION_ENABLED` / `FRAUD_DETECTION_MODE` | Docker Compose environment variables for the two settings above. See the `ECommerceFraud` README for the full system | `false` / `Monitor` |
| `FraudDetection__Mode` | `Monitor`: decisions are recorded, orders are never held. `Enforce`: orders under review cannot be confirmed; an analyst rejection cancels the order | `Monitor` |
| `RabbitMQ__Host` | RabbitMQ host | `localhost` |
| `Elasticsearch__Uri` | Elasticsearch URI | `http://localhost:9200` |
| `Email__Host` | SMTP host | `localhost` |

> ⚠️ The API **refuses to start** if the key is empty, shorter than 32 bytes, or a placeholder. Generate one with `openssl rand -hex 32`.
> Production compose: `JWT_SECRET_KEY=... docker compose -f docker-compose.yml up -d`
>
> Rate limits are per client (login: 5/min per IP; API: 30/min per user or IP). If the frontend proxy is not in a trusted network, every user counts as one client.

---

## 📡 API Endpoints

### Auth — `/api/v1/auth`

| Method | Path | Description |
|---|---|---|
| `POST` | `/register` | Register a new user |
| `POST` | `/login` | Sign in → JWT + refresh token |
| `POST` | `/refresh-token` | Refresh the access token |
| `POST` | `/revoke-token` | Revoke a refresh token |

### Products — `/api/v1/products`

| Method | Path | Authorization | Description |
|---|---|---|---|
| `GET` | `/` | — | Product list (paginated and filterable) |
| `GET` | `/{id}` | — | Product detail |
| `GET` | `/search?q=...` | — | Elasticsearch fuzzy search |
| `POST` | `/` | SellerOrAdmin | Create a product |
| `PUT` | `/{id}` | SellerOrAdmin | Update a product |
| `DELETE` | `/{id}` | Admin | Delete a product |

### Orders — `/api/v1/orders`

| Method | Path | Authorization | Description |
|---|---|---|---|
| `GET` | `/` | Authenticated | My orders |
| `GET` | `/{id}` | Authenticated | Order detail |
| `POST` | `/` | Customer | Create an order |
| `PUT` | `/{id}/confirm` | SellerOrAdmin | Confirm the order |
| `PUT` | `/{id}/ship` | SellerOrAdmin | Mark as shipped |
| `PUT` | `/{id}/deliver` | SellerOrAdmin | Mark as delivered |
| `PUT` | `/{id}/cancel` | Authenticated | Cancel the order |

### Other Endpoints

| Prefix | Description |
|---|---|
| `/api/v1/cart` | Cart operations (Redis-backed) |
| `/api/v1/categories` | Hierarchical category management |
| `/api/v1/coupons` | Create and apply coupons |
| `/api/v1/addresses` | User address management |
| `/api/v1/reviews` | Product reviews |
| `/api/v1/payments` | Payment operations |

---

## 🧪 Tests

```bash
# Unit tests
dotnet test tests/ECommerce.UnitTests

# Integration tests
dotnet test tests/ECommerce.IntegrationTests

# Architecture tests (layer dependencies)
dotnet test tests/ECommerce.ArchitectureTests

# All tests
dotnet test ECommerceAPI.sln
```

---

## 🛠️ Development Tools

| Service | URL | Details |
|---|---|---|
| Swagger UI | http://localhost:5000/swagger | API documentation |
| Hangfire Dashboard | http://localhost:5000/hangfire | Background-job management (dev) |
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| MailHog | http://localhost:8025 | Test email inbox |
| Kibana | http://localhost:5601 | Elasticsearch visualization |

---

## 📁 Project Structure

```
ECommerceAPI/
├── .github/
│   └── workflows/
│       └── ci.yml                 # GitHub Actions — Build · Test · Docker Push
├── src/
│   ├── ECommerce.Domain           # Entities, enums, interfaces, specifications, domain events
│   ├── ECommerce.Application      # Features (CQRS), DTOs, validators, behaviors, mapping
│   ├── ECommerce.Infrastructure   # JWT, Redis, Elasticsearch, RabbitMQ, email, Hangfire jobs
│   ├── ECommerce.Persistence      # EF Core DbContext, repositories, UoW, migrations
│   └── ECommerce.API              # Controllers, middleware, Program.cs
├── tests/
│   ├── ECommerce.UnitTests        # Domain + Application unit tests
│   ├── ECommerce.IntegrationTests # Integration tests with a real DB
│   └── ECommerce.ArchitectureTests # Layer-dependency rules (NetArchTest)
├── docker-compose.yml             # All services (prod)
├── docker-compose.override.yml    # Development-environment overrides
├── Dockerfile                     # Multi-stage build
└── ECommerceAPI.sln
```

---

## 📄 License

MIT — see the [LICENSE](LICENSE) file for details.

---

## 🔒 Security & Quality Audit (November 2026)

The backend (this repo) and the frontend (`ECommerceFrontend`) were audited end to end. For every vulnerability and bug, a test that catches it was written first, then the fix.

### Test results

| | Before | After |
|---|---|---|
| Backend tests | 330 | **405** (Unit 300, Integration 101, Architecture 4), all passing |
| Frontend unit tests | 104 | **113**, all passing |
| Frontend E2E (Playwright) | — | **28/28** passing (storefront, admin, seller) |
| Coverage: Application | 61% | **72%** |
| Coverage: Domain | 83% | **88%** |
| Dependency advisories (NuGet / npm) | 2 high / 9 (1 critical) | **0 / 1** (see below) |

New test groups: rate limiting and proxy trust, login lockout, refresh-token reuse, error-response leakage, order/coupon/payment/stock lifecycle, cancellation and refund, product deletion, review and address ownership (IDOR), coupon validation, cart updates, the Outbox job, open redirect.

### Vulnerabilities fixed

| # | Issue | Impact | Fix |
|---|---|---|---|
| 1 | Rate limits were one global counter | 5 failed logins from anyone blocked login for **every user** | Per-client limits (IP / user). `X-Forwarded-For` is trusted only from known proxies; the frontend forwards it |
| 2 | Public JWT key in the repo | Anyone could forge **Admin** tokens in production | The API refuses to start with an empty, short or placeholder key |
| 3 | Demo admin with a known password in production | Admin login with `Admin123!` | Demo accounts are seeded only in Development |
| 4 | No brute-force protection | Unlimited password guessing | 15-minute lockout after 5 failed attempts |
| 5 | Refresh-token theft went unnoticed | A stolen token could be used silently | Reusing a rotated token revokes all sessions (30 s grace for multiple tabs) |
| 6 | `revoke-token` had no ownership check | Other users' sessions could be revoked | Only the caller's own tokens can be revoked |
| 7 | Internal details in error responses | EF/LINQ internals and file paths reached clients | Only application-code messages are shown; others return a generic 500 |
| 8 | Frontend open redirect | `/giris?redirect=/\evil.com` sent users to an attacker site after login | Redirects are resolved against a fixed origin and validated |
| 9 | Next.js critical RCE and other advisories | Remote code execution via the Image Optimizer | next 15.5.27 → 15.5.29, follow-redirects → 1.16.3, AutoMapper 16.1.1 → 16.3.0 |
| 10 | Login route exposed backend errors | Internal addresses and connection details leaked | Generic message to the browser; details go to the server log |

### Logic bugs fixed

- **Deleting a product deleted every historical order line containing it** (FK `Cascade`). The FK is now `Restrict` (migration `RestrictOrderItemProductDelete`); ordered products are taken off sale instead of deleted.
- Cancelled orders did not give back their coupon use.
- Percentage discounts were not rounded to cents (amounts like 33.3333 TL).
- Refunds set the status directly, so no refund email was sent. They now go through `Order.MarkAsRefunded()`, which raises an event.
- Shipped orders could be cancelled at the domain level; completed payments could be marked failed.
- Cart merge dropped product images; the quantity of an off-sale product could still be raised.
- The Outbox stored a meaningless reflection message instead of the broker's real error.
- The `NotFoundException` message was malformed.
- After `DbSet<User>` was removed, EF planned to rename the `Users` table; the table name is now pinned.

### Known and accepted remaining risks

- **Zod 3 (frontend):** a memory-exhaustion advisory for oversized arrays; the fix requires Zod 4. The forms validate no arrays, so there is no attack path. The major upgrade will be planned separately.
- **Payments are simulated:** confirming an order marks its payment completed. This stays until a real payment provider is integrated.
- Elasticsearch, email and RabbitMQ consumer classes have no tests yet; they need integration tests against the real services.

### Running the tests

```bash
# Backend (separate, throwaway services for the PostgreSQL/Redis tests)
ECOMMERCE_TEST_POSTGRES="Host=localhost;Port=55432;Username=postgres;Password=..." \
ECOMMERCE_TEST_REDIS="localhost:56379" \
dotnet test ECommerceAPI.sln --settings coverage.runsettings

# Frontend (in the ECommerceFrontend folder)
pnpm test && pnpm typecheck && pnpm lint
pnpm build && pnpm test:e2e   # without Playwright's browser: E2E_CHROME_PATH=<path to chrome.exe>
```
