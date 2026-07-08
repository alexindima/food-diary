# Results Guidelines

## Scope
Rules for `Shared/FoodDiary.Results/`.

## Role
- Own result and error primitives shared by backend modules.
- Keep this package generic and independent from FoodDiary domain, application, infrastructure, presentation, and host projects.

## Rules
- Do not add feature-specific errors, request models, handlers, or services here.
- Do not reference ASP.NET, EF Core, provider SDKs, or application/domain projects.
- Keep public abstractions small and stable; changes here can affect every backend module.

## Commands
- Build: `dotnet build Shared/FoodDiary.Results/FoodDiary.Results.csproj`
