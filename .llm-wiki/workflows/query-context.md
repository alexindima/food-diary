---
id: workflow.query-context
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Find-LlmWikiContext.ps1
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/tools/Test-LlmWikiCompiledIndexSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiDiffContextSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiTaskBriefSqlParity.ps1
  - .llm-wiki/tools/Build-LlmWikiCatalog.ps1
  - .llm-wiki/generated/repository-catalog.json
  - AGENTS.md
---

# Query Repository Context

Use the context resolver before exploring a cross-cutting change. It returns a
compact, ranked packet of wiki pages, scoped instructions, projects,
controllers, C# symbols, dependency-injection registrations, tests, module
dependencies, Angular features/routes/symbols/localization, ranked
implementation files, and recommended verification commands.

The resolver reads repository-catalog, C# symbol, and frontend feature/symbol/
route/localization candidates from the local SQLite compiled-index projection
by default. The graph is refreshed before the
read-only facade snapshot is created, and the reader verifies normalized source
hashes before returning data. A missing or stale projection fails explicitly;
`-CompiledIndexSource Json` is reserved for parity tests and diagnostics rather
than automatic fallback.

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

Required smoke tests compare SQLite and JSON-baseline output for context, diff,
and task-brief routes. They check exact functional parity, normalized source
hashes, changed-path candidate reduction, and bounded SQL/transport overhead.
Context parity includes frontend results and test recommendations across
frontend-specific queries. Task-brief parity excludes only route diagnostics,
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

`-Module` is matched against the executable application-module graph. `-Query`
adds free-text search terms. `-ChangeType` adjusts project ranking and emits
area-specific checks. `-PlannedPath`/`-ScopePath` boosts candidates in the
declared directories and feature roots. A frontend-only query suppresses
unrelated .NET clusters. CamelCase-aware token boundaries ensure a short term
such as `AI` matches `AiPhotoResult`, but not the letters inside `MailInbox`.
`-Limit` controls the maximum results per category.

For frontend work, `implementationFiles` searches tracked TypeScript, template,
and stylesheet sources. Planned paths are a hard boundary when supplied; files
outside that scope are excluded. Results expose whether the query matched the
path, content, or both, plus provenance and score. This list is intended to
answer “where is the implementation?” more directly than the broader feature
and symbol sections.

## Interpretation

Scores rank navigation candidates; they do not establish authority or prove
that a file must change. Read the returned wiki pages and applicable
`AGENTS.md`, then verify the result against code, tests, manifests, and contract
snapshots.

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
