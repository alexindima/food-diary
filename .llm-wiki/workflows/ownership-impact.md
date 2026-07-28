---
id: workflow-ownership-impact
kind: workflow
status: current
title: Review ownership and downstream impact
summary: Resolve scoped AGENTS guides and transitively affected application modules for a change set.
tags:
  - workflow
  - ownership
  - impact
sources:
  - docs/architecture/module-dependencies.json
  - .llm-wiki/tools/Get-LlmWikiOwnershipImpact.ps1
---

# Review ownership and downstream impact

Ownership analysis accepts the compiled diff used by a change packet, avoiding
duplicate module and guide discovery.

Run before a cross-module change or review:

```powershell
./.llm-wiki/wiki.ps1 ownership
```

The report identifies directly changed modules, all downstream consumers in the
executable module graph, and the nearest scoped `AGENTS.md` for every changed
path. Downstream impact means review/test consideration, not that every consumer
must be edited.
