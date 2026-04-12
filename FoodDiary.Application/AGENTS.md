# Application Layer Guidelines

## Scope
Rules for `FoodDiary.Application/`.

## Feature-First Layout
For each feature (`Products`, `Recipes`, etc.), prefer:
- `Feature/Commands/<Action>/`
- `Feature/Queries/<Action>/`
- `Feature/Mappings/`
- Optional: `Feature/Common/`
- Optional: `Feature/Services/`

Keep namespaces aligned with folder paths.

## CQRS + Validation
- Commands/queries with focused handlers.
- FluentValidation validators per request model.
- Prefer a dedicated `{RequestName}Validator` for request-shape validation instead of embedding those checks in handlers.
- Keep handler-side validation only for runtime/domain guards that depend on repository state, strongly typed ID construction, enum parsing, or aggregate/value-object invariants.
- Reuse feature-level common models from `Feature/Common/`.

## Domain Interaction
- Respect strongly typed IDs/value objects.
- Preserve aggregate invariants; avoid leaking primitives in public contracts.
- Prefer `Enum.TryParse(..., out ...)` in handlers/services and return validation errors instead of relying on `Enum.Parse` exceptions.
- Do not rely only on FluentValidation for request safety when a handler/service constructs value objects or parses enums; guard invalid or empty IDs in the handler/service path and return a normal failure instead of letting exceptions define control flow.
- Propagate the incoming `CancellationToken` to all async service/repository calls (avoid `CancellationToken.None` in request flow).
- For time-dependent logic in handlers/services, prefer `IDateTimeProvider` over direct `DateTime.UtcNow`.
- For authentication flows, centralize token issuance/refresh-token persistence via `IAuthenticationTokenService` instead of duplicating token code in each handler.

## Commands
- Build: `dotnet build FoodDiary.Application/FoodDiary.Application.csproj`

## Migration Guidance
- Migrate legacy flat/partial structures incrementally, feature by feature.
- Prefer moving shared request models out of `Commands/Common` into `Feature/Common`.
