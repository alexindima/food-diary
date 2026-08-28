---
id: workflow.query-context
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Find-LlmWikiContext.ps1
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/tools/Ensure-LlmWikiSqliteProjection.ps1
  - .llm-wiki/tools/Test-LlmWikiCompiledIndexSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiDiffContextSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiTaskBriefSqlParity.ps1
  - .llm-wiki/tools/Build-LlmWikiCatalog.ps1
  - .llm-wiki/evals/context-search-holdout-100.json
  - .llm-wiki/evals/context-search-unseen-20260826.json
  - .llm-wiki/generated/repository-catalog.json
  - AGENTS.md
---

# Query Repository Context

Use the context resolver before exploring a cross-cutting change. It returns a
compact packet built around one unified ranked candidate list, plus derived
Wiki pages, scoped instructions, controllers, implementation files, symbols,
tests, and recommended verification commands. Some legacy-shaped sections such
as projects, routes, or dependency-injection registrations can be empty on the
SQL route; use the ranked paths and their reasons as the primary navigation
contract.

The resolver reads repository-catalog, C# symbol, and frontend feature/symbol/
route/localization candidates from the local SQLite compiled-index projection
by default. The graph is refreshed by the resolver inside the
read-only facade snapshot, and the reader verifies normalized source
hashes before returning data. A missing or stale projection fails explicitly;
`-CompiledIndexSource Json` is reserved for parity tests and diagnostics rather
than automatic fallback.

On a clean checkout, backend-oriented context requests can bootstrap the SQLite
projection without installing frontend packages. That backend-only refresh
publishes C# and generated query documents and marks the TypeScript projection
as incomplete. A later `Any` or `Frontend` context request automatically performs
the full graph refresh. If the TypeScript compiler is unavailable, the full
refresh fails immediately with an actionable `npm ci` message; it never waits for
a late parser failure and never silently switches to JSON.

Diff context uses the same projection in `changed-paths` mode. SQLite applies
the exact changed-path predicate before transporting C# and frontend symbol payloads, while
catalog-derived modules, projects, and guides retain their previous shape.
Task-brief intent inference also uses SQLite candidates before applying its
existing PowerShell scoring, then reuses the same selection for exact nested
diff filtering. Both commands keep `-CompiledIndexSource Json` as an explicit
test/diagnostic baseline and never select it automatically.

JSON result callers reuse an exact content-addressed result keyed by the query
arguments, HEAD, relevant worktree paths, and the selected source dependencies.
SQLite routes use the graph dependency fingerprint; explicit JSON baselines hash
their generated source files. `-ScopePath` supplies the explicit cache boundary; `-Module` derives the
corresponding application project paths. An unrelated edit no longer invalidates
the query, while an edit inside the scope or a dependent-index change does.
Unchanged orchestration calls avoid querying and transporting catalog/symbol
records again.
Text output remains an uncached interactive view.

Required smoke tests compare SQLite and JSON-baseline output for diff and
task-brief routes and exercise the SQL context route directly. They check
normalized source hashes, changed-path candidate reduction, multi-scope
coverage, and bounded SQL/transport overhead. Context coverage includes
frontend results and test recommendations across frontend-specific queries.
Task-brief parity excludes only route diagnostics,
requires exact functional output, proves that the SQLite intent selection is
reused by nested diff, and checks the seven-source impact projection across
compact and full results. It guards duplicate preservation, normalized source
hashes, freshness bytes, materialized payload reduction, and end-to-end latency.
Backend- and frontend-contract query tools apply the same freshness/no-fallback
contract in specialized SQL views with their own exact parity groups. This
protects result quality while the remaining build-time and explicit baseline
JSON consumers migrate incrementally.

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
