# Statistics Module Extraction Wiki Report

## Scope and verified boundary

Statistics owns two mediator query slices (`GetStatistics` and `GetStatisticsSummary`), their validators and response models, and UTC/date normalization. The handlers compose these existing read capabilities:

- `IDashboardStatisticsReadService` for nutrition buckets;
- `IWeightEntryReadService` and `IWaistEntryReadService` for body-measurement summaries;
- `ICurrentUserAccessService` for access checks.

Code, project references, EF registrations, and tests show no Statistics-owned aggregate, value object, domain event, repository port, EF configuration, provider adapter, job, migration, or model-snapshot entry. The proven target is therefore application-only: `Modules/Statistics/Application` plus its focused application tests. Dashboard and Body Metrics retain their projection contracts and implementations; central Infrastructure retains the optimized dashboard query, shared `FoodDiaryDbContext`, migrations, and snapshot. Presentation and hosts remain adapters/composition roots.

## Useful Wiki evidence

- `start` classified the change as architectural and created a governed workspace with the correct primary acceptance criteria: module application placement, declared dependencies, host registration, focused tests, and removal of the donor path.
- `research` found the exact earlier Statistics extraction precedent (`61ee449368`) and the TDEE application-only extraction precedent (`8249e9f97b`). Both were useful navigation anchors and were verified against current source.
- Explicit `brief` and `test-plan` calls found the two focused Statistics test files plus central architecture and presentation consumers.
- `privacy` correctly identified nutrition, weight, and waist fields as health data. Source inspection confirmed that the relocation changes no storage, logging, authorization, sharing, retention, or provider boundary.
- `dependencies` correctly detected the physical project/package declarations moving from the donor path and the new focused test project.
- `update` correctly regenerated the repository catalog, module page, contract/symbol/quality indexes, runtime topology, and sensitive-data index with the new project and test paths.

## False positives

- `rollout` inferred changed background-job semantics and public API behavior from JobManager/Docker/presentation consumers. The diff changes only project and Docker COPY paths. `AddStatisticsModule`, CLR namespaces, assembly name, mediator requests, HTTP routes/payloads/status codes, and Swagger-visible types are unchanged.
- The affected-update summary required API snapshot review solely because a presentation project reference changed. No presentation source, route, request/response type, status mapping, or OpenAPI snapshot changed.
- `dependencies` reports package removal and addition with empty versions for a pure project relocation. This is structurally true but reads like a dependency version change; the central package declarations and resolved versions did not change.
- `privacy` included unrelated Cycles fields when given a scope containing shared abstractions and presentation. Those candidates are outside the Statistics change.

## False negatives

- `brief`, `test-plan`, and `ownership` returned empty/zero scope when called without repeating `-Intent` and `-PlannedPath`, even immediately after `start` created `tasks/current`. Repeating explicit arguments recovered `brief` and `test-plan`; `ownership` still returned no direct/downstream modules despite verified Statistics-to-Dashboard/BodyMetrics seams.
- `research` reported `relevant-workspace-paths=0` during its cold-cache scan even though explicit planned paths existed and its final packet later listed six grounded paths.
- `design` remained blocked on a generic “select and record the boundary” question after the intent already stated the compatibility and architecture boundary. It did not surface a concrete command for recording the source-proven choice.
- The Wiki test plan omitted the new standalone module test project because it reasoned from the pre-change solution. Direct source/test inventory was required to plan that project and its lockfile.

## Reproducible Wiki defects and improvement candidates

1. Facade commands should inherit the active `tasks/current` intent and planned paths by default. Today an omitted argument silently degrades `brief`, `test-plan`, and `ownership` to empty output.
2. `ownership` should map project references and contract consumers for application-only modules even when there is no Domain/Infrastructure project. Statistics has a clear read-composition boundary that the command missed.
3. `rollout` should distinguish a project-path/Docker COPY relocation from behavioral changes in jobs or HTTP contracts. Assembly/namespace preservation and unchanged source hashes are strong suppression signals.
4. Dependency reporting should label unchanged central-package versions as “relocated declaration” rather than separate removed/added dependencies.
5. Cold-cache progress should not emit `relevant-workspace-paths=0` when explicit planned paths are present; this contradicts the final grounded-path count.
6. Design checkpoints need a documented non-interactive way to record an evidence-backed boundary decision for autonomous tasks.
7. A workspace created by `start` under the advertised JSON fallback cannot be refreshed after implementation: `task-refresh` requests a missing SQLite projection even when `acceptance-matrix.json` records `compiledIndexSource: Json`. Supplying explicit changed paths does not avoid the SQLite requirement, so already executed checks cannot be imported into governed delivery evidence and `delivery-validate` remains mechanically blocked.

These are general workflow defects; no generator special-case for Statistics is warranted.
