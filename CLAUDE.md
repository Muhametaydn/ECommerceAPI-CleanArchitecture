# CLAUDE.md — ECommerceAPI Project Context

## Proje Özeti
.NET 8 ile Clean Architecture + CQRS tabanlı E-Ticaret RESTful API. Portfolio projesi. 17 haftalık yol haritası ile geliştiriliyor.

## Tech Stack
- **Framework:** .NET 8, ASP.NET Core Web API, EF Core 8
- **DB:** PostgreSQL (Npgsql), Redis (StackExchange.Redis — cart + response cache), Elasticsearch 8.x (ürün arama)
- **Auth:** ASP.NET Identity + JWT Bearer (access: 15dk, refresh: 7gün) + Token Rotation
- **Messaging:** RabbitMQ + MassTransit 8.x (consumers: OrderCreated, OrderStatusChanged, PaymentProcessed, LowStockAlert)
- **Background Jobs:** Hangfire (PostgreSQL storage) — OutboxProcessorJob her dakika çalışır
- **Email:** FluentEmail + MailKit SMTP (local: MailHog port 1025 / web UI port 8025)
- **Patterns:** Clean Architecture, CQRS (MediatR), Repository + UoW, Specification Pattern, Rich Domain Model, Domain Events, Outbox Pattern, Cache-Aside Pattern
- **Validation:** FluentValidation + MediatR Pipeline Behavior
- **Mapping:** AutoMapper
- **Test:** xUnit, Moq, FluentAssertions, NetArchTest
- **API Docs:** Swashbuckle/Swagger

## Solution Yapısı
```
ECommerceAPI/
├── src/
│   ├── ECommerce.Domain           # Entity, Enum, Interface, Specification, Constant
│   ├── ECommerce.Application      # Features (CQRS), DTOs, Validators, Behaviors, Mapping
│   ├── ECommerce.Infrastructure   # AuthService, RedisCartService, JWT, DI Registration
│   ├── ECommerce.Persistence      # DbContext, Repositories, UnitOfWork, Migrations, Configurations
│   └── ECommerce.API              # Controllers, Middleware, Program.cs
├── tests/
│   ├── ECommerce.UnitTests
│   ├── ECommerce.IntegrationTests
│   └── ECommerce.ArchitectureTests
└── ECommerceAPI.sln
```

