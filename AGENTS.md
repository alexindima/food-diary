# Repository Guidelines

## Scope
This file is the root aggregator. It defines cross-repo defaults and points to project-specific guides.
When working in a project folder, prefer that folder's `AGENTS.md` for concrete rules and commands.

## Project Guides
- Application abstractions: `FoodDiary.Application.Abstractions/AGENTS.md`
- Frontend app: `FoodDiary.Web.Client/AGENTS.md`
- Frontend admin app: `FoodDiary.Web.Client/projects/fooddiary-admin/AGENTS.md`
- UI kit: `FoodDiary.Web.Client/projects/fd-ui-kit/AGENTS.md`
- Tour engine: `FoodDiary.Web.Client/projects/fd-tour/AGENTS.md`
- Presentation adapter: `FoodDiary.Presentation.Api/AGENTS.md`
- API host/presentation: `FoodDiary.Web.Api/AGENTS.md`
- Application layer: `FoodDiary.Application/AGENTS.md`
- Domain layer: `FoodDiary.Domain/AGENTS.md`
- Infrastructure layer: `FoodDiary.Infrastructure/AGENTS.md`
- Integrations layer: `FoodDiary.Integrations/AGENTS.md`
- Initializer: `FoodDiary.Initializer/AGENTS.md`
- Job manager: `FoodDiary.JobManager/AGENTS.md`
- Resources/localization/report text: `FoodDiary.Resources/AGENTS.md`
- Shared mediator: `Shared/FoodDiary.Mediator/AGENTS.md`
- Shared domain primitives: `Shared/FoodDiary.Domain.Primitives/AGENTS.md`
- Tests: `tests/AGENTS.md`
- Architecture tests: `tests/FoodDiary.ArchitectureTests/AGENTS.md`
- Mail inbox application layer: `MailInbox/FoodDiary.MailInbox.Application/AGENTS.md`
- Mail inbox client package: `MailInbox/FoodDiary.MailInbox.Client/AGENTS.md`
- Mail inbox domain layer: `MailInbox/FoodDiary.MailInbox.Domain/AGENTS.md`
- Mail inbox infrastructure layer: `MailInbox/FoodDiary.MailInbox.Infrastructure/AGENTS.md`
- Mail inbox initializer: `MailInbox/FoodDiary.MailInbox.Initializer/AGENTS.md`
- Mail inbox presentation layer: `MailInbox/FoodDiary.MailInbox.Presentation/AGENTS.md`
- Mail inbox Web API host: `MailInbox/FoodDiary.MailInbox.WebApi/AGENTS.md`
- Mail relay application layer: `MailRelay/FoodDiary.MailRelay.Application/AGENTS.md`
- Mail relay client package: `MailRelay/FoodDiary.MailRelay.Client/AGENTS.md`
- Mail relay domain layer: `MailRelay/FoodDiary.MailRelay.Domain/AGENTS.md`
- Mail relay infrastructure layer: `MailRelay/FoodDiary.MailRelay.Infrastructure/AGENTS.md`
- Mail relay initializer: `MailRelay/FoodDiary.MailRelay.Initializer/AGENTS.md`
- Mail relay presentation layer: `MailRelay/FoodDiary.MailRelay.Presentation/AGENTS.md`
- Mail relay Web API host: `MailRelay/FoodDiary.MailRelay.WebApi/AGENTS.md`
- Telegram bot: `FoodDiary.Telegram.Bot/AGENTS.md`
- Shared result primitives: `Shared/FoodDiary.Results/AGENTS.md`

## Cross-Repo Rules
- Keep architecture feature-first and move legacy flat areas incrementally.
- Keep .NET shared build settings in root `Directory.Build.props`.
- Keep nullable enabled in C# projects and align namespaces with folders.
- Use K&R brace style for C# code (opening brace on the same line).
- Prefer C# primary constructors where applicable.
- Respect the dependency matrix enforced in `tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs`.
- Primary FoodDiary core projects may interact with MailRelay/MailInbox only through approved client packages. Today that cross-service access belongs in `FoodDiary.Integrations`.
- Keep executable hosts as composition roots. Put HTTP transport in presentation projects, use cases in application projects, persistence/provider implementations in infrastructure projects, and domain rules in domain projects.
- Async backend methods should use the `Async` suffix and accept `CancellationToken` unless they are framework entrypoints covered by architecture-test exceptions.
- If backend HTTP routes, payloads, status codes, or Swagger-visible API surface change, update the relevant contract snapshots under `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/` and commit them with the feature.
- For UI text changes, update both locales:
  - `FoodDiary.Web.Client/assets/i18n/en/*.json`
  - `FoodDiary.Web.Client/assets/i18n/ru/*.json`
- Verify Russian text rendering after edits (no mojibake / replacement symbols).

## Build Baseline
- `dotnet build FoodDiary.slnx`
- `cd FoodDiary.Web.Client && npm run build`
- Focused architecture guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
- Backend coverage: `dotnet test FoodDiary.slnx --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory .\TestResults\coverage-backend`
- Frontend full verification: `cd FoodDiary.Web.Client && npm run verify`
- Commits can take a while because the pre-commit hook runs formatting, linters, and tests. If `git commit` appears to time out, check `git status` and `git log -1` before retrying; the commit may still finish successfully after the command wrapper stops waiting.
- Pushes can take a long time because the pre-push hook runs the full frontend and backend test suites. If `git push` appears to time out, check `git status`, `git log -1`, and the remote branch state before retrying.

## Documentation
- Long-form documentation lives under `docs/`.
- Start with `docs/README.md`, `docs/ARCHITECTURE.md`, `docs/BACKEND_MODULE_MAP.md`, and `docs/TESTING_STRATEGY.md` for broad context.
- Product and feature plans live under `docs/plans/`; treat them as planning context unless referenced by current guides.
- Historical or stale documents should be removed once durable decisions are captured in current guides or ADRs. Git history is the repository history.

## EF Core Migrations
- Always commit both migration files: `*.cs` and `*.Designer.cs`.
- Add `[ExcludeFromCodeCoverage]` to migration implementation classes and model snapshots so generated EF code stays out of dotCover/code coverage.
- After editing or generating a migration, run a whitespace/style pass before commit. Prefer `dotnet format whitespace FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj` or an equivalent fix on the migration files so CI does not fail with `WHITESPACE: Fix whitespace formatting`.
