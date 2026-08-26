---
id: workflow-developer-experience
kind: workflow
status: current
title: Use the compact LLM Wiki developer experience
summary: Drive normal work through a small facade while preserving existing adaptive, manifest, journey, evidence, and delivery sources of truth.
tags:
  - workflow
  - developer-experience
  - planning
sources:
  - .llm-wiki/wiki.ps1
  - .llm-wiki/tools/Get-LlmWikiExperience.ps1
  - .llm-wiki/tools/Get-LlmWikiSolutionComparison.ps1
  - .llm-wiki/tools/Get-LlmWikiPhaseStatus.ps1
  - .llm-wiki/tools/Get-LlmWikiManualQaPlan.ps1
  - .llm-wiki/tools/Get-LlmWikiWorkflowMetrics.ps1
  - .llm-wiki/tools/Get-LlmWikiResearchPacket.ps1
  - .llm-wiki/tools/Manage-LlmWikiImpactSimulation.ps1
  - .llm-wiki/tools/Get-LlmWikiChangePacket.ps1
  - .llm-wiki/tools/LlmWikiQueryCache.ps1
  - .llm-wiki/tools/Invoke-LlmWikiMcpCommand.ps1
  - .llm-wiki/tools/Test-LlmWikiQueryCache.ps1
  - .llm-wiki/policies/experience-policies.json
---

# Compact LLM Wiki developer experience

Normal development has five user-facing steps:

```text
develop -> next -> phase-next -> validate -> handoff
```

The default `help` output deliberately exposes only the primary daily workflow
instead of presenting every compatibility and governance route as an equal
choice. Use `help -Detailed` when diagnosing or automating an administrative
command. The complete command catalog and its existing routes remain supported;
the short help is a discoverability boundary, not a breaking CLI migration.

Start or inspect work with one command:

```powershell
./.llm-wiki/wiki.ps1 next -Intent "Fix the invitation link" -PlannedPath '<known path>'
./.llm-wiki/wiki.ps1 next -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

`next` prints exactly one recommended action and a short preview of later work.
It derives state from the adaptive route or existing governed workspace; it does
not create a second workflow record. `status` exposes the same compact pipeline
without replacing the detailed delivery and task status commands. Route-only
fields such as `profile` are rendered only when present, because governed and
adaptive state intentionally have different payload shapes.

When a durable choice exists, compare options before design:

```powershell
./.llm-wiki/wiki.ps1 solutions -Intent '<outcome>' `
  -Option '<bounded option>','<structural option>' `
  -BoundaryEvidence '<current-source proof when a new boundary is required>'
