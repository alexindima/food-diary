# Full persistence composition

## Scope and ownership

FoodDiary.Infrastructure owns FoodDiaryDbContext, complete EF model composition,
cross-module relationships, design-time factory, migration history and snapshot.
Its AddInfrastructure registration composes AddPersistenceRuntime and lazily
registers the full context through IModuleContextFactory. ICompositionReadContext
aliases that same scoped context; composed reads must not gain write capabilities.

Generic context/session/save/transaction, audit, email-outbox and replay engines
belong to Shared/FoodDiary.Persistence.Runtime (ADR 0043). Shared outbox claiming
and processing belong to Shared/FoodDiary.Outbox.Infrastructure. Module adapters
use their owner context and narrow persistence coordination contracts.

ADR 0044 removes authentication and application option binding from this project.
Hosts explicitly register Shared authentication infrastructure and Identity email
options. Identity, AI and Products register their own memory cache prerequisites.
Do not reintroduce token services, SSO storage, structured log adapters, email
configuration or external providers here.

## Dependency rules

- Respect the exact project dependency matrix. This assembly may compose module
  Domain/PersistenceModel types and approved narrow contracts, but never module
  Infrastructure, Application implementations, presentation or host assemblies.
- Keep owner mappings in PersistenceModel projects and apply them explicitly.
  Only cross-module relationships belong to Persistence/Composition here.
- Preserve migration identity and the full context CLR identity. Module ownership
  changes must not silently change database schema or generated snapshots.
- Keep domain rules in owner aggregates. Mixed read SQL belongs to
  FoodDiary.ReadModel.Composition and consumes ICompositionReadContext.
- Do not add binary compatibility wrappers for types moved to owner assemblies.

## Runtime invariants

Preserve ADR 0042 shared connection, scoped participant identity, save order,
execution strategy, intermediate saves, late transaction enlistment, rollback,
tracker reset, cancellation and post-commit ordering. The full context joins the
runtime session lazily at save order 1. Users saves at -100, shared root at 0,
other owners at their declared order. Owner saves and independent transactions
remain subject to the architecture-test allowlist.

Register owner ISaveChangesInterceptor instances in the runtime after telemetry
and domain-event interceptors. Do not reference owner interceptor implementation
types here or copy shared save interceptors into every module context. Users owns
its explicit Telegram conflict interceptor registration. Audit/replay writes must
remain atomic with owner writes through the shared unit of work.

## Migrations and validation

- Keep migration implementation, matching Designer file and model snapshot together.
- Mark generated migration implementations and snapshots ExcludeFromCodeCoverage.
- Use nullable C#, folder-aligned namespaces, primary constructors and K&R braces.
- Format migration whitespace after generation; remove unnecessary System imports
  and UTF-8 BOM. Keep LF line endings.
- Build: dotnet build FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj
- Unit tests: dotnet test tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj
- Run focused PostgreSQL integration tests for transaction or mapping behavior;
  skipped Docker tests do not constitute verification.

See docs/backend/BACKEND_MIGRATION_SAFETY.md and ADRs 0042, 0043 and 0044.
