# Domain Layer Guidelines

## Scope
Rules for `FoodDiary.Domain/`.

## Responsibilities
- Keep pure domain model: entities, value objects, domain services, domain events.
- Enforce invariants inside aggregates.
- Prefer strongly typed IDs/value objects on public surfaces.

## Boundaries
- No infrastructure concerns (EF, HTTP, external services).
- No UI/API contracts.

## Design Rules
- Prefer factory/static creation methods when invariants are non-trivial.
- Keep behavior close to data (rich domain where useful).
- Keep namespaces aligned with folder structure.

## Commands
- Build: `dotnet build FoodDiary.Domain/FoodDiary.Domain.csproj`
