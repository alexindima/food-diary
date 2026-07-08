# Domain Primitives Guidelines

## Scope
Rules for `Shared/FoodDiary.Domain.Primitives/`.

## Role
- Own domain model primitives shared by backend bounded contexts.
- Keep this package generic and independent from FoodDiary application, infrastructure, presentation, host, and feature projects.

## Rules
- Do not add feature-specific entities, value objects, domain events, repositories, handlers, or services here.
- Do not reference ASP.NET, EF Core, provider SDKs, or application/infrastructure projects.
- Keep public abstractions small and stable; changes here can affect every backend domain module.
- Keep domain entities auditable through `Entity<TId>` unless a future bounded context explicitly proves it needs a separate primitive.

## Commands
- Build: `dotnet build Shared/FoodDiary.Domain.Primitives/FoodDiary.Domain.Primitives.csproj`
