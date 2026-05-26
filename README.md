# ECommerce API

.NET 8 ile geliştirilmiş, **Clean Architecture** ve **CQRS** desenlerini kullanan production-ready E-Ticaret RESTful API.

![CI/CD](https://github.com/YOUR_USERNAME/ECommerceAPI/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

---

## Mimari

```
┌─────────────────────────────────────────────────────────────┐
│                        API (HTTP)                           │
│          Controllers · Middleware · Swagger                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                     Application                             │
│     CQRS (MediatR) · Validators · Behaviors · DTOs         │
└──────────┬────────────────────────────────────┬─────────────┘
           │                                    │
┌──────────▼──────────┐            ┌────────────▼────────────┐
│    Infrastructure   │            │      Persistence        │
│  JWT · Redis · ES   │            │  EF Core · PostgreSQL   │
│  RabbitMQ · Email   │            │  Repositories · UoW     │
└─────────────────────┘            └─────────────────────────┘
           │                                    │
┌──────────▼────────────────────────────────────▼────────────┐
│                        Domain                               │
│         Entities · Events · Specifications · Enums         │
└─────────────────────────────────────────────────────────────┘
```

## Tech Stack

| Katman | Teknoloji |
|---|---|
| Framework | .NET 8, ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Veritabanı | PostgreSQL 16 |
| Cache | Redis 7 (Cart + Response Cache) |
| Arama | Elasticsearch 8.13 |
| Mesajlaşma | RabbitMQ 3.13 + MassTransit 8 |
| Background Jobs | Hangfire (PostgreSQL storage) |
| Auth | ASP.NET Identity + JWT Bearer + Token Rotation |
| Email | FluentEmail + MailKit SMTP |
| Loglama | Serilog (Console + File) |
| Dokümantasyon | Swagger / OpenAPI |
| Test | xUnit, Moq, FluentAssertions, NetArchTest |

## Özellikler

- **Clean Architecture** — Domain, Application, Infrastructure, Persistence, API katmanları
- **CQRS + MediatR** — Command/Query ayrımı, Pipeline Behaviors (Validation, Caching, Logging)
- **Domain Events + Outbox Pattern** — Garantili integration event teslimatı
- **JWT Authentication** — Access (15dk) + Refresh token rotation (7gün)
- **Policy-Based Authorization** — Admin, Seller, Customer rolleri
- **Redis Cache** — Cache-Aside pattern, prefix-tabanlı invalidation
- **ETag / HTTP Cache Validation** — 304 Not Modified desteği
- **Elasticsearch** — Fuzzy multi-match ürün arama
- **Rate Limiting** — Auth (5/dk), API (30/dk), Global (60/dk)
- **RFC 7807 Problem Details** — Standart hata yanıtları
- **Specification Pattern** — Karmaşık sorgu filtreleme
- **Rich Domain Model** — İş mantığı entity içinde

## Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker & Docker Compose](https://www.docker.com/get-started)

## Hızlı Başlangıç

### 1. Repoyu klonla

```bash
git clone https://github.com/YOUR_USERNAME/ECommerceAPI.git
cd ECommerceAPI
```

### 2. Altyapı servislerini başlat

```bash
docker compose up -d postgres redis rabbitmq mailhog elasticsearch
```

### 3. API'yi çalıştır

```bash
cd src/ECommerce.API
dotnet run
```

API `http://localhost:5000` adresinde çalışır. Swagger UI: `http://localhost:5000/swagger`

### 4. Tüm stack'i Docker ile başlat (opsiyonel)

```bash
docker compose up -d
```

API `http://localhost:8080` adresinde çalışır.

## Ortam Değişkenleri

`appsettings.json` üzerinden veya environment variable olarak yapılandırılabilir:

| Değişken | Açıklama | Varsayılan |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL bağlantı dizesi | `Host=localhost;...` |
| `ConnectionStrings__Redis` | Redis bağlantı dizesi | `localhost:6379` |
| `JwtSettings__SecretKey` | JWT imzalama anahtarı (≥32 karakter) | — |
| `RabbitMQ__Host` | RabbitMQ host | `localhost` |
| `Elasticsearch__Uri` | Elasticsearch URI | `http://localhost:9200` |
| `Email__Host` | SMTP host | `localhost` |

> ⚠️ Production'da `JwtSettings__SecretKey` mutlaka değiştirilmelidir.

## API Endpoints

### Auth — `/api/v1/auth`

| Method | Path | Açıklama |
|---|---|---|
| POST | `/register` | Yeni kullanıcı kaydı |
| POST | `/login` | Giriş, JWT + Refresh token döner |
| POST | `/refresh-token` | Access token yenileme |
| POST | `/revoke-token` | Refresh token iptal |

### Products — `/api/v1/products`

| Method | Path | Yetki | Açıklama |
|---|---|---|---|
| GET | `/` | — | Ürün listesi (sayfalı, filtrelenebilir) |
| GET | `/{id}` | — | Ürün detayı |
| GET | `/search?q=...` | — | Elasticsearch ile arama |
| POST | `/` | SellerOrAdmin | Ürün oluştur |
| PUT | `/{id}` | SellerOrAdmin | Ürün güncelle |
| DELETE | `/{id}` | Admin | Ürün sil |

### Orders — `/api/v1/orders`

| Method | Path | Yetki | Açıklama |
|---|---|---|---|
| GET | `/` | Authenticated | Siparişlerim |
| GET | `/{id}` | Authenticated | Sipariş detayı |
| POST | `/` | Customer | Sipariş oluştur |
| PUT | `/{id}/confirm` | SellerOrAdmin | Siparişi onayla |
| PUT | `/{id}/ship` | SellerOrAdmin | Kargoya ver |
| PUT | `/{id}/deliver` | SellerOrAdmin | Teslim edildi |
| PUT | `/{id}/cancel` | Authenticated | İptal et |

### Diğer Endpoint'ler

- `GET/POST /api/v1/cart` — Sepet işlemleri (Redis)
- `GET/POST /api/v1/categories` — Hiyerarşik kategoriler
- `GET/POST /api/v1/coupons` — Kupon yönetimi
- `GET/POST /api/v1/addresses` — Adres yönetimi
- `GET/POST /api/v1/reviews` — Ürün değerlendirmeleri
- `POST /api/v1/payments` — Ödeme işlemleri

## Testler

```bash
# Unit testler
dotnet test tests/ECommerce.UnitTests

# Entegrasyon testleri
dotnet test tests/ECommerce.IntegrationTests

# Mimari testler
dotnet test tests/ECommerce.ArchitectureTests

# Tümü
dotnet test ECommerceAPI.sln
```

## Geliştirme Araçları

| Servis | URL | Credentials |
|---|---|---|
| Swagger UI | http://localhost:5000/swagger | — |
| Hangfire Dashboard | http://localhost:5000/hangfire | Dev only |
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| MailHog | http://localhost:8025 | — |
| Kibana | http://localhost:5601 | — |

## Proje Yapısı

```
ECommerceAPI/
├── .github/
│   └── workflows/
│       └── ci.yml               # GitHub Actions CI/CD
├── src/
│   ├── ECommerce.Domain         # Entity, Enum, Interface, Specification
│   ├── ECommerce.Application    # Features (CQRS), DTOs, Validators, Behaviors
│   ├── ECommerce.Infrastructure # AuthService, Redis, JWT, Email, RabbitMQ
│   ├── ECommerce.Persistence    # DbContext, Repositories, Migrations
│   └── ECommerce.API            # Controllers, Middleware, Program.cs
├── tests/
│   ├── ECommerce.UnitTests
│   ├── ECommerce.IntegrationTests
│   └── ECommerce.ArchitectureTests
├── docker-compose.yml           # Tüm servisler
├── docker-compose.override.yml  # Dev ortamı overrides
├── Dockerfile                   # Multi-stage build
└── ECommerceAPI.sln
```

## Lisans

MIT
