# Repository Guidelines

## Scope

This file is the root aggregator. It defines cross-repo defaults and points to project-specific guides.
When working in a project folder, prefer that folder's `AGENTS.md` for concrete rules and commands.

## Project Guides

- Build-time analyzers: `FoodDiary.Analyzers/AGENTS.md`
- Application abstractions: `FoodDiary.Application.Abstractions/AGENTS.md`
- Admin application module: `FoodDiary.Application.Admin/AGENTS.md`
- Frontend app: `FoodDiary.Web.Client/AGENTS.md`
- Frontend admin app: `FoodDiary.Web.Client/projects/fooddiary-admin/AGENTS.md`
- UI kit: `FoodDiary.Web.Client/projects/fd-ui-kit/AGENTS.md`
- Tour engine: `FoodDiary.Web.Client/projects/fd-tour/AGENTS.md`
- Presentation adapter: `FoodDiary.Presentation.Api/AGENTS.md`
- API host/presentation: `FoodDiary.Web.Api/AGENTS.md`
- Application runtime: `FoodDiary.Application.Runtime/AGENTS.md`
- Billing application module: `FoodDiary.Application.Billing/AGENTS.md`
- Marketing application module: `FoodDiary.Application.Marketing/AGENTS.md`
- Notifications application module: `FoodDiary.Application.Notifications/AGENTS.md`
- Users application module: `FoodDiary.Application.Users/AGENTS.md`
- Content reports application module: `FoodDiary.Application.ContentReports/AGENTS.md`
- Gamification application module: `FoodDiary.Application.Gamification/AGENTS.md`
- Export application module: `FoodDiary.Application.Export/AGENTS.md`
- Weekly goals application module: `FoodDiary.Application.WeeklyGoals/AGENTS.md`
- USDA application module: `FoodDiary.Application.Usda/AGENTS.md`
- Weekly check-in application module: `FoodDiary.Application.WeeklyCheckIn/AGENTS.md`
- Daily advices application module: `FoodDiary.Application.DailyAdvices/AGENTS.md`
- Dashboard application module: `FoodDiary.Application.Dashboard/AGENTS.md`
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
- Place every application command and query slice in its own feature folder under `Commands/` or `Queries/`; do not put C# files directly in those folders.
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
- Always run `git commit` and `git push` with hooks enabled. Do not use `--no-verify`. If a hook fails, inspect the reported log under `.git/hook-logs/`, fix the cause, and rerun the original command.
- A running local API must not require bypassing hooks. The pre-push backend build uses `.artifacts/pre-push` so it does not overwrite assemblies held by the development server.
- Keep isolated .NET outputs under the repository-level `.artifacts/` by using `--artifacts-path`; never pass a relative `BaseOutputPath`, because MSBuild creates a separate `.artifacts` folder under every project. Pre-commit clears the root and accidental nested `.artifacts` folders before validation; pre-push removes accidental nested folders via `scripts/Clean-NestedDotnetArtifacts.ps1`.

## SSH Access

- In this repository, requests such as "подключись к серверу", "зайди на сервер", or "проверь на сервере" refer to the `fooddiary-prod` SSH alias.
- Connect with `ssh fooddiary-prod`; credentials and host details are managed by the user's local SSH configuration and must not be copied into the repository.
- Never use, modify, or reconfigure the `integration-01` SSH alias for this repository. It belongs to an unrelated work project.
- Treat server access as read-only unless the user explicitly requests a change or the requested operation clearly requires one. Before destructive or deployment-affecting actions, resolve the exact target and scope.

## Local Development

- In this repository, requests such as "перейди к локальной разработке", "запусти локально", or "открой локальное приложение" mean preparing and running the complete local application.
- Build the frontend with `npm run build` in `FoodDiary.Web.Client`, then run its development server with `npm start`.
- Run the backend with `dotnet run --project FoodDiary.Web.Api`.
- Verify that the frontend is available at `http://localhost:4200` and that it can communicate with the backend before reporting readiness.
- Local sign-in credentials are stored outside the repository in `%USERPROFILE%\.codex\secrets\food-diary.local.md`. Read that file only when authentication is required; never print, log, copy, or commit its contents.

## Production Grafana

- In this repository, requests such as "перейди в Grafana", "открой Grafana", or "проверь Grafana" refer to `https://grafana.fooddiary.club`.
- Grafana credentials are stored outside the repository in `%USERPROFILE%\.codex\secrets\food-diary.grafana-prod.md`. Read that file only when authentication is required; never print, log, copy, or commit its contents.
- Treat production Grafana access as read-only by default. Do not modify dashboards, alerts, data sources, users, organizations, API keys, service accounts, or other Grafana configuration unless the user explicitly requests that specific change.
- Never expose Grafana credentials in terminal output, screenshots, task summaries, documentation, or repository files.

