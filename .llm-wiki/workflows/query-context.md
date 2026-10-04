---
id: workflow.query-context
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Find-LlmWikiContext.ps1
  - .llm-wiki/tools/LlmWikiQueryCache.ps1
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/tools/code-graph-identity.mjs
  - .llm-wiki/tools/code-graph-query-terms.mjs
  - .llm-wiki/tools/Ensure-LlmWikiSqliteProjection.ps1
  - .llm-wiki/tools/Test-LlmWikiCompiledIndexQueries.ps1
  - .llm-wiki/tools/Test-LlmWikiDiffContextQueries.ps1
  - .llm-wiki/tools/Test-LlmWikiTaskBriefQueries.ps1
  - .llm-wiki/tools/Build-LlmWikiCatalog.ps1
  - .llm-wiki/evals/context-search-holdout-100.json
  - .llm-wiki/evals/context-search-unseen-20260826.json
  - .llm-wiki/generated/repository-catalog.json
  - FoodDiary.Development.Mcp/Wiki/WikiQueryService.cs
  - FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs
  - .llm-wiki/tools/LlmWiki.SqliteReader/ContextSearchReader.cs
  - AGENTS.md
---

# Query Repository Context

Each CLI context request probes the live graph status before cache reuse. Its
bounded `currentWorkspace` token carries the repository identity, Git HEAD,
content fingerprint, changed-path count and freshness state. The cache key reuses
this token within that request instead of invoking Git and hashing the same
workspace again. Dependency hashes are still checked separately; the next
request performs a new status probe, including same-size edits with preserved
timestamps. A global token cannot replace a scoped cache snapshot. Module source
directory discovery runs only on a cache miss.

CLI and MCP context ranking share `FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs`. The CLI adapter restores indexed layer, module, role, and test features while preserving its response shape. Identical concurrent MCP command-cache misses share a bounded gate; cancelling a waiter leaves the running command intact.

The CLI reads candidate features in one parameterized batch from `context_search_features`, joining the FTS table by rowid. It selects the first source row for each requested path and record type, preserving the earlier feature selection and candidate order without scanning the FTS table once per candidate.

For explicit instruction questions, a bounded subject-specific pool recalls long agent guides whose bodies contain the named subject. Queries requesting both frontend and backend remain low-confidence and ambiguous. Frozen corpus targets and thresholds are unchanged.

For compound subjects, adjacent query words are matched together in the path/title index. A bounded pool reserves distinct paths before limiting rows; only missing paths are appended, preserving existing lexical order and scores. This prevents repeated projection records from excluding another relevant file.

## Retrieval regression checks

Application files are retrieval inputs: adding a renderer or moving a module can
change rankings even when Wiki scripts are untouched. Pre-push runs
`Test-LlmWikiSqlContextEvaluation.ps1` before solution builds, using the same
frozen corpora, quality thresholds and Node/.NET parity checks as CI. Index
freshness alone does not validate search quality.

On failure, inspect `.artifacts/llm-wiki/context-evaluation/`. Reports contain
per-case expected/actual paths, confidence, score margins and ranking reasons;
the console also lists missed cases. Check role specificity and exact file
identity before adding a path-specific rule. Keep frozen thresholds and expected
answers intact. A generic renderer name is insufficient evidence of localized
resource ownership, and a query naming a projection must not receive a generic
reader-service bonus. Both search runtimes must apply the same rules.

The lexical candidate budget is filled to distinct source paths while retaining
all rows and ordering from the original lexical window. Additional paths contribute
their best FTS match, with source-row order breaking ties. Duplicate code, contract
and quality projection records cannot crowd another file out of the pool, and
existing representations remain available for ranking. Node and the read-only
.NET reader use the same selection; pool limits and ranking scores are unchanged.

Keep the working tree stable during evaluation. A runtime `snapshot-mismatch`
means source/index state changed, not that every query ranked incorrectly.
The parity gate reports unavailable runtime context separately; finish edits
and index generation before rerunning the complete evaluation.

