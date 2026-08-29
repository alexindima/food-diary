# ADR 0016: Logical Module Folders And Incremental Fasting Extraction

- Status: Accepted
- Date: 2026-08-29
- Owners: Backend architecture
- Related: ADR 0001, ADR 0004, ADR 0006, ADR 0009, ADR 0015
- Supersedes: None

## Context

FoodDiary's horizontal projects make layer direction clear, but a business module is spread across several repository roots. That makes ownership harder to discover, permits consumers to depend on implementation assemblies when they need only a stable read contract, and causes Wiki tooling to confuse project names with business-module identities.

ADR 0006 selected Fasting as the pilot for executable business ownership. The extraction needs to improve physical cohesion without moving the shared EF migration host, changing the database, or changing HTTP contracts.

## Decision Drivers

- A business module needs one canonical identity independent of its project count.
- Stable cross-module contracts should not require an implementation reference.
- Paths, namespaces, architecture tests, container builds, and Wiki discovery must agree.
- Extraction must preserve runtime behavior, HTTP contracts, database schema, and deployment topology.
- The pattern must support incremental adoption by other modules.

## Considered Options

1. Keep horizontal project roots and improve documentation only.
2. Move all Fasting layers, persistence, migrations, and presentation in one change.
3. Introduce a logical module folder, extract Application and Contracts first, then extract Domain and Infrastructure through separate acyclic projects while retaining the shared migration host.

## Decision

Adopt option 3.

- `Fasting` is the canonical logical module identifier.
- Its new root is `Modules/Fasting`.
- `Modules/Fasting/FoodDiary.Modules.Fasting.csproj` owns application implementation under `Application/`.
- `Modules/Fasting/Application/Abstractions` owns repository ports and internal persistence projections.
- `Modules/Fasting/Contracts/FoodDiary.Modules.Fasting.Contracts.csproj` owns stable read DTOs/read services and operational job contracts.
- `Modules/Fasting/Domain` owns Fasting aggregates, enums, and identifiers. Existing CLR namespaces remain stable in this tranche so EF model identity does not change.
- `Modules/Fasting/Infrastructure/Model` owns EF configurations and exposes the model-builder registration seam used by the shared context.
- `Modules/Fasting/Infrastructure` owns repositories and the complete `AddFastingModule` composition facade.
- The application project temporarily retains the legacy assembly name `FoodDiary.Application.Fasting`; the assembly name is a compatibility detail, not the module identity.
- Implementation namespaces use `FoodDiary.Modules.Fasting.Application.*`; contract namespaces use `FoodDiary.Modules.Fasting.Contracts.*`.
- The shared `FoodDiaryDbContext`, migration history, and model snapshot remain in `FoodDiary.Infrastructure`; HTTP transport remains in `FoodDiary.Presentation.Api`.
- Central Infrastructure references only Fasting Domain and persistence-model projects. Fasting Infrastructure references central Infrastructure for the shared context; central Infrastructure never references Fasting Infrastructure, so no project cycle is introduced.
- Composition roots reference Fasting Infrastructure to call `AddFastingModule`. Other modules consume Contracts unless a reviewed implementation dependency is necessary.
- The backend module manifest and Wiki generators resolve the logical module across all declared source mappings instead of assuming one project equals one module.
- HTTP routes, payloads, status codes, serialization, database schema, and deployment topology do not change in this tranche.

## Consequences

### Positive

- Fasting implementation and stable contracts become discoverable under one root.
- Dashboard and jobs can compile against a smaller, explicit contract surface.
- Architecture tests can distinguish logical identity, physical projects, and source roots.
- Wiki ownership remains stable while files move between projects.
- The change provides a repeatable migration shape for later modules.

### Negative

- The shared migration host and transport remain horizontal by design, so the module is vertically cohesive without duplicating database or HTTP composition concerns.
- Domain and application-abstraction namespaces intentionally remain compatible during physical extraction.
- Composition-root projects still reference the implementation assembly.
- Structural moves create broad path and namespace churn and require generated Wiki refreshes.

## Enforcement

- `docs/architecture/backend-modules.json` declares the canonical logical root and mapped projects.
- `tests/FoodDiary.ArchitectureTests/FastingModuleExtractionTests.cs` verifies physical projects and approved references.
- `tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs` governs all new project edges.
- `tests/FoodDiary.ArchitectureTests/BusinessModuleBoundaryTests.cs` retains semantic ownership rules.
- `.llm-wiki/tools/Test-LlmWikiBackendModuleModel.ps1` and `.llm-wiki/tools/Test-LlmWikiIntentOwnership.ps1` protect logical-module discovery.

## Follow-up

- Measure the pilot before migrating another module.
- Evaluate the Fasting project split and guardrails before choosing the next module.
- Rename preserved compatibility namespaces only as a separately governed change with explicit EF model/snapshot evidence.
- Prefer a small composition bootstrapper later if hosts need registration without referencing implementation from job code.