## Documentation

- Long-form documentation lives under `docs/`.
- Start with `docs/README.md`, `docs/ARCHITECTURE.md`, `docs/BACKEND_MODULE_MAP.md`, and `docs/TESTING_STRATEGY.md` for broad context.
- For cross-cutting repository discovery, start at `.llm-wiki/index.md`, then verify relevant claims in its declared sources before changing code.
- Treat `.llm-wiki/` as compiled navigation, never as authority over code, tests, accepted ADRs, current `docs/`, or scoped `AGENTS.md`.
- Use `./.llm-wiki/wiki.ps1 diff` to discover change-set context and `./.llm-wiki/wiki.ps1 verify` before handing off wiki-affecting changes.
- Use `./.llm-wiki/wiki.ps1 brief` to compile risk, scoped instructions, affected modules, focused tests, and review obligations for a non-trivial change.
- Start a large or cross-layer feature with `./.llm-wiki/wiki.ps1 start -Intent <task>`; it captures the baseline, performs initial research, creates a scope-aware acceptance checklist, and initializes governed state when required. For ordinary bugs and bounded features use `develop`; add `-PlannedPath` whenever likely files are known. Follow the adaptive route rather than applying the full governed workflow to every change.
- During implementation, prefer `./.llm-wiki/wiki.ps1 next` for the single recommended action, `phase-next` for governed implementation phases, and `qa` for journey-derived manual scenarios. These facade commands derive from existing Wiki artifacts and do not replace detailed commands when diagnosis is needed.
- Use `research` before editing a non-trivial existing flow. It combines ranked code context, focused tests, known failures, and Git precedents; verify all inferred paths and historical patterns in current sources.
- Use `design` only when the adaptive route requires it or when research exposes a blocking product, compatibility, privacy, provider, persistence, or architecture decision.
- For governed work spanning sessions, use `pause` and `resume`; resume must report clean continuity or require a task refresh before edits continue.
- Use `journeys` for behavioral changes to identify affected FoodDiary end-to-end scenarios. In governed work, map applicable journey scenario IDs to acceptance criteria.
- For governed work, use `delivery-status` during implementation, `delivery-replan -Reason <evidence>` for intentional scope/plan divergence, and `delivery-validate -FailOnInvalid` before completion. Critical and architectural changes also require `delivery-critique -FailOnInvalid`.
- Use `./.llm-wiki/wiki.ps1 test-plan` to derive focused tests and risk scenarios before implementing or reviewing behavioral changes.
- Use `./.llm-wiki/wiki.ps1 decision` when project references, dependency injection, deployment, ownership, or module graph changes.
- Use `dependencies` for manifest changes and `rollout` for migrations, configuration, jobs, providers, or deployment-sensitive changes.
- Use `hotspots` and `test-gaps` to calibrate review depth; treat test references as navigation evidence, never as execution coverage.
- Use `topology` before changing external clients, webhooks, background workers, recurring jobs, or message delivery behavior.
- Use `privacy` before changing credentials, identity/health/financial data, private content, exports, logs, caches, queues, or provider sharing.
- For explicitly bounded autonomous work, use `task-init` and `task-validate` to detect accidental changes outside the declared path scope.
- Use `./.llm-wiki/wiki.ps1 trace -Query <command-or-query>` before changing an existing backend flow.
- Use `./.llm-wiki/wiki.ps1 ownership` for cross-module changes and `api-compat` after API snapshot changes.
- Search `./.llm-wiki/wiki.ps1 failures -Query <error>` before repeating diagnosis; record only verified, reusable resolutions.
- Use `docs/ai/CODE_REVIEW.md` for consistent AI-assisted review and resolve triggered change-policy obligations through an evidence bundle when the task warrants formal handoff.
- Product and feature plans live under `docs/plans/`; treat them as planning context unless referenced by current guides.
- Historical or stale documents should be removed once durable decisions are captured in current guides or ADRs. Git history is the repository history.

## EF Core Migrations

- Always commit both migration files: `*.cs` and `*.Designer.cs`.
- Add `[ExcludeFromCodeCoverage]` to migration implementation classes and model snapshots so generated EF code stays out of dotCover/code coverage.
- After editing or generating a migration, run a whitespace/style pass before commit. Prefer `dotnet format whitespace FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj` or an equivalent fix on the migration files so CI does not fail with `WHITESPACE: Fix whitespace formatting`.
