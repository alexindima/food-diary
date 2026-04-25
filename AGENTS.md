# Repository Guidelines

## Scope
This file is the root aggregator. It defines cross-repo defaults and points to project-specific guides.
When working in a project folder, prefer that folder's `AGENTS.md` for concrete rules and commands.

## Project Guides
- Frontend app: `FoodDiary.Web.Client/AGENTS.md`
- UI kit: `FoodDiary.Web.Client/projects/fd-ui-kit/AGENTS.md`
- Presentation adapter: `FoodDiary.Presentation.Api/AGENTS.md`
- API host/presentation: `FoodDiary.Web.Api/AGENTS.md`
- Application layer: `FoodDiary.Application/AGENTS.md`
- Domain layer: `FoodDiary.Domain/AGENTS.md`
- Contracts: `FoodDiary.Contracts/AGENTS.md`
- Infrastructure layer: `FoodDiary.Infrastructure/AGENTS.md`
- Job manager: `FoodDiary.JobManager/AGENTS.md`
- Mail relay application layer: `FoodDiary.MailRelay.Application/AGENTS.md`
- Mail relay client package: `FoodDiary.MailRelay.Client/AGENTS.md`
- Mail relay domain layer: `FoodDiary.MailRelay.Domain/AGENTS.md`
- Mail relay infrastructure layer: `FoodDiary.MailRelay.Infrastructure/AGENTS.md`
- Mail relay initializer: `FoodDiary.MailRelay.Initializer/AGENTS.md`
- Mail relay presentation layer: `FoodDiary.MailRelay.Presentation/AGENTS.md`
- Mail relay Web API host: `FoodDiary.MailRelay.WebApi/AGENTS.md`
- Telegram bot: `FoodDiary.Telegram.Bot/AGENTS.md`

## Cross-Repo Rules
- Keep architecture feature-first and move legacy flat areas incrementally.
- Keep .NET shared build settings in root `Directory.Build.props`.
- Keep nullable enabled in C# projects and align namespaces with folders.
- Use K&R brace style for C# code (opening brace on the same line).
- Prefer C# primary constructors where applicable.
- If backend HTTP routes, payloads, status codes, or Swagger-visible API surface change, update the relevant contract snapshots under `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/` and commit them with the feature.
- For UI text changes, update both locales:
  - `FoodDiary.Web.Client/assets/i18n/en.json`
  - `FoodDiary.Web.Client/assets/i18n/ru.json`
- Verify Russian text rendering after edits (no mojibake / replacement symbols).

## Build Baseline
- `dotnet build FoodDiary.slnx`
- `cd FoodDiary.Web.Client && npm run build`

## EF Core Migrations
- Always commit both migration files: `*.cs` and `*.Designer.cs`.
- After editing or generating a migration, run a whitespace/style pass before commit. Prefer `dotnet format whitespace FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj` or an equivalent fix on the migration files so CI does not fail with `WHITESPACE: Fix whitespace formatting`.
