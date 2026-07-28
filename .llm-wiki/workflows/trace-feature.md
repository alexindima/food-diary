---
id: workflow-trace-feature
title: Trace a backend feature
kind: workflow
status: current
summary: Follow a request from its definition and handler to dependencies, implementations, presentation adapters, and tests.
tags:
  - workflow
  - trace
  - backend
sources:
  - .llm-wiki/tools/Find-LlmWikiTrace.ps1
---

# Trace a backend feature

Use this before changing an existing command or query:

```powershell
./.llm-wiki/wiki.ps1 trace -Query StartPremiumTrial
```

Use `-Format Json` when another tool or agent will consume the result.

The request, handler, dependency, implementation, and test links are derived from
explicit C# contracts. Presentation links are marked `direct` when the request type
is referenced and `heuristic` when the controller uses an HTTP mapping extension.
Confirm heuristic links before editing.
