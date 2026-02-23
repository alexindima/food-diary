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
- Normalize date/time inputs consistently:
  - For timestamps: convert to UTC with `ToUniversalTime()`.
  - For date-only domain fields: store UTC date (`utc.Date` with `DateTimeKind.Utc`).
- Prefer explicit validation errors over silent correction for invalid domain input ranges.
- Canonicalize user-facing codes on write (e.g. language/gender codes) and trim profile text input.
- Enforce invariants in link entities as well (reject `Empty` IDs in constructors/factories).

## Commands
- Build: `dotnet build FoodDiary.Domain/FoodDiary.Domain.csproj`
