---
id: workflow.review-diff
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Get-LlmWikiDiffContext.ps1
  - .llm-wiki/tools/Get-LlmWikiImpact.ps1
  - AGENTS.md
---

# Review Change Context

Use the diff context command before implementation handoff or PR review:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1
```

By default it compares the working tree, index, and untracked files with
`HEAD`. For a committed change set:

```powershell
./.llm-wiki/tools/Get-LlmWikiDiffContext.ps1 `
  -BaseRef origin/master `
  -HeadRef HEAD
```

The result identifies:

- backend, API, frontend, database, test, documentation, localization, and
  contract scopes;
- affected application modules and their dependency context;
- changed .NET projects and applicable scoped guides;
- wiki pages requiring review;
- focused test files and recommended verification commands;
- generated catalogs or module pages that need regeneration;
- contract, localization, migration, and cross-module warnings.

Use `-Format Json` when another agent or automation will consume the packet.
The output is advisory: actual requirements still come from scoped
`AGENTS.md`, source code, tests, contracts, and the user's request.
