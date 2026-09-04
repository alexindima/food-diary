# Application Abstractions Guidelines

## Scope
Rules for `FoodDiary.Application.Abstractions/`.

## Role
- Own application-facing contracts used by the primary FoodDiary application and its adapters.
- Keep interfaces and models close to their feature unless they are genuinely cross-cutting.
- Keep this project independent from hosts, presentation, infrastructure implementations, provider SDKs, and ASP.NET transport details.
- Users owns its Contracts and repository ports; Identity owns Authentication/email-template contracts; Admin owns role-audit reader contracts; Billing owns its marketing consumer port. Keep only references required by actual central facades/types, CurrentUserAccessResolver and shared IAdminSsoCodeStore here. Consumers reference owner contracts directly instead of relying on this assembly to re-export unused dependencies. See docs/ai/direct-contract-dependencies.md.

## Structure
- Feature-specific contracts should live under `Feature/Common/`, `Feature/Abstractions/`, or `Feature/Services/`.
- Shared cross-feature contracts live under `Common/Abstractions/...` or `Common/Interfaces/...`.
- Keep `Common/Interfaces/Persistence` intentionally small. Architecture tests currently allow only the cross-feature repositories already listed there.
- Keep namespaces aligned with folder paths.

## Rules

- BodyMetrics and Exercises own their error factories and direct callers. Do not recreate Errors.WeightEntry, Errors.WaistEntry, Errors.Exercise or central references to their owner Abstractions. Existing consumers reference the owners explicitly; see docs/ai/measurement-error-facades.md.

- All feature error facades are retired, including Billing and Lesson factories. Call owner factories directly and preserve codes/messages/kinds, including existing cross-module error choices. Only Authentication and Validation remain here because central parsers use them. Keep exactly the five actual shared/Users dependencies; see docs/ai/feature-error-retirement.md.
- Do not reference `FoodDiary.Web.Api`, `FoodDiary.Presentation.Api`, or `FoodDiary.Infrastructure`.
- Do not introduce ASP.NET types such as `HttpContext`, `IActionResult`, or `ControllerBase`.
- Do not bind configuration directly here with `IConfiguration` or `IOptions<T>`.
- Async interfaces should accept `CancellationToken`.
- Prefer narrow feature contracts over regrowing flat shared repository/service buckets.

## Commands
- Build: `dotnet build FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

Favorites ports/errors and persistence projections belong to Modules/Favorites/Application/Abstractions; public read services/models belong to Modules/Favorites/Contracts. Do not restore central Errors.Favorite* facades, FavoriteMeals/FavoriteProducts/FavoriteRecipes source folders or a direct Favorites Domain reference.
