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
- Reuse feature-level common models from `Feature/Common/`.

## Domain Interaction
- Respect strongly typed IDs/value objects.
- Preserve aggregate invariants; avoid leaking primitives in public contracts.

## Commands
- Build: `dotnet build FoodDiary.Application/FoodDiary.Application.csproj`

## Migration Guidance
- Migrate legacy flat/partial structures incrementally, feature by feature.
- Prefer moving shared request models out of `Commands/Common` into `Feature/Common`.
