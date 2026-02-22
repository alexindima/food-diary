# Web API (Presentation) Guidelines

## Scope
Rules for `FoodDiary.Web.Api/`.

## Architecture
- Treat this project as presentation + composition root.
- Keep controllers/endpoints feature-first under `Features/<FeatureName>/`.
- Keep thin controllers delegating to MediatR.
- Keep startup/composition in extensions where practical.

## Structure
- Features: `Features/`
- Program entrypoint: `Program.cs`
- Composition helpers: `Extensions/`

## Commands
- Build: `dotnet build FoodDiary.Web.Api/FoodDiary.Web.Api.csproj`
- Run: `dotnet run --project FoodDiary.Web.Api`

## API Practices
- Explicit DTO-to-domain mapping.
- Prefer structured results (`Result`/`OneOf`) over ad-hoc status branching.
- Apply validation via MediatR pipeline + FluentValidation.
- Keep cross-cutting concerns in middleware/pipeline behaviors.

## Migration Guidance
- For legacy flat controller areas, migrate feature-by-feature.
- Keep namespaces aligned with folders.
