# USDA, BodyMetrics and Notifications integration

## Source and compatibility

Integrated into local master on top of coverage commit `c421af4f3eca13de3c36fe80170219176aaaf8f6`:

- USDA: `c711c02253076097f7b5553bceb23bf307d094dc`.
- BodyMetrics: `0576e634557cf18248e517a135ff636a5b89f4e0`.
- Notifications: `9796332e724ca0d8b5f2f15eccdc64a50f1f814b`.

Shared project references, Docker COPY paths, host registrations, EF model registrations,
solution entries and dependency guards were reconciled together. NuGet locks and Wiki
indexes were regenerated from the combined graph, not selected from a single branch.
The production sources and tests added for coverage baseline 115 are unchanged.
There is no push or deployment in this integration.

USDA retains its Product-linked central Domain types; BodyMetrics retains User-linked
measurement entities and User-owned goals. Notifications owns its domain, ports,
persistence model and web-push implementation. Shared Outbox.Abstractions contains only
the unchanged IOutboxMessage contract. The generic multi-stream engine, claim/replay
implementation, migrations, model snapshot and HTTP snapshots are unchanged.

## Integration fixes

The central split-repository DI fixture now explicitly registers Billing, OpenFoodFacts,
Wearables and USDA alongside the other extracted modules. This repairs the six failures
independently confirmed on the old master, and keeps the relocated USDA repository in
the same scoped-alias check. Production DI behavior was not altered by this test fix.

The module-page generator now resolves both legacy abstraction-area names and explicit
repository-relative paths. Previously it prefixed `Modules/...` with the legacy central
abstractions directory, silently reporting zero public contracts. BodyMetrics now reports
18 public contract files/types and Billing 28. The backend-module regression tests cover
legacy/nested areas, explicit and Windows-style paths, and independently count public
files for module-relative manifest mappings. The existing isolation check also recognizes
the already-used logical-module/module-root manifest classifications.

## Verification of the combined tree

Ordinary tests only; no dotCover, XPlat or other coverage collector was run.
Use repository-level output root `.artifacts/module-integration-20260831`; actual VSTest
TRX files are retained in `.artifacts/module-integration-20260831-results`.

| Suite | Passed |
| --- | ---: |
| USDA Application | 27 |
| BodyMetrics Application | 97 |
| Notifications Application / Domain / Infrastructure | 111 / 22 / 76 |
| Central Application / Domain / Infrastructure | 1362 / 1046 / 764 |
| JobManager / Presentation / Web.Api unit | 168 / 825 / 247 |
| Architecture, after catalog regeneration | 751 |
| PostgreSQL outbox/USDA/tracking-focused integration | 8 |
| HTTP Notifications/Swagger/measurement/USDA-focused integration | 37 |
| Total, without counting the earlier architecture attempt | 5541 |

All listed final runs have zero failures and zero skips. The initial architecture run
failed only the stale repository-catalog assertion; regenerating the combined catalog
and rerunning the whole architecture suite resolved it. Full solution restore and build
succeeded with zero warnings/errors. EF's central design-time factory reported no pending
model changes; the existing 10.0.10 tool / 10.0.11 runtime version warning remains.
The module-page regression also passed with PowerShell StrictMode Latest.

These checks are not a claim that the entire backend/frontend regression or coverage
measurement ran. Existing master build outputs were preserved during cleanup; only
recorded build outputs in completed worktree scopes were cleaned with dotnet clean.
TRX, committed handoffs and Wiki caches were retained.

## Wiki evaluation and remaining limitations

The combined index update and source checks are useful for catching missing solution
projects and source moves. The newly fixed path-resolution defect was reproduced across
module mappings, not patched with a BodyMetrics-specific rule.

Initial cold graph construction and snapshots interrupted by source changes required
refreshing after merge conflicts were resolved; such stale-read failures are not evidence
of a valid audit. A bounded brief still rated the multi-module integration low risk and
returned no direct modules, so its ranking was not used as a completeness guarantee.
An unscoped decision snapshot exceeded Windows' command-line length while enumerating
the large changed-path set. These planning limitations are distinct from build, test,
EF model and Wiki freshness verification. The extraction tasks' separate governed
delivery reports must not be represented as approval of the combined tree.
