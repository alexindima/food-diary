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
- Preserve the result invariant: successes use `Error.None`, while failures always contain a non-null error.
- Reject null error codes and messages at construction boundaries and reject blank codes or messages in failures.
- Snapshot error details so later mutations of caller-owned collections cannot change an existing error.
- Keep `Error` value equality structural, including field-specific details.

## Commands
- Build: `dotnet build Shared/FoodDiary.Results/FoodDiary.Results.csproj`