## Katman Bağımlılık Kuralı
- Domain → hiçbir şeye bağımlı değil (saf C#)
- Application → sadece Domain
- Infrastructure & Persistence → Application (interface implementasyonları)
- API → hepsini referans eder (DI registration için)

## Domain Entities
Tümü `BaseEntity` (Id: Guid, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) sınıfından türer.

| Entity | Önemli Özellikler |
|---|---|
| Product | Price, SKU, StockQuantity, CategoryId. Metodlar: UpdatePrice(), DecreaseStock(), IncreaseStock() |
| Category | Hiyerarşik (MaxDepth=3), slug, parent-child. Metodlar: CanAddSubCategory(), SetParent() |
| Order | OrderNumber (ORD-YYYYMMDD-RANDOMID), State Machine: Pending→Confirmed→Shipped→Delivered/Cancelled/Refunded. Metodlar: Confirm(), Ship(), Deliver(), Cancel(), CalculateTotal() |
| OrderItem | Quantity, UnitPrice, TotalPrice |
| ApplicationUser | IdentityUser<Guid> + FirstName, LastName, ProfileImageUrl, IsActive |
| Cart | Redis-backed (DB'de yok), TTL: auth 30gün / anon 7gün |
| Payment | PaymentMethod (CreditCard/DebitCard/BankTransfer), PaymentStatus (Pending/Completed/Failed/Refunded) |
| Review | Title, Comment, Rating (1-5) |
| Coupon | DiscountType (Percentage/Fixed), MinimumOrderAmount, UsageLimit, ExpiryDate |
| Address | Title, AddressLine, City, District, PostalCode, Country (default: Türkiye) |
| RefreshToken | Token rotation: CreatedByIp, RevokedAt, ReplacedByToken |

## API Controllers (api/v1/[controller])
AuthController, ProductsController, OrdersController, CartController, CategoriesController, CouponsController, AddressesController, ReviewsController, PaymentsController

## Auth & Güvenlik
- JWT: Secret + Issuer + Audience appsettings.json'da
- Roller: Admin, Customer, Seller (AppRoles constant)
- Policies: AdminOnly, SellerOrAdmin, CustomerOnly, Authenticated
- Rate Limiting: auth 5/dk, api 30/dk, global 60/dk
- GlobalExceptionMiddleware: RFC 7807 ProblemDetails
- ETagMiddleware: GET yanıtları için MD5 hash → If-None-Match / 304 Not Modified desteği

## Application Layer Pattern
Her feature klasörü şu yapıda:
```
Features/
  Products/
    Commands/
      CreateProduct/
        CreateProductCommand.cs
        CreateProductCommandHandler.cs
        CreateProductCommandValidator.cs
    Queries/
      GetAllProducts/
        GetAllProductsQuery.cs
        GetAllProductsQueryHandler.cs
        GetAllProductsQueryValidator.cs
    DTOs/
      ProductDto.cs
    Mapping/
      ProductMappingProfile.cs
    Specifications/
      ProductFilterSpecification.cs
```

## Önemli Dosya Yolları
- Program.cs: `src/ECommerce.API/Program.cs`
- DbContext: `src/ECommerce.Persistence/Context/ApplicationDbContext.cs`
- AuthService: `src/ECommerce.Infrastructure/Services/AuthService.cs`
- RedisCartService: `src/ECommerce.Infrastructure/Services/RedisCartService.cs`
- GenericRepository: `src/ECommerce.Persistence/Repositories/GenericRepository.cs`
- UnitOfWork: `src/ECommerce.Persistence/Repositories/UnitOfWork.cs`
- ValidationBehavior: `src/ECommerce.Application/Common/Behaviors/ValidationBehavior.cs`
- GlobalExceptionMiddleware: `src/ECommerce.API/Middlewares/GlobalExceptionMiddleware.cs`
- ETagMiddleware: `src/ECommerce.API/Middlewares/ETagMiddleware.cs`
- CachingBehavior: `src/ECommerce.Application/Common/Behaviors/CachingBehavior.cs`
- ICacheService / ICacheableRequest: `src/ECommerce.Application/Common/Interfaces/`
- RedisCacheService: `src/ECommerce.Infrastructure/Services/RedisCacheService.cs`
- ElasticsearchService: `src/ECommerce.Infrastructure/Services/ElasticsearchService.cs`
- ElasticsearchSettings: `src/ECommerce.Infrastructure/Settings/ElasticsearchSettings.cs`
- SearchProducts Query: `src/ECommerce.Application/Features/Products/Queries/SearchProducts/`
- Performance Migration: `src/ECommerce.Persistence/Migrations/20260507100000_AddPerformanceIndexes.cs`
- Seed Data: `src/ECommerce.Persistence/Migrations/` (3 kategori: Elektronik, Giyim, Ev & Yaşam)
- OutboxRepository: `src/ECommerce.Persistence/Repositories/OutboxRepository.cs`
- OutboxProcessorJob: `src/ECommerce.Infrastructure/Jobs/OutboxProcessorJob.cs`
- MassTransitEventBus: `src/ECommerce.Infrastructure/EventBus/MassTransitEventBus.cs`
- EmailService: `src/ECommerce.Infrastructure/Services/EmailService.cs`
- UserLookupService: `src/ECommerce.Infrastructure/Services/UserLookupService.cs`
- docker-compose.yml: repo kökünde (postgres, redis, rabbitmq, mailhog, elasticsearch, kibana, api)
- docker-compose.override.yml: repo kökünde (dev ortamı overrides)
- Dockerfile: repo kökünde (multi-stage build)
- .dockerignore: repo kökünde
- CI/CD Pipeline: `.github/workflows/ci.yml`
- README.md: repo kökünde

## Mevcut Durum (Faz Takibi)

| Faz | Konu | Durum |
|---|---|---|
| 1 | Temel Mimari & Altyapı (Clean Arch, Domain, EF Core, Repo+UoW, CQRS, Error Handling) | TAMAMLANDI |
| 2 | Auth & Yetkilendirme (Identity, JWT+Refresh, Policy-Based Auth, Rate Limiting) | TAMAMLANDI |
| 3 | Core E-Ticaret (Ürün, Kategori, Sepet, Sipariş, Ödeme, Stok, Kupon) | TAMAMLANDI |
| 4a | Domain Events + Integration Events + Outbox Pattern | TAMAMLANDI |
| 4b | RabbitMQ + MassTransit entegrasyonu, Hangfire background jobs, FluentEmail bildirimleri | TAMAMLANDI |
| 5 | Performans & Caching (Redis Cache, ETag, DB Optimization, Elasticsearch) | TAMAMLANDI |
| 6 | Test & Kalite (Unit, Integration, Architecture Tests) | TAMAMLANDI |
| 7 | DevOps & Deployment (Docker, CI/CD, Logging, Versioning, README) | TAMAMLANDI |

## Sonraki Adım
Tüm fazlar tamamlandı. Proje production-ready durumda.

## DevOps & Deployment Mimarisi (Faz 7)
- **Dockerfile**: Multi-stage build (sdk:8.0 → aspnet:8.0), non-root user, `/app/logs` volume
- **docker-compose.override.yml**: Dev ortamı overrides (daha az RAM, Development env)
- **docker-compose.yml API servisi**: api servisi eklendi; postgres, rabbitmq, elasticsearch health check bağımlılıkları var
- **Serilog**: `builder.Host.UseSerilog()` ile yapılandırıldı. Console (renkli template) + File (rolling daily, 7 gün retention) sink. Bootstrap logger ile startup hataları da yakalanır. `UseSerilogRequestLogging()` ile HTTP istek logları.
- **API Versioning**: `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` paketleri. URL segment (`/api/v1/`) + `X-Api-Version` header desteği. Tüm controller'lara `[ApiVersion("1.0")]` eklendi.
- **GitHub Actions** (`.github/workflows/ci.yml`):
  - `build-and-test` job: restore → build → unit tests → arch tests → integration tests (PostgreSQL service container ile). Test sonuçları artifact olarak yüklenir.
  - `docker` job: `main` branch push'ta tetiklenir. ghcr.io'ya image push eder. BuildKit cache ile hızlı build.
- **README.md**: Mimari diyagram, tech stack tablosu, hızlı başlangıç, environment variables, tüm API endpoint listesi, geliştirme araçları URL'leri, proje yapısı.

## Domain Events & Integration Events Mimarisi (Faz 4a)
- `IDomainEvent` → Domain katmanında saf C# marker interface
- `BaseEntity.DomainEvents` → entity metodlarında event raise edilir (örn: `order.Confirm()`)
- `DomainEventNotification<T>` → MediatR için Application katmanı wrapper'ı
- `DomainEventDispatcher` → MediatR Publish ile in-process dispatch (Infrastructure)
- `OutboxMessage` → integration event'leri DB'de saklar (Outbox Pattern)
- `INotificationHandler<DomainEventNotification<T>>` → handler'lar outbox tablosuna yazar
- Yeni dosyalar: `Domain/Events/`, `Domain/Outbox/`, `Application/IntegrationEvents/`, `Application/Common/Interfaces/`, `Infrastructure/Events/`, `Persistence/Repositories/OutboxRepository.cs`

## RabbitMQ + MassTransit + Hangfire Mimarisi (Faz 4b)
- `MassTransitEventBus` → `IEventBus` implementasyonu; `IPublishEndpoint.Publish<T>()` ile RabbitMQ'ya iletir
- `OutboxProcessorJob` → `[DisableConcurrentExecution(30)]` Hangfire job; her dakika çalışır
  - DB'den işlenmemiş OutboxMessage'ları batch (50) okur
  - JSON deserialize → reflection ile `IEventBus.PublishAsync<T>()` çağırır
  - Başarıda `MarkAsProcessedAsync`, hataDA `MarkAsFailedAsync` (RetryCount < 5 limiti var)
- **Consumers** (`Infrastructure/Consumers/`):
  - `OrderCreatedConsumer` → sipariş onay e-postası
  - `OrderStatusChangedConsumer` → durum değişikliği e-postası
  - `PaymentProcessedConsumer` → ödeme makbuzu e-postası
  - `LowStockAlertConsumer` → admin'e düşük stok uyarısı
- `IUserLookupService` → `UserManager<ApplicationUser>` ile UserId → (Email, FullName) çözümleme
- `EmailService` → FluentEmail + MailKitSender, HTML şablonlar, Türkçe durum çevirileri
- **Docker servisleri** (docker-compose.yml): postgres:16, redis:7, rabbitmq:3.13-management, mailhog
- Hangfire Dashboard: `/hangfire` (sadece Development ortamı)
- RabbitMQ Management UI: `localhost:15672` (guest/guest)
- MailHog Web UI: `localhost:8025`
- Yeni dosyalar: `Infrastructure/EventBus/MassTransitEventBus.cs`, `Infrastructure/Consumers/`, `Infrastructure/Jobs/OutboxProcessorJob.cs`, `Infrastructure/Services/EmailService.cs`, `Infrastructure/Services/UserLookupService.cs`, `Infrastructure/Settings/RabbitMQSettings.cs`, `Infrastructure/Settings/EmailSettings.cs`, `docker-compose.yml`

## Performans & Caching Mimarisi (Faz 5)

### Redis Cache (Cache-Aside Pattern)
- `ICacheService` → Application katmanında interface (GetAsync, SetAsync, RemoveAsync, RemoveByPrefixAsync)
- `RedisCacheService` → Infrastructure'da `IDistributedCache` + `IConnectionMultiplexer` ile implementasyon
- `ICacheableRequest` → Query'lerin implement ettiği interface; `CacheKey` + `CacheDuration` tanımlar
- `CachingBehavior<TRequest,TResponse>` → MediatR pipeline; ICacheableRequest olan tüm query'leri otomatik cache'ler
- **Cacheable query'ler**: GetAllProductsQuery (3dk), GetProductById (10dk), GetCategoryTree/ById/Slug (30dk)
- **Cache invalidation**: Her Create/Update/Delete command sonrası `RemoveByPrefixAsync` ile prefix temizleme
  - Ürün yazmaları: `products:list:*` + `products:single:{id}` temizler
  - Kategori yazmaları: `categories:*` temizler

### ETag / HTTP Cache Validation
- `ETagMiddleware` → Response body'sinden MD5 hash hesaplar, `ETag` header ekler
- İstemci `If-None-Match` header'ı ile gönderirse ve hash eşleşirse → 304 Not Modified (body yok)
- Sadece GET + 2xx + JSON yanıtlara uygulanır

### DB Optimizasyonu
- `GenericRepository`: `ApplySpecification()`, `GetAllAsync()`, `GetWhereAsync()`, `FirstOrDefaultAsync()` → `AsNoTracking()`
- `ProductRepository`: Tüm read-only metodlar `AsNoTracking()` ile işaretlendi
- `GetByIdAsync` → tracking aktif (write handler'ları kullanır)
- Yeni indexler (migration `20260507100000_AddPerformanceIndexes`):
  - Products: `CategoryId`, `(IsActive, Price)`, `(IsActive, CreatedAt)`, `(CategoryId, IsActive, Price)`
  - Orders: `UserId`, `(UserId, Status)`, `Status`, `CreatedAt`

### Elasticsearch (Ürün Arama)
- Paket: `Elastic.Clients.Elasticsearch` 8.16.x
- `ProductSearchDocument` → flat ES belgesi (ürün + kategori adı)
- `ISearchService` → SearchProductsAsync, IndexProductAsync, DeleteProductFromIndexAsync, ReindexAllAsync
- `ElasticsearchService` → multi-match fuzzy arama (name^3, description, sku), range filter, sort
- `SearchProductsQuery` → MediatR handler aracılığıyla `GET /api/v1/products/search`
- **Write senkronizasyonu**: Create/Update → IndexProductAsync, Delete → DeleteProductFromIndexAsync (paralel Task.WhenAll)
- **Docker**: elasticsearch:8.13.4 (port 9200), kibana:8.13.4 (port 5601) docker-compose'a eklendi
- appsettings.json → `Elasticsearch.Uri` + `Elasticsearch.ProductIndexName`

## Kodlama Kuralları
- Entity'lerde Rich Domain Model — iş mantığı entity içinde (anemic model yok)
- Her yeni feature: Command/Query + Handler + Validator + DTO + MappingProfile
- Guid primary key, int ID yok
- Türkçe kategori slug'larında karakter dönüşümü mevcut (ş→s, ü→u vb.)
- SaveChangesAsync override: audit field dolumu + domain event dispatch + outbox yazımı
- Domain event'ler entity metodlarında AddDomainEvent() ile raise edilir, SaveChanges sonrası dispatch olur
- Specification Pattern ile karmaşık sorgu filtreleme
