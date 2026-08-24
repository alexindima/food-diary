# FoodDiary Development MCP

Local, read-only stdio MCP server that exposes stable `.llm-wiki/wiki.ps1`
analysis entrypoints without replacing repository source-of-truth checks.

## Tools

- `get_change_context` wraps `wiki.ps1 brief`.
- `trace_backend_flow` wraps `wiki.ps1 trace`.
- `get_test_plan` wraps `wiki.ps1 test-plan` and accepts explicit
  `changedPaths` or fallback `plannedPaths` when the worktree is clean. Optional
  `baseRevision`/`headRevision` pin compatibility analysis; without a base on a
  clean worktree the result explicitly reports that compatibility is unavailable.
- `get_development_context` selects scope through the in-process SQLite FTS
  projection, then runs a compact brief and fast test plan concurrently. It
  accepts the projection only for the exact Git/worktree fingerprint, attempts
  one graph refresh when the projection is missing or stale, and returns an
  explicit partial result with a recovery action if SQL remains unavailable or
  returns no scope. It never invokes the JSON trace automatically. It refreshes
  the fingerprint between phases and rejects the result if the snapshot changed.
- `get_server_status` reports repository, Git HEAD, Wiki, index presence, and
  runtime identity (PID, process start, startup HEAD, build HEAD/source
  fingerprint, assembly MVID/hash, and build timestamps).
  It also reports the in-process query-cache hit rate and entry count, active
  and queued PowerShell commands, completion/failure/cancellation/timeout
  counters, bounded per-command p50/p95/maximum timings, and matching stage
  timings for request serialization, queue wait, process round-trip, and result
  processing.
  `runningCodeIncludesWorktreeChanges` compares the running build with the
  current MCP sources. Wiki index freshness is `verified` only when both source
  and index fingerprints match the receipt from a successful full Wiki verify.
  The MCP and receipt writer load the same
  `.llm-wiki/policies/query-indexes.json` manifest, so topology, privacy,
  frontend, domain, contract, quality, and catalog dependencies cannot silently
  fall outside the freshness proof.

The server does not expose governed task lifecycle, generation, delivery, or
repair commands. Wiki output remains derived navigation: callers must verify
change-sensitive conclusions in the referenced code, tests, ADRs, current docs,
and scoped `AGENTS.md` files.

`get_development_context` reads the existing SQLite FTS projection through an
in-process, read-only `Microsoft.Data.Sqlite` reader. A fresh, non-empty result
is the primary source of expanded code scope and avoids the legacy trace
subprocess. The Node graph manager remains the only writer. It publishes the
complete Git/worktree content fingerprint transactionally and rolls back when
the worktree changes during a build. Missing, stale, invalid, locked, or empty
local graph state produces `context_search_unavailable`, preserves any explicit
`plannedPath` as bounded scope, and describes how to rebuild, retry, or refine
the query. `trace_backend_flow` remains available only as an explicit tool.

Both the Node and MCP readers consume
`.llm-wiki/policies/context-search-ranking.json`. Routing counts appear in
runtime telemetry as `context-routing/sqlite-primary` and
`context-routing/sqlite-unavailable`; historical `json-fallback` events remain
readable for the retirement audit. SQLite query timing appears as
`context-search/in-process-sqlite`. `get_server_status` also derives refresh
attempt/success/failure counts, the current SQLite-primary streak, unavailable
count/rate, and the frozen fallback-retirement evidence. These aggregates do not
expand the persisted event envelope with queries, paths, or payloads. ADR 0014
records the fallback retirement decision. Git-backed source, tests, policies,
ADRs, and scoped instructions stay authoritative regardless of retrieval route.

MCP queries request JSON from the Wiki. This enables its snapshot-keyed query
cache; repeated requests against the same Git HEAD and worktree avoid repeating
expensive discovery. The MCP adds a bounded two-minute in-memory layer above
the Wiki disk cache, keyed by the same Git/worktree fingerprint plus the exact
command arguments. It retains at most 128 successful results, refuses payloads
larger than 1 MiB, and never caches failures or cancellations. The standalone
`get_test_plan` remains comprehensive,
while the aggregate context deliberately uses the fast graph plan to stay
within interactive tool timeouts. Its compact brief skips the embedded full
test-plan calculation because the aggregate already requests that plan in
parallel. JSON graph traces return the validated probe directly instead of
executing the same graph query a second time.

Large and Unicode-rich intents/path lists are serialized to a temporary JSON
request file instead of being placed on the Windows command line. Tool results
use MCP `structuredContent`; warnings, paths, and checks are read from the JSON
structure instead of regex-scanning serialized JSON. Compact results are the
default for trace, test-plan, brief, and aggregate tools. Use
`includeDetailedContext` for complete structured data or `includeRawOutput` for
raw diagnostics.

The command executor admits at most three PowerShell queries at once and caps
each stdout/stderr stream at 8 MiB. Cancellation, timeout, and output overflow
terminate the complete command process tree; corrupt JSON query-cache entries
are removed and recomputed instead of being returned as successful output.

## Run

From the repository root:

```powershell
./scripts/Start-FoodDiaryDevelopmentMcp.cmd --build-if-stale
```

The launcher binds the process to the worktree containing the launcher and sets
`FOODDIARY_REPOSITORY_ROOT` explicitly. A different validated worktree can be
passed as the second argument. All protocol traffic uses stdout; host
diagnostics use stderr.

The registered client uses `--build-if-stale`. The launcher fingerprints the MCP
project and shared build inputs, rebuilding only when the output is absent or no
longer matches those inputs. It publishes the output to an immutable temporary
runtime keyed by that source fingerprint. Concurrent and repeated clients reuse
the same runtime, so they do not lock shared `bin` output and disconnected
clients do not accumulate one directory per session. Active runtimes hold a
shared lock; old fingerprints and legacy session directories are removed only
after the lock can be acquired exclusively. The repository registration marks this server as
required and allows 120 seconds for startup, so startup failures are reported
instead of silently leaving the FoodDiary tools unavailable.

The trusted-project `.codex/config.toml` registers this server for Codex. Restart
the desktop app or extension after pulling/building the project.
