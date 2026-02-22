# Infrastructure Layer Guidelines

## Scope
Rules for `FoodDiary.Infrastructure/`.

## Responsibilities
- EF Core persistence, external integrations, and technical implementations.
- Implement abstractions declared in upper layers.

## Data Access
- Keep `DbContext` and entity configurations here.
- Use Fluent API for mapping and constraints.
- Keep migrations in this project.

## Rules
- Do not move domain rules from aggregates into persistence code.
- Keep dependency direction inward (Infrastructure depends on Application/Domain, not vice versa).
- Keep retries/logging policies consistent with API composition.

## Commands
- Build: `dotnet build FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`

## EF Core Migrations
- Always commit both files for each migration: `*.cs` and `*.Designer.cs`.
