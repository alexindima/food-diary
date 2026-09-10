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
  - .llm-wiki/tools/code-graph-trace-scope.mjs
  - .llm-wiki/tools/Test-LlmWikiTraceScope.mjs
  - .llm-wiki/generated/frontend-index.json
  - .llm-wiki/generated/frontend-contract-index.json
---

# Trace an existing feature

With `-Fast`, a PascalCase identifier query with no graph matches returns an
explicit `status: no-match`, warnings, and recovery steps without a full source
scan. This means no indexed match in the requested scope, not proof that a
feature is absent. `-FullTrace` retains the explicit source-scan route. Snapshot
preparation still validates the graph before lookup; stale/unavailable failures
are not converted into a no-match result.

Backend graph filtering applies to exact matches as well as ranked candidates;
an identically named frontend method cannot satisfy `-TraceView Backend`.
Semantic backend traces include module-owned presentation mappings and follow
the mapped `HttpRequest` or `HttpQuery` type to controllers within the same feature. The
`mapping-type` confidence records this evidence separately from direct command
references; it remains navigation rather than runtime call-graph proof.

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

The backend source scan prunes `obj`, `bin`, `.artifacts`, `TestResults`, and
`Migrations` directories before descending into them. It retains visible,
untracked C# sources and the prior generated-file exclusions, without following
directory symlinks. The candidate ranking and trace output are unchanged.

Backend-oriented natural-language queries automatically use the SQLite graph
when they contain terms such as SMTP, persistence, readiness, outbox, hosted
service, or telemetry. Narrow ambiguous searches explicitly with `-Layer
Backend`, `-Module MailInbox`, `-PathPrefix 'Services/MailInbox/'`, and optional
`-SymbolKind HostedService|Service|Handler|Controller|Repository`. A broad
graph result returns ranked candidates with confidence and concrete scoring
reasons. Candidate filters retain an explicit backend/frontend layer and match
module folder/project segments exactly: `Recipes` does not mean `FavoriteRecipes`.
A named backend module on the fast graph route defaults to backend candidates
unless a frontend view is explicitly requested. Test candidates remain ranked
after production. These scope rules do not change the context-search ranking or
quality thresholds; exact-symbol tracing may still show cross-module consumers.

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

Semantic source traces follow one same-namespace interface-inheritance hop when
interface names are unambiguous. Implementation entries expose `viaContract`
and `evidence: source-declaration`. `traceDepth`, `limitations`, and
`unresolvedDependencies` prevent a bounded result from implying a complete
runtime chain. Nested services, DI factory choices, and external/framework
implementations require separate inspection. These semantic fields apply to the
explicit source scan (`-FullTrace`), not an expanded traversal of fast MCP graph results.

Qualified static mapping calls returning the exact request type are followed
within the same feature with `mapping-method` evidence and the method name.
