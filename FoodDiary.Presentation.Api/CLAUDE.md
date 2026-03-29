# FoodDiary.Presentation.Api

HTTP presentation layer (class library, not a runnable host). Houses all controllers, HTTP DTOs, mapping logic, error formatting, and SignalR hubs.

## Architecture

### Controller Hierarchy
- `BaseApiController` — abstract, `[ApiVersion("1.0")]`, provides `Mediator`, `HandleOk/HandleCreated/HandleNoContent` + `HandleObserved*` variants for telemetry
- `AuthorizedController` — adds `[Authorize]` on top of BaseApiController
- Auth/Admin controllers extend `BaseApiController` directly (handle their own auth)
- All other feature controllers extend `AuthorizedController`

### Feature-Slice Organization (`Features/`)
16 feature areas (Admin, Ai, Auth, Consumptions, Cycles, Dashboard, Goals, Hydration, Images, Products, Recipes, ShoppingLists, Statistics, Users, WaistEntries, WeightEntries), each containing:
```
Features/{Feature}/
  {Feature}Controller.cs          — thin controller delegating to MediatR
  Requests/                       — sealed record HTTP request DTOs
  Responses/                      — sealed record HTTP response DTOs
  Mappings/                       — static extensions: HTTP DTO <-> Application command/query/model
```

### User Context
`[FromCurrentUser] Guid userId` attribute backed by `CurrentUserIdModelBinder` — reads `ClaimTypes.NameIdentifier` from JWT. Missing claim throws `CurrentUserUnavailableException` (caught globally as 401).

### Error Response
- `ApiErrorHttpResponse` — uniform error envelope (code, message, traceId, details)
- `PresentationErrorHttpMapper` — maps `ErrorKind` to HTTP status codes
- `ResultExtensions` — converts `Result<T>` to appropriate `IActionResult`
- `ApiErrorDetailsMapper` — normalizes validation error keys to camelCase

## Key Conventions

### Thin Controllers
Every action is a single expression calling a `Handle*` method. **Never** put business logic in controllers. **Never** call `Mediator.Send()` directly — use the Handle* helpers.

### DTO Separation
HTTP request/response DTOs are separate from Application-layer command/query/model types. Mapping happens via **static extension methods** in `Mappings/` folders. Mapping lambdas passed to `Handle*` use the `static` keyword to prevent closure allocations.

### Route Pattern
`[Route("api/v{version:apiVersion}/{feature}")]` with `[ApiController]`

### Forbidden Patterns (enforced by architecture tests)
- No `Mediator.Send(` directly — use `HandleOk/HandleCreated/HandleNoContent`
- No `BadRequest()`, `NotFound()`, `Ok()` etc. — results go through `ResultExtensions`
- No parsing claims directly — use `[FromCurrentUser]`
- No `FoodDiary.Domain` imports — only reference Application-layer types
- No `FoodDiary.Contracts` namespace usage

## DI Registration
`AddPresentationApi()` registers: MVC controllers with API versioning (default v1.0), SignalR hub, `TelemetryActionFilter` + `IdempotencyFilter` as global filters, custom `InvalidModelStateResponseFactory`.

`MapPresentationApi()` maps controllers + SignalR hub `/hubs/email-verification`.

## Constraints

- References only `FoodDiary.Application` (never Infrastructure or Web.Api)
- SDK: `Microsoft.NET.Sdk` (class library, not web host)
- Only NuGet: `Asp.Versioning.Mvc`
