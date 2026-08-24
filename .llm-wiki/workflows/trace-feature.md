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
  - .llm-wiki/tools/Test-LlmWikiFrontendTraceSqlParity.ps1
  - .llm-wiki/tools/Test-LlmWikiTraceOutput.ps1
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

Backend-oriented natural-language queries automatically use the SQLite graph
when they contain terms such as SMTP, persistence, readiness, outbox, hosted
service, or telemetry. Narrow ambiguous searches explicitly with `-Layer
Backend`, `-Module MailInbox`, `-PathPrefix 'MailInbox/'`, and optional
`-SymbolKind HostedService|Service|Handler|Controller|Repository`. A broad
graph result returns ranked candidates with confidence and concrete scoring
reasons. Production C# candidates receive a backend-intent boost, while
frontend and test candidates are penalized for backend queries.

Text output is compact by default: one best match, bounded direct consumers,
routes, calls, and tests. Use `-FullTrace` only when broad dependency discovery
is intentional. JSON remains complete so composed tools do not lose evidence.

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

The frontend route reads the refreshed SQLite frontend and frontend-contract
projections by default. Matching, source traversal, contract joins, and route/test
selection run in one bounded graph process; the functional trace shape remains
unchanged. Missing or stale projections fail explicitly. Direct diagnostics may
select `-CompiledIndexSource Json`, and custom `-IndexRoot` fixtures require that
explicit source; neither path is an automatic fallback. The eight-case parity
smoke checks exact functional JSON, both normalized source hashes, payload
reduction, and a required end-to-end improvement.