Search supplements the existing lexical and identity candidate pools with at
most `identityCandidatePoolLimit` candidates from a compact path/title FTS index.
This index excludes document bodies so long pages retain title-based recall.
The Node writer rebuilds it with the search projection after a schema change;
the .NET reader remains read-only. Both runtimes recognize English `-es` plurals
such as `indexes` while retaining previous query alternatives. Pool limits and frozen evaluation thresholds remain unchanged.

Use the context resolver before exploring a cross-cutting change. It returns a
compact packet built around one unified ranked candidate list, plus derived
Wiki pages, scoped instructions, controllers, implementation files, symbols,
tests, and recommended verification commands. Some legacy-shaped sections such
as projects, routes, or dependency-injection registrations can be empty on the
SQL route; use the ranked paths and their reasons as the primary navigation
contract.

If the ranked window contains no tests, the resolver makes one additional
test-oriented SQLite query with the same module, query text, and path scopes.
Its results populate only `tests`; production ranking and confidence are
preserved. This prevents a large API surface from hiding focused module tests.

SQLite is the only compiled-index query provider. Generated JSON snapshots remain
inputs for the sole Node projection writer and reviewable Git artifacts. Queries
validate exact source hashes, select bounded records in SQL, and report explicit
recovery errors when preparation fails. Backend-only preparation works without
TypeScript; frontend code-graph discovery requires the locked npm dependencies.
Direct behavior tests cover identity, selection, scope, freshness and output bounds.

On a clean checkout, backend-oriented context requests can bootstrap the SQLite
projection without installing frontend packages. That backend-only refresh
publishes C# and generated query documents and marks the TypeScript projection
as incomplete. A later `Any` or `Frontend` context request automatically performs
the full graph refresh. If the TypeScript compiler is unavailable, the full
refresh fails immediately with an actionable `npm ci` message; it never waits for
a late parser failure and never silently switches to JSON.

JSON result callers reuse an exact content-addressed result keyed by the query
arguments, HEAD, relevant worktree paths, and the selected source dependencies.
SQLite routes use the graph dependency fingerprint and exact source dependencies.
`-Module` derives application paths through the backend module map, including
`Modules/<Module>/Application` and supported legacy layouts. Context also keys on
the complete graph change-set fingerprint, so an
unrelated edit that changes that projection can invalidate its result cache.
Unchanged orchestration calls avoid querying and transporting catalog/symbol
records again.
Text output remains an uncached interactive view.

Required smoke tests check diff, task-brief and context behavior directly. They check
normalized source hashes, changed-path candidate reduction, multi-scope
coverage, and bounded SQL/transport overhead. Context coverage includes
frontend results and test recommendations across frontend-specific queries.
Task-brief tests preserve explicit planned paths, prove that SQLite intent selection is
reused by nested diff, and checks the seven-source impact projection across
compact and full results. It guards duplicate preservation, normalized source
hashes, freshness bytes, materialized payload reduction, and end-to-end latency.
Backend- and frontend-contract query tools apply the same freshness/no-fallback
contract in specialized SQL views with direct behavior groups. Generated JSON
remains only a build input and a Git-review artifact for compiled indexes.

## Examples

```powershell
./.llm-wiki/tools/Find-LlmWikiContext.ps1 -Module Billing -ChangeType Api

./.llm-wiki/tools/Find-LlmWikiContext.ps1 `
  -Module Fasting `
  -Query notifications `
  -ChangeType Backend

./.llm-wiki/tools/Find-LlmWikiContext.ps1 `
  -Query "AI dashboard" `
  -ChangeType Frontend `
  -PlannedPath @(
    'FoodDiary.Web.Client/src/app/features/dashboard'
    'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar'
  ) `
  -Format Json
