---
id: workflow-trace-feature
title: Trace an existing feature
kind: workflow
status: current
summary: Follow a backend request or frontend component through consumers, dependencies, routes, HTTP calls, and tests.
tags:
  - workflow
  - trace
  - backend
  - frontend
sources:
  - .llm-wiki/tools/Find-LlmWikiTrace.ps1
  - .llm-wiki/tools/Find-LlmWikiFrontendTrace.ps1
  - .llm-wiki/generated/frontend-index.json
  - .llm-wiki/generated/frontend-contract-index.json
---

# Trace an existing feature

Use this before changing an existing command, query, or Angular component:

```powershell
./.llm-wiki/wiki.ps1 trace -Query StartPremiumTrial
./.llm-wiki/wiki.ps1 trace -Query "dietologist invitation email link"
./.llm-wiki/wiki.ps1 trace -Query AiPhotoPreviewComponent
```

The default `-TraceView Auto` selects frontend trace when the query resolves an
indexed frontend symbol and otherwise falls back to the backend request trace.
Use `-TraceView Frontend` or `-TraceView Backend` to force a view. Use
`-Format Json` when another tool or agent will consume the result.

Backend queries may be exact request names or short natural-language descriptions.
Terms are normalized through common English and Russian aliases, then candidates
are ranked by matches in request names, handlers, paths, and handler source.
JSON output includes the score and matched terms so agents can calibrate trust.

The request, handler, dependency, implementation, and test links are derived from
explicit C# contracts. Presentation links are marked `direct` when the request type
is referenced and `heuristic` when the controller uses an HTTP mapping extension.
Confirm heuristic links before editing.

Frontend trace starts from an indexed symbol or selector and walks component
consumers plus AI-related facade/service dependencies. It reports consuming
routes, selector bindings, HTTP calls, and nearby tests. Treat route-to-feature
matching as navigational evidence and confirm the selected runtime path in source.
