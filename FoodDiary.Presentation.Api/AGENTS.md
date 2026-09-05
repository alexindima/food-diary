# Presentation API Guidelines

## Scope
Rules for `FoodDiary.Presentation.Api/`.

## Role
- Treat this project as the shared HTTP/SignalR presentation kernel, not as the executable host or a feature container.
- Keep reusable ASP.NET transport primitives here; feature controllers, DTOs and mappings belong in `Modules/<Feature>/Presentation`.
- Keep composition root, environment wiring, and middleware orchestration in `FoodDiary.Web.Api`.

## Architecture
- Keep only genuinely shared or version-neutral endpoints under `Features/`; organize business endpoints feature-first inside their owning module Presentation project.
- Keep controllers thin: accept transport model, resolve route/query/current-user context, map to application request, call MediatR, map result to HTTP response.
- Do not put business logic in controllers.
- Keep feature HTTP request/response mapping in `Modules/<Feature>/Presentation`, not in Application or this shared kernel.
- Do not reference `FoodDiary.Infrastructure` or `FoodDiary.Web.Api` from this project.
- Do not reference `FoodDiary.Domain` directly; map through application requests/models.
- Do not introduce or revive `FoodDiary.Contracts` namespaces/projects.

## Structure
- Base controllers and binders: `Controllers/`
- Shared/version-neutral controllers: `Features/`; module feature controllers, requests, responses and mappings: `Modules/<Feature>/Presentation/Features/`
- Reusable HTTP responses/wrappers: `Responses/`
- Presentation-only services: `Services/`
- Auth/presentation policies: `Authorization/`, `Policies/`, `Security/`
- ASP.NET filters and filter attributes: `Filters/`
- SignalR hubs and hub method constants: `Hubs/`
- Presentation option records: `Options/`
- Presentation telemetry attributes/helpers: `Telemetry/`
- Shared registration and endpoint mapping: `Extensions/`; every module exposes an explicit `Add<Feature>Presentation` registration.

## Naming
- Use `*HttpRequest` for request bodies.
- Use `*HttpQuery` for grouped query parameters.
- Use `*HttpResponse` for HTTP response models.
- Use `*HttpMappings` / `*HttpResponseMappings` for presentation mappings.

## Controller Flow
Preferred flow:

1. Receive `HttpRequest` / `HttpQuery` model from this project.
2. Resolve route values, query values, or current user context.
3. Map to application command/query.
4. Call `Send(...)` from `BaseApiController`.
5. Map application output to HTTP response model.
6. Return `IActionResult` through `ResultExtensions` where appropriate.

Target shape:

- `Controller -> HttpRequest/HttpQuery -> HttpMappings -> Command/Query -> MediatR -> App Model -> HttpResponse`

## Current Conventions
- Current-user access should use `[FromCurrentUser]` instead of manually parsing claims.
- Controllers should prefer `Send(...)` from base controller so request cancellation uses `HttpContext.RequestAborted`.
- Controllers should not call `Mediator.Send(...)` directly.
- Controllers should not return ad-hoc `BadRequest`, `Unauthorized`, `Conflict`, `NotFound`, `Forbid`, or `StatusCode` responses. Use shared result/error mapping.
- Error responses should use the standard `ApiErrorHttpResponse` contract.
- Unhandled exceptions are normalized in `FoodDiary.Web.Api`; do not add ad-hoc try/catch in controllers unless behavior is endpoint-specific.
- Expensive or abuse-prone endpoints may use presentation policy names from `Policies/PresentationPolicyNames.cs`.

## SignalR
- Keep hub transport/auth concerns here.
- `EmailVerificationHub` is part of the presentation boundary and should stay thin.
- User identity for hubs should continue to flow through presentation `IUserIdProvider`.

## Testing Expectations
- If you change controller transport behavior, update presentation or integration tests.
- Contract-sensitive changes should be covered in:
  - `tests/FoodDiary.Presentation.Api.Tests`
  - `tests/FoodDiary.Web.Api.IntegrationTests`
- Preserve OpenAPI and error-contract expectations unless the contract change is intentional.
- If the public HTTP contract changes intentionally, update the matching snapshots in `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`.
- For Swagger/OpenAPI changes, refresh at least `openapi-full-contract.json`, and update the narrower OpenAPI snapshots too when their selected endpoints changed.
- Architecture tests enforce that only base controllers remain in `Controllers/`; new endpoint controllers belong in feature folders.

## Migration Rules
- Do not introduce a new contracts project for HTTP request DTOs.
- Do not move HTTP mapping logic into `FoodDiary.Application`.
- When adding a new feature, follow the existing feature folder pattern immediately instead of using legacy flat layout.