```

The comparison is derived advice. It reports grounded current paths, matching Git
precedent counts, tradeoffs, rejection conditions, and the evidence that would
change its recommendation. Missing evidence remains explicit, so a structural
option cannot appear grounded merely because it was proposed. Record the selected
option and source evidence through the existing `design -Decision` or task journal.

For governed work, inspect implementation phases with `phase-status` or
`phase-next`. `phase-complete -FailOnInvalid` verifies the selected manifest
phase against the current Git diff. Phase state is never stored separately from
the manifest and Git evidence.

Generate manual exploratory coverage with `qa`. Journey cases come from the
durable product journey catalog; generic failure and retry cases are always
included, while accessibility, localization, and mobile cases are added only for
frontend scope. Generated output is disposable unless a reviewed scenario is
promoted into the journey catalog.

`workflow-metrics` summarizes local workspace adoption and outcomes. Sealed tasks
also append a compact outcome under `.git/llm-wiki/workspace-outcomes`, so adoption
evidence survives cleanup of the working task directory. With neither active
workspaces nor durable outcomes it reports `insufficient-data` instead of a
misleading zero adoption rate. `status` without an active workspace returns an
idle state and the next routing hint rather than throwing. Per-operation median
and p95 durations expose workflow cost.
Full verification also records each phase with its profile, run id, duration,
and failure category, making standalone audits and their failure point visible.
These signals measure operation and reliability rather than product value; all
metrics remain local inputs for retrospective improvement, not proof that the
Wiki caused quality.
Ceremony budgets keep tiny and bug work short and reserve governed workspaces and
independent critique for evidence that requires them.

Repeated structured planning queries reuse a content-addressed cache under the
ignored Git directory. Task briefs, research packets, context queries, and test
plans include the current commit, normalized arguments, hashes of relevant
modified/untracked paths, and hashes of their dependent indexes. An edit outside
the declared scope preserves a warm result; a relevant edit or index-lineage
change invalidates it. Injected test inputs bypass the cache, and the cache stores
only derived JSON; authoritative sources and generated Wiki pages remain unchanged.
Writes use unique temporary files and atomic replacement. Cleanup is
idempotent, so concurrent sessions may prune the same stale entry without
turning an already-successful query into a failure.
Reads parse the cached payload as JSON before reuse. A truncated or otherwise
invalid entry is deleted and treated as a miss, preventing derived-command
success with missing structured data after a crash or interrupted write.
Read-only facade commands consume the already published SQLite projection and
its dependency fingerprint. They never refresh or regenerate indexes as an
implicit side effect. Missing or stale projections stop with an explicit
recovery action; `graph-build`, `update`, and `verify` remain the deliberate
writer/verification paths. Isolated snapshots use Git status to detect tracked
or untracked mutations without re-hashing every multi-megabyte compiled index on
each query.

The table-driven `catalog`, `symbols`, `frontend`, contract, topology, quality,
configuration, sensitive-data, architecture-health, and module commands are
readers by default. They return bounded sections from committed projections and
state that freshness is not verified. `-Check` invokes the corresponding
generator in verification-only mode; regeneration belongs to `update`.

Adaptive routing asks the task brief to omit focused test-plan construction,
because route selection consumes scope, risk, ownership, rollout, privacy, and
decision evidence but never test scenarios. Diff context reads catalog metadata
and exact changed-path C# symbols from SQLite, then enumerates focused C# test
candidates from Git once instead of recursively scanning the working tree for
every matched module. Task-brief intent inference also starts from SQL-filtered
symbol candidates and reuses that same compiled result for its exact nested
diff, avoiding both a frontend-index JSON parse and a second Node process.
Task-brief impact analysis then performs one SQLite query across seven projected
indexes. It verifies every normalized source hash but materializes only exact
changed-path records and global architecture violations, avoiding repeated
multi-megabyte PowerShell JSON parses during routing regressions and MCP queries.
Missing or stale projections stop the route explicitly; JSON is available only
when a caller requests the diagnostic baseline.

The Development MCP keeps an additional bounded in-memory cache for successful
read-only Wiki results. Planned paths and trace candidates define a scoped
snapshot fingerprint, so unrelated concurrent edits do not evict the entry or
produce a false `snapshot_changed`. A development-context request without an
initial planned path runs the read-only graph trace first, accepts high/medium
candidate paths as its scope, then captures one baseline for brief and test-plan
composition. Entries expire after two minutes; oversized results, failures, and
cancellations are not retained. `get_server_status` exposes cache hit/miss counts,
queue depth, active commands, failure categories, and bounded per-command p50/p95
timings for operational diagnosis.

The MCP command bridge sends compact `brief` and fast `test-plan` requests
directly to their implementation scripts while keeping the public `wiki.ps1`
facade as the fallback for every other command. It strips facade-only arguments
before splatting, so direct execution preserves the same contract without paying
for repeated facade initialization. The 12-case development-context gate now
caps cold p95 at 20 seconds and warm p95 at 10 seconds; it continues to require
complete bundles, focused checks, bounded scope, and explainable SQL ranking.

Facade command routing is guarded structurally: the command-catalog regression
parses `wiki.ps1`, requires every `ValidateSet` command to have exactly one
router clause, rejects undeclared or duplicate routes, and validates every
table-driven index command against an existing builder script. Index builder
commands share one dispatch map instead of maintaining twelve repeated switch
handlers.

The `ui-discovery` budget prevents intent-only UI wording from selecting a
governed route. It allows only runtime-owner research and grounded
reclassification; inferred paths cannot authorize edits or introduce backend
and auth context merely because the named screen belongs to a sensitive journey.

The `scope-discovery` budget applies the same rule to ambiguous non-visual
features and bugs. It allows a compact brief plus existing-flow research, then
requires reclassification with refined intent and confirmed paths. No design,
workspace, security, rollout, or implementation ceremony starts until evidence
proves the corresponding boundary. Existing sensitive fields remain visible as
review context without automatically escalating an unchanged data lifecycle.
Database vocabulary in a query/read-model bug follows this discovery rule;
explicit security incidents and data-lifecycle mutations do not.

Frontend context and diff discovery now receive SQL-prefiltered frontend-index
candidates through the same compiled-context call already used for C# evidence.
The raw frontend index no longer participates separately in SQLite context-cache
keys; the graph dependency fingerprint carries its normalized source lineage.
Runtime-owner discovery also avoids parsing that index because its ranking uses
the richer frontend-contract component and consumer records exclusively. Those
records are now selected from SQLite as a bounded candidate set; render-chain
edges are followed inside the same process, and the command reports source,
candidate/returned counts, source hash, SQL time, and full round-trip time.
The broader frontend trace now uses the same process boundary for frontend symbols,
routes, component contracts, selector consumers, API calls, and source traversal.
JSON remains a committed generation and parity source, but normal `trace` calls no
longer materialize both indexes in PowerShell.
Impact simulation likewise reuses a minimal frontend feature catalog from the
change packet's existing compiled-context round trip. Normal simulation no
longer parses frontend-index JSON, and research cache keys rely on the graph
dependency fingerprint instead of hashing frontend and quality indexes again.

The `maintenance` budget treats concrete CI diagnostics, manifest/lockfile
compatibility errors, and Docker or deployment-build failures as primary
evidence. It uses four stages: compact evidence brief, bounded implementation,
the exact failing validation command, and diff plus `verify-fast`. Application
flow research, handler trace, journeys, and design stay absent unless concrete
evidence expands the task beyond maintenance.

The `pattern-extension` budget treats a grounded, current repository precedent
as design evidence. Its five stages are precedent brief, compatibility delta,
implementation, focused parity verification, and strict affected completion.
It remains lightweight only while provider, sensitive-data lifecycle, security,
configuration, and architecture boundaries stay unchanged.

The `test-only` budget is based on changed paths rather than vocabulary found in
the code under test. It uses four stages: compact coverage brief, assertion or
fixture implementation, focused execution, then affected refresh plus
`verify-fast`. It omits journeys, design, privacy, rollout, and governed
workspace ceremony. Test project manifests and runner configuration are not
eligible because they can change dependencies or execution infrastructure.

The bounded cross-layer bug route treats layer count as transport shape rather
than feature scope. Once current sources prove one root cause and one existing
module flow, an additive Frontend/API/Backend fix uses a compact brief and trace,
implementation, focused tests, then diff plus `verify-fast`. Journey mapping,
design, and full local verification become conditional; publication hooks and
CI remain strict. Grounded repository query fixes omit request-handler trace and
use their supplied source and integration-test paths as the discovery boundary.

The `visual-ui-change` budget is a five-stage focused route for bounded frontend
rendering work and local interaction inside an existing component with unchanged
routes, public component contracts, API, provider, persistence, privacy, security,
configuration, and architecture boundaries. It starts with a compact brief that
confirms runtime ownership, UI-kit versus application-shell placement, scoped
instructions, design-system constraints, and observable outcomes. It then moves
directly through implementation, combined focused tests and build, browser
evidence, and final diff plus `verify-fast`, avoiding a separate acceptance
ceremony, a full research packet, and unrelated full-index refreshes. The brief
constrains the work but does not invent the UX solution; current code, the
design system, and browser inspection remain the design inputs. Repository
pre-push and CI retain the complete frontend and Wiki verification as
publication gates. Browser QA follows the declared viewport scope instead of
requiring mobile proof for every desktop-only patch.

Research packets expose five stable lanes—flow, tests, integrations, precedents,
and guidance—so independent investigation can be parallelized without making
agent-specific scratch output part of the Wiki contract. For `feature`, `critical`,
and `architectural` work, design checkpoints add three acceptance-oriented vertical
slices that keep behavior and its closest verification together. Smaller profiles
do not gain slice-planning ceremony.

The packet keeps routing confidence separate from research confidence and
explains discovery, blocker-count, and implementation-scope confidence
individually. Read-only assessment reports implementation scope as
`not-required`; it does not turn an intentionally absent edit boundary into a
false warning or design blocker.

Compact research applies `-Limit` at every external array boundary and rejects
generic graph-only links such as `Unit`, `DependencyInjection`, and `Result`.
Known-failure matches require two meaningful tokens or explicit path evidence;
repository-brand stopwords do not qualify. With a planned path, unrelated routes
and runtime evidence are omitted unless graph or scope evidence connects them;
existing planned files remain the highest-priority implementation evidence. The
JSON contract advertises and enforces a 30,000-character maximum.

The same packet compiles non-empty research lanes into a policy-bounded
`researchPlan`. It groups lanes that share at least two repository paths,
publishes a deduplicated read set and duplicate-read savings, and marks groups
that have no cross-group path dependency as parallel-eligible. These are
execution hints rather than an agent-orchestration requirement. Use
`research-next-question` to retrieve only the highest-priority grounded open
question; remaining questions stay deferred in the packet.
