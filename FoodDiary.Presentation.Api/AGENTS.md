# Presentation API Guidelines

## Scope
Rules for `FoodDiary.Presentation.Api/`.

## Role
- Treat this project as the HTTP/SignalR presentation layer, not as the executable host.
- Keep ASP.NET transport concerns here.
- Keep composition root, environment wiring, and middleware orchestration in `FoodDiary.Web.Api`.

## Architecture
- Organize code feature-first under `Features/<FeatureName>/`.
- Keep controllers thin: accept transport model, resolve route/query/current-user context, map to application request, call MediatR, map result to HTTP response.
- Do not put business logic in controllers.
- Keep HTTP request/response mapping in `FoodDiary.Presentation.Api`, not in `FoodDiary.Application`.
- Do not reference `FoodDiary.Infrastructure` or `FoodDiary.Web.Api` from this project.

## Structure
- Base controllers and binders: `Controllers/`
- Feature controllers, requests, responses, mappings: `Features/`
- Reusable HTTP responses/wrappers: `Responses/`
- Presentation-only services: `Services/`
- Auth/presentation policies: `Authorization/`, `Policies/`, `Security/`
- Registration and endpoint mapping: `Extensions/`

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

## Migration Rules
- Do not introduce new HTTP request DTOs into `FoodDiary.Contracts`.
- Do not move HTTP mapping logic into `FoodDiary.Application`.
- When adding a new feature, follow the existing feature folder pattern immediately instead of using legacy flat layout.
