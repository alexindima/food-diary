---
id: workflow.query-context
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Find-LlmWikiContext.ps1
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

JSON callers reuse an exact content-addressed result keyed by the query
arguments, HEAD, relevant worktree paths, and the catalog/symbol/frontend index
hashes. `-ScopePath` supplies the explicit cache boundary; `-Module` derives the
corresponding application project paths. An unrelated edit no longer invalidates
the query, while an edit inside the scope or a dependent-index change does.
Unchanged orchestration calls avoid reparsing the catalog and symbol indexes.
Text output remains an uncached interactive view.

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
