# Export module extraction: Wiki findings

## Scope and verified ownership

Export is a synchronous read-composer. Current source and tests prove ownership of diary/cycle export queries, validation, file-result models, CSV generation, and export-specific adapter contracts. It owns no Domain aggregate, EF mapping/repository, retained object, background job, or provider implementation. Meals and Cycles remain data owners and expose semantic read services; current-user access and sensitive-cycle password verification remain Users capabilities. PDF rendering, remote image policy, report resources, HTTP transport, and host composition remain central seams.

## Useful Wiki evidence

- `start` selected the critical governed workflow and found the Cycles and ContentReports extraction precedents.
- `research` correctly ranked the legacy Export abstraction files and required an explicit compatibility/privacy boundary before implementation.
- Focused `privacy` identified the password credential, diary calories/satiety/comments, and PDF health-data path.
- `topology` found the central `IDiaryPdfGenerator` HTTP client seam and its redirect/proxy/DNS/timeout controls, which prevented incorrectly moving provider networking into Export.
- `test-plan` found the application, architecture, and presentation test surfaces and explicitly called out cancellation coverage.

## False positives and low-signal output

- Unscoped `privacy` reported repository-wide authentication tokens and billing customer IDs unrelated to Export.
- Unscoped `topology` emitted all compose services, mail webhooks, hosted services, recurring jobs, and network policies even though Export has no background or storage lifecycle.
- `brief`, `test-plan`, `decision`, and `ownership` returned empty or low-risk output when invoked without repeating intent/planned paths, despite an active governed task.
- `design` continued to show an unresolved generic boundary question after the intent explicitly recorded compatibility, privacy, provider, persistence, and architecture constraints.

## False negatives

- `ownership` found the donor guide but did not enumerate proven downstream consumers: Presentation, Web API, Initializer, JobManager, Infrastructure PDF adapter, Resources, and their tests.
- Default `privacy` did not surface cycle notes, symptoms, bleeding/fertility fields, meal composition, user scoping, or the sensitive-export password re-verification flow; these were found through source/tests.
- `journeys` returned no matches for existing diary/cycle download routes.
- `rollout` produced generic deployment guidance and did not identify the Docker project-copy seams or NuGet lockfile cascade.

## Reproducible Wiki defects

1. Active task context is not consistently reused by facade commands; omitting repeated `-Intent`/`-PlannedPath` yields empty results.
2. The JSON-baseline ownership graph does not expand project references, DI registration, HTTP consumers, or adapter implementations for a module query.
3. Focused privacy matching is primarily declaration-name based and misses sensitive fields reached through cross-module projections and serialization/generation paths.
4. Topology scoping is too broad for a module-only change and mixes relevant provider seams with unrelated repository-wide infrastructure.
5. Journey discovery has no Export route coverage despite explicit controller routes and focused controller tests.

These are general discovery/scoping defects; no Export-specific generator special case was added.
