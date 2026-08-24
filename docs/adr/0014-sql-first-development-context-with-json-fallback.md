# ADR 0014: SQL-first Development Context with JSON Fallback

- Status: Accepted; JSON fallback retired 2026-08-24
- Date: 2026-08-21
- Owners: Developer experience
- Related: ADR 0013
- Supersedes: ADR 0013

## Context

ADR 0013 introduced an in-process, read-only SQLite context reader in shadow mode. The shared Node and .NET ranking policy passes a committed 60-case regression corpus above the promotion criteria: 96.7% top-1 accuracy, 100% top-10 recall, and 0.9833 mean reciprocal rank. A separately authored 40-case challenge corpus improved from a pre-tuning baseline of 11/40 top-1 and 19/40 top-10 to 40/40 top-1. Together the corpora cover 100 cases with no top-10 miss, including Russian queries, backend, frontend, integrations, jobs, privacy, API compatibility, and JavaScript/MJS tooling paths.

Shadow mode still paid for the legacy JSON trace on every aggregate request, so it could measure SQL quality but could not deliver the main latency benefit. Promotion also required a stronger freshness proof than file timestamps or Git HEAD alone because uncommitted source changes must participate in the selected context.

## Decision Drivers

- Warm aggregate context requests should avoid the PowerShell/Node trace subprocess.
- SQLite candidates must describe the exact Git HEAD and tracked or untracked worktree content used by the request.
- The Node graph manager must remain the only SQLite writer.
- A stale, unavailable, invalid, or empty SQL result must not make development context unavailable.
- The existing brief, test-plan, policy, ADR, scoped instruction, and source-verification obligations must remain intact.
- The active route and fallback frequency must be visible in MCP runtime telemetry.

## Considered Options

1. Keep SQL in shadow mode and always execute the JSON trace.
2. Make SQL primary and fail the aggregate request whenever its projection is unavailable.
3. Make SQL primary, attempt one governed graph refresh when stale, and retain JSON trace as a bounded fallback.
4. Remove all JSON-backed Wiki data and store policy and reviewed knowledge in SQLite.

## Decision

Adopt option 3 for `get_development_context`.

- The graph writer records the complete change-set fingerprint and Git HEAD in the same transaction as the FTS projection. It verifies that the worktree did not change between the beginning and end of the build; otherwise it rolls back.
- The fingerprint includes Git HEAD, raw porcelain status, ordered changed paths, and a SHA-256 content digest or missing marker for every tracked or untracked changed path.
- The MCP obtains the same complete snapshot and accepts SQLite candidates only when the fingerprints match exactly.
- A fresh, non-empty SQL result supplies up to ten expanded scope paths. The aggregate request skips the legacy JSON trace but still runs the existing compact brief and fast test plan against that scope.
- A missing projection, missing freshness metadata, or fingerprint mismatch triggers at most one normal `graph-build`. The MCP refreshes its snapshot and retries once.
- If the retry is unavailable, stale, invalid, or empty, the request executes the established JSON trace. There is no retry loop.
- The standalone `trace_backend_flow` tool and JSON-backed policy and knowledge indexes are unchanged. SQLite remains a reconstructable code-navigation projection, not the authority for reviewed decisions or source claims.
- Runtime telemetry records `context-routing/sqlite-primary` or `context-routing/json-fallback`. SQLite query timing remains separately visible as `context-search/in-process-sqlite`.
- A bounded persistent route sample is stored outside the worktree under the resolved Git directory. It contains only timestamp, route, normalized fallback category, duration, and graph-refresh outcome; query text, intent, content, source paths, fingerprints, user identity, and payloads are prohibited.
- `get_server_status` reports route counts, fallback categories and rate, p50/p95, refresh outcomes, the current SQLite-primary streak, retention, persistence health, the effective sample target and remaining successful samples, and whether the evidence threshold for considering JSON fallback retirement has been met.

### Recovery compatibility boundary

