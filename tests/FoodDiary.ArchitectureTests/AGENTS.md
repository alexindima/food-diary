# Architecture Test Guidelines

## Scope
Rules for `tests/FoodDiary.ArchitectureTests/`.

## Role
- Encode architectural decisions as fast guardrail tests.
- Treat these tests as a source of truth for dependency direction, feature structure, source conventions, and service boundaries.

## Current Guardrails
- `ProjectDependencyMatrixTests` owns the production project reference matrix. Add every new production `.csproj` there.
- `LayeringTests` protects primary FoodDiary layer direction.
- `MailRelayArchitectureTests` and `MailInboxArchitectureTests` protect service-specific layer direction and runtime database separation.
- `ApplicationGuardrailTests` protects application-layer conventions and prevents shared buckets from regrowing.
- `RequestFolderConventionTests` requires every application command and query slice to live in its own feature folder.
- `BusinessModuleBoundaryTests` protects governed vertical module ownership, EF configuration placement, and the explicit cross-module projection allowlist.
- `AsyncMethodGuardrailTests` uses Roslyn syntax parsing for async suffix and cancellation-token rules.
- `ClientPackageBoundaryTests` protects MailRelay/MailInbox client packages from server-side coupling.
- `HostCompositionBoundaryTests` protects host-only concerns from leaking into application/presentation/resource projects.
- `ContainerSupplyChainGuardrailTests` requires production images to carry provenance and SBOM metadata, resolve to image indexes, and be signed and verified before deployment.
- `ProjectFileConventionTests` keeps unconditional `ProjectReference` items in one `ItemGroup` and rejects empty `ItemGroup` elements.
- `SolutionModuleFolderTests` rejects empty solution-folder subtrees, duplicate folders/projects and missing project files; keeps module tests under `/Modules/<Owner>/tests/` and service tests under `/Services/<Owner>/Tests/`; and prevents redundant `Application/Core` and `Tests/Core` wrappers. Physical test directories alone do not provide solution grouping: each test project must be listed inside the corresponding solution folder. Folders with content in descendants (including solution files) and meaningful single-child groups remain valid.

## Rules

- `ModuleErrorOwnershipTests` keeps DailyAdvices, Dietologist, Fasting, Hydration and Meals error factories in their existing owner Abstractions. The central Errors facade depends one-way on them; do not restore Dietologist's obsolete central abstraction reference.

- `FavoritesContractOwnershipTests` protects the 20 owner-port/error/read-model files and six public read-service/projection files in separate Favorites projects, rejects central duplicates and aggregate/repository types in consumer contracts, and keeps Meals' source-reader dependency one-way.

- `OutboxReplayOwnershipTests` rejects concrete stream types/names in the common
  replay coordinator and saving/transactions in its module-owned stream adapters.
  Keep the same scoped context, explicit tie ordering and email replay prohibition.

- `UsersAdministrationReaderOwnershipTests` keeps both administrative read aliases
  and focused SQL tests in Users, while Users tracked lookup/Google/write ports
  and Users security-state reader retain their separate responsibilities.

- `UsersSecurityReaderOwnershipTests` protects the Users-owned security-state
  reader and focused provider tests, existing host composition, and separation
  from the Users repository aliases. No production graph edge changes.

- `UsersRepositoryOwnershipTests` requires the tracked repository and focused
  provider tests in Users, rejects the old central helper, and protects caller-owned
  SaveChanges/transactions. Existing reader guards preserve separate responsibilities.

- `IdentityAuthenticationOwnershipTests` protects module-owned JWT/password-hash
  implementations/tests, direct crypto package ownership and explicit registration
  in all three hosts; shared JwtOptions remain central. It also protects ordinary
  SSO service/test ownership and keeps the shared in-memory store registration central.

- `DietologistAuditOwnershipTests` protects module-owned collaboration audit rules
  and focused tests. Central persistence resolves EF's interceptor port, never the
  Dietologist implementation; behavior/order/lifetime and PostgreSQL rollback are
  tested at the module/shared integration boundaries.

- `ModuleOutboxOwnershipTests` protects Images/Gamification technical records and
  mappings in their PersistenceModel projects; the dependency matrix permits the
  shared Outbox.Abstractions edge, not a context-to-adapter dependency.

- `AdminModuleExtractionTests` distinguishes Admin's role-audit read projection
  from Users' role-audit entity ownership; require the module adapter and focused
  PostgreSQL tests, and prevent the old central helper from returning.

- `IdentityPersistenceOwnershipTests` requires the two extracted Identity adapters
  and their focused tests to remain with the module. Keep the login-event bulk
  deletion exception explicit after relocation; do not weaken its scope.
  Its Telegram cases also protect module-owned replay guard/model/configuration
  and the focused provider tests extracted from the mixed Dietologist class.
- Shared-library and development-tool test projects belong physically under `Shared/tests/` and `Tooling/tests/`, respectively, and in matching solution folders. `SolutionModuleFolderTests` also verifies their central test-settings imports; do not duplicate runner or build defaults.
- Prefer Roslyn-based checks for C# syntax over regex when inspecting declarations.
- Prefer `SourceScanner`, `ProjectReferenceReader`, `ArchitectureTestPaths`, and `CSharpSyntaxReader` over ad-hoc filesystem parsing.
- Keep failures actionable: output relative file paths and line numbers when possible.
- Do not add broad string bans without considering false positives in comments, generated code, or migrations.
- If an architecture boundary changes intentionally, update both the test and the relevant `AGENTS.md`.

## Commands
- Run: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
