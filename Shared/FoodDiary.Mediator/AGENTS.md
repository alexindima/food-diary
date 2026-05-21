# Shared Mediator Guidelines

## Scope
Rules for `Shared/FoodDiary.Mediator/`.

## Role
- Own the lightweight in-process mediator abstractions and dispatch behavior shared by backend modules.
- Keep this package generic and independent from FoodDiary domain, application, infrastructure, presentation, and host projects.

## Rules
- Do not add feature-specific request, handler, or pipeline behavior here.
- Do not reference ASP.NET, EF Core, provider SDKs, or application/domain projects.
- Preserve cancellation propagation through mediator APIs.
- Keep public abstractions small and stable; changes here can affect every backend module.

## Commands
- Build: `dotnet build Shared/FoodDiary.Mediator/FoodDiary.Mediator.csproj`
- Tests: `dotnet test tests/FoodDiary.Mediator.Tests/FoodDiary.Mediator.Tests.csproj`