- The SQLite code graph is a reconstructable local cache. When the sole Node writer confirms `SQLITE_CORRUPT` or `SQLITE_NOTADB` while holding the build lock, it may quarantine the database and its sidecars under `.artifacts/llm-wiki/code-graph/` and rebuild from current sources.
- Busy or locked databases, cancellation, lock-wait timeout, permission failures, and unclassified SQLite errors are never treated as corruption and never trigger quarantine.
- `DevelopmentContext.BackendTrace`, `ContextRetrievalSource`, and `ContextFallbackReason` remain compatible while the JSON fallback retirement criteria are incomplete. The standalone `trace_backend_flow` tool is not coupled to aggregate-context retirement.
- Worktree-change races continue to fail with the existing retryable `snapshot_changed` error instead of publishing a graph built from mixed source states.

## Consequences

### Positive

- Warm aggregate requests avoid one PowerShell and Node process round trip.
- Dirty-worktree freshness is an exact content property rather than an age heuristic.
- A graph generated from a changing worktree cannot be published as fresh.
- Existing behavior remains available during bootstrap, corruption, lock contention, and unexpected retrieval gaps.
- Route counts survive MCP restarts and provide concrete, privacy-safe data for later fallback removal decisions.

### Negative

- The first request after a relevant change may pay for a graph refresh.
- Node and .NET still implement the shared ranking policy independently and require parity evaluation.
- The compatibility JSON trace remains maintained until its removal criteria are met.
- Aggregate context now performs a complete snapshot check before accepting SQL, although the MCP snapshot service caches content hashing between filesystem changes.

## Enforcement

- `SqliteWikiContextSearchTests` verifies freshness acceptance, mismatch rejection, ranking, read-only behavior, and telemetry.
- `WikiQueryServiceTests` verifies SQL-primary scope, one refresh attempt, JSON fallback, fingerprint forwarding, and routing telemetry.
- The promoted search suite contains 450 strict cases across the primary, holdout, generalization, validation, and seven promoted probe corpora. A separate 100-case retirement holdout and two 30-case controls remain diagnostic and preserve their blind baselines.
- `Test-LlmWikiSqlContextEvaluation.ps1` requires every corpus to meet its thresholds, both promotion corpora to meet switch criteria, a combined promotion floor of 100 cases, and no top-10 promotion miss.
- `--evaluate-context-search` calculates the live MCP change-set fingerprint before evaluating, so a stale database cannot produce a false passing result.
- `Test-LlmWikiDevelopmentContextEvaluation.ps1` evaluates the complete SQL-first aggregate bundle, including scope recall, noise budget, change context, focused checks, compact payload size, and end-to-end timing.
- `ContextRoutingTelemetryStore` tests enforce bounded retention, reload, concurrent writers, normalized fallback categories, and absence of query/path data.
- `Test-LlmWikiCodeGraph.ps1` and the full Wiki verification validate the writer and projection lifecycle.

## JSON Trace Removal Criteria

Removing the fallback is a separate decision. It requires all of the following evidence:

1. Node and in-process evaluations produce the same top candidates across the committed corpus, expanded to at least 100 cases with no top-10 misses.
2. Persistent runtime route telemetry from representative development sessions contains at least 100 aggregate requests and no more than 1% JSON fallbacks; successful first-request refreshes remain SQL-primary observations.
3. Bootstrap, missing database, corrupt database, writer lock, cancelled refresh, and worktree-change races have explicit user-facing recovery behavior outside the JSON trace.
4. A consumer scan confirms that aggregate callers do not depend on trace-only payload fields or ordering.
5. Source, test, ADR, policy, privacy, journey, and scoped-instruction verification remains Git-backed even if the compatibility trace is removed.

## Retirement Evidence Review: 2026-08-24

The fallback is not ready for removal.

- A new 100-case holdout was authored before retrieval, with ten equal cohorts,
  unique primary targets, and no target reused from the promoted 450-case suite.
- Node produced 21/100 top-1, 61/100 top-10, and 0.3404 MRR. The in-process .NET
  reader matched every expected rank and every top-five candidate, so the result
  identifies a shared retrieval-quality gap rather than reader drift.
