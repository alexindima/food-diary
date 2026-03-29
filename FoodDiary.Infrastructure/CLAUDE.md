# FoodDiary.Infrastructure

Implements all external concerns: EF Core persistence (PostgreSQL), S3 storage, JWT auth, Telegram auth, OpenAI, SMTP email, telemetry.

## Architecture

### Persistence (`Persistence/`)
- **FoodDiaryDbContext** — 20 DbSets, configurations auto-discovered via `ApplyConfigurationsFromAssembly`
- **FoodDiaryDbContextFactory** — `IDesignTimeDbContextFactory` for `dotnet ef` CLI. Falls back to `Host=localhost;Database=food_diary;Username=postgres;Password=postgres`
- **Configurations/** — `internal sealed` IEntityTypeConfiguration classes. Key conventions:
  - `xmin` row version for optimistic concurrency on core entities (User, Meal, Recipe, Product, ShoppingList)
  - Inline `HasConversion()` with lambdas for strongly-typed IDs (not global converters)
  - `PropertyAccessMode.Field` on navigation collections for DDD encapsulation
  - Enums stored as strings (`ActivityLevel`, `RecentItemType`)
  - Owned types for complex value objects (`CycleDay.Symptoms`)
- **Converters/** — 9 `ValueConverter<TId, Guid>` singletons
- **Interceptors/** — `DomainEventDispatchInterceptor` (currently logs events, does not dispatch via MediatR)
- **Repositories** — feature-organized subfolders, each implementing an Application-layer interface

### Repository Conventions
- Primary constructor injecting `FoodDiaryDbContext`
- Registered as **Scoped** in DI
- `AsNoTracking()` by default for reads; `asTracking` boolean parameter when needed
- `SaveChangesAsync()` called immediately per write (no Unit of Work across repositories)
- Paged queries: separate count + fetch-by-IDs to avoid cartesian explosion
- `.AsSplitQuery()` for entities with multiple Include chains (Meal, Recipe)
- `EF.Functions.ILike()` with proper LIKE pattern escaping for search

### Authentication (`Authentication/`)
- `JwtTokenGenerator` — HMAC-SHA256, zero ClockSkew, access + refresh tokens
- `TelegramAuthValidator` — Mini App initData HMAC verification with constant-time comparison
- `TelegramLoginWidgetValidator` — Login Widget hash verification (different key derivation)
- `AdminSsoService` — one-time codes via IDistributedCache with 2-minute TTL

### Services (`Services/`)
- `S3ImageStorageService` — presigned PUT URLs (15min), keys: `users/{userId}/images/{guid}-{file}`, allowed: JPEG/PNG/WebP/GIF
- `OpenAiFoodService` — OpenAI Responses API (`/v1/responses`), vision + nutrition, JSON Schema structured output, auto-fallback model, monthly quota per user
- `SmtpEmailSender` — DB-backed templates with memory cache + hardcoded fallback, locale-aware (ru/en)
- `PasswordHasher` — BCrypt wrapper
- `InfrastructureTelemetry` — `System.Diagnostics.Metrics` counters for AI, DB, email, storage

### Options (`Options/`)
Six option classes bound from config sections with `Validate()` + `ValidateOnStart()`: DatabaseOptions, JwtOptions, S3Options, OpenAiOptions, EmailOptions, TelegramAuthOptions.

## DI Registration (`DependencyInjection.cs`)
`AddInfrastructure(IServiceCollection, IConfiguration)` registers: memory cache, options, DbContext (Npgsql with retry), S3 client (singleton, supports MinIO), 15 scoped repositories, singleton services (JWT, password, Telegram, S3, email), OpenAI via `AddHttpClient` with Polly circuit breaker.

## Migrations (`Migrations/`)
~40+ migrations from `20251108` to `20260216`. Both `.cs` and `.Designer.cs` files must be committed together.

## Constraints

- References only `FoodDiary.Application` (which transitively includes Domain)
- Does NOT reference Web.Api or Presentation.Api
- Internal abstractions for testability: `IObjectStorageClient` wraps `IAmazonS3`, `IEmailTransport` wraps `SmtpClient`
