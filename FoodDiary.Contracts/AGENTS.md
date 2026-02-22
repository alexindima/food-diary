# Contracts Guidelines

## Scope
Rules for `FoodDiary.Contracts/`.

## Responsibilities
- Hold shared contracts used across boundaries (DTOs/messages/constants that are truly shared).
- Keep contracts stable and version-friendly.

## Rules
- Keep contracts framework-agnostic and lightweight.
- Avoid domain behavior/business logic in contracts.
- Prefer explicit nullable annotations and clear naming.

## Compatibility
- Treat public contract changes as breaking unless proven otherwise.
- When changing existing contract fields, assess API/client impact first.

## Commands
- Build: `dotnet build FoodDiary.Contracts/FoodDiary.Contracts.csproj`