- Persistent representative telemetry contained 61 aggregate requests:
  60 SQLite-primary routes and one `graph-refresh-failed` JSON fallback. The
  1.64% fallback rate exceeds the 1% ceiling and the sample count is below 100.
  Five requests attempted a graph refresh; four succeeded. With no additional
  fallback, 39 more SQLite-primary requests are required to reach 1/100 = 1%.
- The weakest holdout cohorts were adjacent-role disambiguation (0/10 top-1),
  conversational Russian (1/10), persistence (1/10), and domain invariants
  (1/10).
- Root-cause fixes made the candidate pool independent of requested result
  limit, removed negated roles from positive FTS and boosts, preserved adjacent
  roles such as `handler` when only `command` is negated, translated negative
  configuration intent to `unconfigured`, and expanded shared bilingual role
  and subject affinity.
- The post-fix Node and in-process .NET evaluations both produced 57/100 top-1,
  100/100 top-10, and 0.719 MRR, with zero exact-rank or top-five differences.
  This satisfies the retrieval-quality and reader-parity portion of criterion
  1, but the tuned holdout is no longer unseen generalization evidence.
- A new post-fix control then froze 30 additional unique targets, reusing none
  of the earlier 550 targets. Its untouched first run produced 17/30 top-1,
  29/30 top-10, and 0.7079 MRR. Shared subject vocabulary and explicit roles
  later raised it to 27/30 top-1, 30/30 top-10, and 0.95 MRR with exact Node/.NET
  rank and top-five parity; its original baseline remains unchanged.
- A second control froze 30 targets unused by all earlier 580 cases. Its blind
  run produced 18/30 top-1, 28/30 top-10, and 0.7467 MRR. General fixes for
  quality/value objects, invariants, test roles, notification intent, exercises,
  password changes, and builders raised it to 26/30 top-1, 30/30 top-10, and
  0.925 MRR. Node and .NET again had zero exact-rank or top-five differences.

The original holdout baseline must not be reclassified as unseen evidence after
ranking is tuned against it. The initial review did not authorize removal; the
later retirement amendment below records the additional evidence and decision.

## JSON Fallback Retirement Amendment: 2026-08-24

All five removal criteria are satisfied, so `get_development_context` no longer
invokes JSON trace automatically. The standalone `trace_backend_flow` tool is
unchanged and remains available when a caller explicitly needs a backend trace.

- Persistent evidence reached 160 aggregate requests: 159 SQLite-primary and
  one historical JSON fallback (0.63%), with 115 consecutive SQLite-primary
  observations and healthy persistence. The controlled retirement runs used
  the frozen 30-case control corpora through the complete `WikiQueryService`
  bundle, not the isolated search reader.
- One pre-removal full-bundle control exposed a real generalization gap: three
  explicit test queries without `plannedPath` were searched as `Any`. Query
  intent now recognizes Russian and English test/spec terms. The unchanged
  corpus then passed 30/30 SQLite-primary, 30/30 expected-path top-10, and 30/30
  complete bundles.
- Missing, stale, confirmed-corrupt-after-rebuild, locked, empty-candidate, and
  refresh-failure states now return an explicit partial result with
  `context_search_unavailable` and a concrete rebuild/retry/refine action.
  Cancellation still propagates as `cancelled`, and worktree races retain the
  retryable `snapshot_changed` failure. None of these paths invokes JSON trace.
- A repository consumer scan found no aggregate consumers of trace-only fields
  or ordering outside the MCP implementation and its tests. `BackendTrace` is
  retained as a null compatibility field, while `ContextRetrievalSource` is
  `sqlite` or `unavailable`; `ContextFallbackReason` temporarily carries the
  normalized unavailability reason for compatibility.
- Persistent events remain privacy-safe. New failures use
  `sqlite-unavailable`; the store continues to read historical `json-fallback`
  events so the retirement evidence remains auditable. Source, tests, ADRs,
  policies, privacy guidance, journeys, and scoped instructions remain
  Git-backed and are not moved into SQLite.

The historical option-3 decision above remains the migration record. This
amendment removes only the automatic aggregate fallback, not JSON publication
artifacts or the explicit trace tool.
