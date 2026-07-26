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
dependencies, Angular features/routes/symbols/localization, and recommended
verification commands.

## Examples

```powershell
./.llm-wiki/tools/Find-LlmWikiContext.ps1 -Module Billing -ChangeType Api

./.llm-wiki/tools/Find-LlmWikiContext.ps1 `
  -Module Fasting `
  -Query notifications `
  -ChangeType Backend

./.llm-wiki/tools/Find-LlmWikiContext.ps1 `
  -Query localization `
  -ChangeType Frontend `
  -Format Json
```

`-Module` is matched against the executable application-module graph. `-Query`
adds free-text search terms. `-ChangeType` adjusts project ranking and emits
area-specific checks. `-Limit` controls the maximum results per category.

## Interpretation

Scores rank navigation candidates; they do not establish authority or prove
that a file must change. Read the returned wiki pages and applicable
`AGENTS.md`, then verify the result against code, tests, manifests, and contract
snapshots.

HTTP matches come from the generated literal attribute-route catalog. Test
matches additionally search test source paths and contents at query time.
