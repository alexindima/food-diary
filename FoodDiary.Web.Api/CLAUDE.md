# FoodDiary.Web.Api

ASP.NET Core composition root — wires Application, Infrastructure, and Presentation.Api layers together. Contains zero controllers.

## Program.cs

Minimal entry point:
- Kestrel max request body: 10 MB
- `builder.Services.AddApiServices(builder.Configuration)` — all DI
- `app.UseApiPipeline()` — all middleware
- `public partial class Program;` for integration test `WebApplicationFactory<Program>`

## DI Wiring (`Extensions/ApiServiceCollectionExtensions.cs`)

Layered composition:
1. `AddApplication()` — MediatR, FluentValidation, application services
2. `AddInfrastructure(configuration)` — EF Core, repositories, external services
3. `AddPresentationApi()` — controllers, versioning, SignalR, filters
4. Cross-cutting (registered here):
   - Distributed memory cache
   - 6 options classes with `ValidateOnStart()`
   - JWT Bearer authentication (symmetric key, zero ClockSkew, SignalR token from query string for `/hubs/email-verification`)
   - Authorization, HTTP logging, ProblemDetails + `ApiExceptionHandler`
   - Rate limiter, output cache
   - Swagger/OpenAPI with Bearer security definition
   - OpenTelemetry tracing + metrics via OTLP (optional — null if no endpoint configured)
   - Health checks: PostgreSQL, S3, SMTP

## Middleware Pipeline (`Extensions/ApiApplicationBuilderExtensions.cs`)

Order: ExceptionHandler -> ForwardedHeaders -> SecurityHeaders -> HttpLogging -> RequestObservability -> (Dev: Swagger; Prod: HSTS+HTTPS) -> CORS -> Authentication -> RateLimiter -> Authorization -> OutputCache -> MapControllers + MapHub

## Cross-Cutting Concerns

### Global Exception Handler (`Extensions/ApiExceptionHandler.cs`)
- `CurrentUserUnavailableException` -> 401
- `DbUpdateConcurrencyException` -> 409
- Everything else -> 500

### Rate Limiting (`Options/RateLimiterOptionsSetup.cs`)
- Auth: 5 req / 60s (fixed window, partitioned by userId or IP)
- AI: 10 req / 60s

### Output Cache (`Options/OutputCacheOptionsSetup.cs`)
- AdminAiUsage: 15s, varies by query string
- UserScoped: 5s, varies by query string + Authorization header

### Security Headers (`Extensions/SecurityHeadersMiddleware.cs`)
X-Content-Type-Options, X-Frame-Options, Referrer-Policy, X-Permitted-Cross-Domain-Policies, Permissions-Policy

### Telemetry (`Extensions/ApiTelemetry.cs`)
OTel instruments: `fooddiary.api.requests`, `fooddiary.api.request.duration`, `fooddiary.api.request.exceptions`, `fooddiary.api.rate_limit.rejections`, `fooddiary.api.business_flow.events`, `fooddiary.api.output_cache.events`

## Configuration

- `appsettings.json` / `.Development.json` / `.Production.json` / `.Template.json`
- Config sections: Database, S3, Jwt, TelegramAuth, OpenAi, Email, Cors, ForwardedHeaders, RateLimiting, OutputCache, OpenTelemetry

## Constraints

- References all three layers: Application, Infrastructure, Presentation.Api
- No controllers — all HTTP endpoints live in Presentation.Api
- This is the only runnable backend host
