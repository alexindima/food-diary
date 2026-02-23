# Repository Guidelines

## Scope
This file is the root aggregator. It defines cross-repo defaults and points to project-specific guides.
When working in a project folder, prefer that folder's `AGENTS.md` for concrete rules and commands.

## Project Guides
- Frontend app: `FoodDiary.Web.Client/AGENTS.md`
- UI kit: `FoodDiary.Web.Client/projects/fd-ui-kit/AGENTS.md`
- API host/presentation: `FoodDiary.Web.Api/AGENTS.md`
- Application layer: `FoodDiary.Application/AGENTS.md`
- Domain layer: `FoodDiary.Domain/AGENTS.md`
- Contracts: `FoodDiary.Contracts/AGENTS.md`
- Infrastructure layer: `FoodDiary.Infrastructure/AGENTS.md`
- Job manager: `FoodDiary.JobManager/AGENTS.md`
- Telegram bot: `FoodDiary.Telegram.Bot/AGENTS.md`

## Cross-Repo Rules
- Keep architecture feature-first and move legacy flat areas incrementally.
- Keep .NET shared build settings in root `Directory.Build.props`.
- Keep nullable enabled in C# projects and align namespaces with folders.
- Use K&R brace style for C# code (opening brace on the same line).
- Prefer C# primary constructors where applicable.
- For UI text changes, update both locales:
  - `FoodDiary.Web.Client/assets/i18n/en.json`
  - `FoodDiary.Web.Client/assets/i18n/ru.json`
- Verify Russian text rendering after edits (no mojibake / replacement symbols).

## Build Baseline
- `dotnet build FoodDiary.sln`
- `cd FoodDiary.Web.Client && npm run build`

## EF Core Migrations
- Always commit both migration files: `*.cs` and `*.Designer.cs`.
