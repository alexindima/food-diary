# Application Abstractions Guidelines

## Scope
Rules for `FoodDiary.Application.Abstractions/`.

## Role
- Own application-facing contracts used by the primary FoodDiary application and its adapters.
- Keep interfaces and models close to their feature unless they are genuinely cross-cutting.
- Keep this project independent from hosts, presentation, infrastructure implementations, provider SDKs, and ASP.NET transport details.

## Structure
- Feature-specific contracts should live under `Feature/Common/`, `Feature/Abstractions/`, or `Feature/Services/`.
- Shared cross-feature contracts live under `Common/Abstractions/...` or `Common/Interfaces/...`.
- Keep `Common/Interfaces/Persistence` intentionally small. Architecture tests currently allow only the cross-feature repositories already listed there.
- Keep namespaces aligned with folder paths.

## Rules
- Do not reference `FoodDiary.Web.Api`, `FoodDiary.Presentation.Api`, or `FoodDiary.Infrastructure`.
- Do not introduce ASP.NET types such as `HttpContext`, `IActionResult`, or `ControllerBase`.
- Do not bind configuration directly here with `IConfiguration` or `IOptions<T>`.
- Async interfaces should accept `CancellationToken`.
- Prefer narrow feature contracts over regrowing flat shared repository/service buckets.

## Commands
- Build: `dotnet build FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