```

`-Module` is matched against the executable application-module graph, remains
the returned module identity, and includes its generated module page and a
representative application implementation when available. `-Query` adds
free-text search terms. `-ChangeType` adjusts ranking and emits area-specific
checks. `-PlannedPath`/`-ScopePath` boosts candidates in the declared
directories and feature roots; when the visible limit can hold them, the
resolver preserves at least one ranked representative for every supplied
scope. A frontend-only query suppresses unrelated .NET clusters.
CamelCase-aware token boundaries ensure a short term such as `AI` matches
`AiPhotoResult`, but not the letters inside `MailInbox`. `-Limit` controls the
maximum visible results per category; the resolver searches a larger bounded
pool so focused tests and scope representatives are not lost behind production
matches.

For frontend work, `implementationFiles` searches tracked TypeScript, template,
and stylesheet sources. Planned paths guarantee representative coverage rather
than acting as an exclusion boundary, so a strongly relevant dependency outside
the supplied scopes can still appear. Results expose rank, score, confidence,
and explainable reasons such as `planned scope affinity`. This list is intended
to answer “where is the implementation?” more directly than the broader symbol
sections.

## Interpretation

Scores rank navigation candidates; they do not establish authority or prove
that a file must change. Read the returned wiki pages and applicable
`AGENTS.md`, then verify the result against code, tests, manifests, and contract
snapshots.

Use the independently authored holdout corpus as the primary retrieval-quality
signal. The frozen target-aware synthetic unseen corpus is a deterministic
diagnostic for ranking regressions and cohort balance; because its expected
paths informed its construction, it is not evidence of real-user query quality.
Keep target paths aligned with verified source relocations without changing
their semantic targets or thresholds. Preserve queries for pure relocations;
when an API is retired, explicitly document any query migration needed to remove
a deleted type name or contradictory architectural premise. Such migrated cases
are regression evidence, not a new blind evaluation. Structural ranking
aliases preserve layer preferences for module providers and shared libraries;
search results still name their current physical owners.

Context discovery is advisory. Run `wiki.ps1 policy` for deterministic
repository obligations and use an evidence bundle when those obligations need
an auditable task handoff.

HTTP matches come from the generated literal attribute-route catalog. Test
matches use `rg` to preselect semantic/path candidates, then read and rank only
those test sources; environments without `rg` retain the complete-scan fallback.

Frontend API discovery is strongest for direct literal calls. When a feature
service inherits request helpers or composes endpoint suffixes through a base
URL, a zero-result API query is inconclusive; inspect the service and its tests
directly.

Conversation-style questions also use bounded subject-name and explicit layer
affinity. Unknown compound identifiers produce low-confidence candidates with
`unmatched-query-identifier`; this is a retrieval limitation, not proof of absence.

MCP distinguishes how Wiki selects tests from a request to locate those tests.
Graph-only test planning recognizes `.test.mjs` and `.test.cjs` consumers as well
as JavaScript, TypeScript, C# and PowerShell tests. References are not execution evidence.

## Compact lookup and exact identities

Use `wiki.ps1 context -Query '<question>' -Compact -Format Json` for a single
bounded list instead of repeated legacy categories. The SQL compact view has
a 12000-character budget and reports omitted candidates; with room for more
than one result it includes a test lead when this does not displace the only
representative of a requested scope. `output.missingScopes` reports uncovered
scopes when the candidate limit is insufficient. If the character budget cannot
retain scope representatives, the command fails explicitly. Omit `-Compact` for
the unchanged full response schema. Cached responses mark `cache.hit` and
`cache.storedTimings`: stored timings are not a fresh latency measurement.
SQLite cache reuse follows freshness validation and fingerprints the graph,
ranking policy and formatter; stale data cannot be authorized by a cache hit.

Exact file paths, filenames and stems precede fuzzy ranking. Multiple exact
names across different paths remain explicitly ambiguous. Physical module and
layer metadata derive from current project identities, including relocated
project folders. [Self-maintenance](self-maintenance.md) checks this projection
and repairs deterministic drift through the existing writer.

Exact compound declared symbols precede fuzzy filenames. PowerShell synopsis
text is indexed as command identity, making a tool's purpose searchable in its
documented languages without adding query-specific ranking rules.

Read-only facades reuse an exclusively locked checkout per HEAD and requested
scope. Changed overlays reset the private checkout and apply the current files;
obsolete untracked files are removed. A fingerprint recheck rejects concurrent
source changes during preparation. `ScopePath` and `ProposedPath` both constrain
the overlay. Verbose snapshot stage timings separate preparation from lookup.

The maintenance regression corpus records audit-driven cases, not a new blind
holdout. Keep independently collected answer-quality questions separate from
ranking tuning. Semantic retrieval remains an experiment to evaluate against
this lexical baseline, not an implicit provider dependency.
