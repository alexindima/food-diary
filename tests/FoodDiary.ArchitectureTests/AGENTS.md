# Architecture Test Guidelines

`DirectOwnerContractReferenceTests` prevents nine unused contract exports from returning to central Application.Abstractions and checks representative direct consumers. The exact dependency matrix protects every production/test ProjectReference; emitted assembly metadata audits complement, but do not replace, compile-time verification.

## Scope
Rules for `tests/FoodDiary.ArchitectureTests/`.

## Role
- Encode architectural decisions as fast guardrail tests.
- Treat these tests as a source of truth for dependency direction, feature structure, source conventions, and service boundaries.

## Current Guardrails
- `BuildWorkflowGuardrailTests` requires a complete, disjoint backend CI project partition, fast-before-slow ordering, bounded slow-group parallelism, and a final gate over all backend groups. `ContainerSupplyChainGuardrailTests` checks locked restore for both the full solution and generated group solutions.
- Wiki CI guardrails retain independent Focused/Full workers, the stable required aggregate gate, explicit PR-only audit skipping, and all compatibility/reporting checks. Gate outcome contracts cover success, failure, cancellation, and skip combinations.
- ProviderAdapterOwnershipTests protects moved provider sources, one-way shared helper dependencies and explicit API/JobManager composition without adding providers to Initializer. Identity's Google/Telegram providers need no Integrations dependency; Images shares only existing URI/telemetry helpers. ExternalHttpClientGuardrailTests scans all six module provider roots; relocation must not remove response-bound/cancellation guard coverage.
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
- `DockerfileDependencyTests` checks explicit project/source copies and shared compiler `AdditionalFiles` from `Directory.Build.props` before publish. Docker builds must include the reviewed persistence manifest; separate restore/build stages keep the modular project graph below filesystem overlay mount limits without broadening source copies.
- `ProjectFileConventionTests` keeps unconditional `ProjectReference` items in one `ItemGroup`, rejects empty `ItemGroup` elements, and requires `PackageReference`, `ProjectReference`, and `FrameworkReference` items to use separate groups, including conditional groups. `PropertyGroup` and `ItemGroup` use multiline layout with two-space indentation and one blank line between adjacent groups.
- `SolutionModuleFolderTests` rejects empty solution-folder subtrees, duplicate folders/projects and missing project files; keeps module tests under `/Modules/<Owner>/tests/` and service tests under `/Services/<Owner>/Tests/`; and prevents redundant `Application/Core` and `Tests/Core` wrappers. Physical test directories alone do not provide solution grouping: each test project must be listed inside the corresponding solution folder. Folders with content in descendants (including solution files) and meaningful single-child groups remain valid.

## Rules

- RetiredErrorFacadeTests also rejects WeightEntry/WaistEntry/Exercise central facades and BodyMetrics/Exercises owner exports. Exact contract and invariant date tests live in the existing owner application suites.

- UsersIdentityContractOwnershipTests separates Users semantic contracts from
  aggregate repository ports, protects Identity's one-way dependency on Users
  Contracts, and retains only the explicit central resolver/SSO seams. Central
  contract features may use Common or Abstractions purpose folders; source-file
  placement remains independently enforced.

- `ModuleErrorOwnershipTests` keeps DailyAdvices, Dietologist, Fasting, Hydration and Meals error factories in their existing owner Abstractions. `RetiredErrorFacadeTests` rejects central DailyAdvice/Fasting/HydrationEntry declarations and owner exports; their exact error contract tests live in their modules. All feature facades, including Dietologist/Meals/Billing/Lesson, are retired. Positive owner guards and the exact five-reference central matrix prevent regression; literal contract tests live in owner suites. Do not restore reverse central references.

- `FavoritesContractOwnershipTests` protects the explicit owner-port/error/source-model files and public read-service/projection files in separate Favorites projects, rejects central duplicates and aggregate/repository types in consumer contracts, and keeps Meals' source-reader dependency one-way.

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

ModuleAggregateIsolationTests rejects foreign aggregate fields and EF navigations,
verifies the legacy Images ID-only contract exception, and checks that the relational
model needs no migration. ModuleDependencyGraphTests requires zero combined API/
service-contract cycles. PersistenceCapabilityTests reconciles the compiler exception
file with the reviewed inventory; a passing analyzer alone does not review raw SQL.

Images.Service.Contracts is the aggregate-free Id/Url access API; the combined graph includes Service.Contracts edges. Scalar mappings remain migration-aligned; ADR 0032 intentionally changes the six image FKs to Restrict.

RuntimeModuleBoundaryTests reconciles consumer-owned interfaces with foreign implementation owners and conservative parameter-type consumers in runtime-module-boundaries.json. The transaction field is reviewed policy; provider tests establish actual transaction behavior. This inventory complements, and does not weaken, the acyclic Application reference graph.

AI persistence boundary pilot: AI-owned entity configurations remain in its PersistenceModel; only its four foreign User/ImageAsset relationships are composed centrally by AiCrossModuleRelationships after module registrations. AiModuleExtractionTests rejects foreign Domain assembly references in the compiled AI model. The exact project matrix and ModuleAggregateIsolationTests protect direct references and unchanged relational schema. See docs/ai/ai-persistence-boundary.md.

Use `./scripts/Format-ProjectFiles.ps1` to format project groups and sort references (own module, Shared, other projects; ordinal paths). `-Check` is read-only and exits nonzero on drift; `-Path` accepts selected projects. `ProjectFiles_MatchAutomaticFormatter` runs this check, requiring PowerShell 7 and Git. Conditional items, expressions, comments, duplicate includes, and Update/Remove entries delimit sorting runs; metadata stays attached.
