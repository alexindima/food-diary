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
./.llm-wiki/wiki.ps1 trace -Query "dietologist invitation email link"
```

Use `-Format Json` when another tool or agent will consume the result.

Queries may be exact request names or short natural-language descriptions.
Terms are normalized through common English and Russian aliases, then candidates
are ranked by matches in request names, handlers, paths, and handler source.
JSON output includes the score and matched terms so agents can calibrate trust.

The request, handler, dependency, implementation, and test links are derived from
explicit C# contracts. Presentation links are marked `direct` when the request type
is referenced and `heuristic` when the controller uses an HTTP mapping extension.
Confirm heuristic links before editing.
