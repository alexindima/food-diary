# ADR 0014: SQL-first Development Context with JSON Fallback

- Status: Accepted
- Date: 2026-08-21
- Owners: Developer experience
- Related: ADR 0013
- Supersedes: ADR 0013

## Context

ADR 0013 introduced an in-process, read-only SQLite context reader in shadow mode. The shared Node and .NET ranking policy now passes the committed 60-case corpus above the promotion criteria: 96.7% top-1 accuracy, 100% top-10 recall, and 0.9833 mean reciprocal rank. The corpus covers backend, frontend, integrations, jobs, privacy, API compatibility, and JavaScript/MJS tooling paths.

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

## Consequences

### Positive

- Warm aggregate requests avoid one PowerShell and Node process round trip.
- Dirty-worktree freshness is an exact content property rather than an age heuristic.
- A graph generated from a changing worktree cannot be published as fresh.
- Existing behavior remains available during bootstrap, corruption, lock contention, and unexpected retrieval gaps.
- Route counts provide concrete data for later fallback removal decisions.

### Negative

- The first request after a relevant change may pay for a graph refresh.
- Node and .NET still implement the shared ranking policy independently and require parity evaluation.
- The compatibility JSON trace remains maintained until its removal criteria are met.
- Aggregate context now performs a complete snapshot check before accepting SQL, although the MCP snapshot service caches content hashing between filesystem changes.

## Enforcement

- `SqliteWikiContextSearchTests` verifies freshness acceptance, mismatch rejection, ranking, read-only behavior, and telemetry.
- `WikiQueryServiceTests` verifies SQL-primary scope, one refresh attempt, JSON fallback, fingerprint forwarding, and routing telemetry.
- `.llm-wiki/evals/context-search.json` contains at least 60 representative cases and independent regression and switch criteria.
- `Test-LlmWikiSqlContextEvaluation.ps1` enforces the corpus floor and retrieval regression gate.
- `--evaluate-context-search` calculates the live MCP change-set fingerprint before evaluating, so a stale database cannot produce a false passing result.
- `Test-LlmWikiCodeGraph.ps1` and the full Wiki verification validate the writer and projection lifecycle.

## JSON Trace Removal Criteria

Removing the fallback is a separate decision. It requires all of the following evidence:

1. Node and in-process evaluations produce the same top candidates across the committed corpus, expanded to at least 100 cases with no top-10 misses.
2. Runtime route telemetry from representative development sessions contains at least 100 aggregate requests and less than 1% unexplained JSON fallbacks; expected first-request refreshes do not count as fallbacks when the retry succeeds.
3. Bootstrap, missing database, corrupt database, writer lock, cancelled refresh, and worktree-change races have explicit user-facing recovery behavior outside the JSON trace.
4. A consumer scan confirms that aggregate callers do not depend on trace-only payload fields or ordering.
5. Source, test, ADR, policy, privacy, journey, and scoped-instruction verification remains Git-backed even if the compatibility trace is removed.
