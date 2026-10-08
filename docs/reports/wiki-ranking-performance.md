# Query-local context ranking preparation

Baseline: `7ec58395dc7abcc7d523f82b6a9cbcc4b7f46664`, plus the previously verified
local Wiki performance changes. This followup changes the Node graph search and
the shared .NET SQLite reader. It does not change SQL recall, pool limits,
ordering rules, score values, reasons, confidence or snapshot/cancellation checks.

The implementations now normalize direct terms and calculate query intent once.
Generic affinity counts are retained only within that query. The .NET reader
filters identity/path boost applicability before visiting candidates, matching
the existing Node approach. Node no longer repeats query checks for already
applicable boosts. Documentation and declared-symbol intent are prepared once;
Node also reuses confidence policy preparation and lazily evaluates the
multi-layer query expression once, preserving the exact-identity short circuit.

## Measurements

An owned fixture samples ten queries from each of five existing corpora and six
additional cases covering negation, scoped module registration, PowerShell tests,
MCP, guidance and frontend transport. All 56 queries retain their complete
candidate pools. Query output comparisons include every returned field, score,
reason, margin and ambiguity value; only elapsed-duration fields are excluded
where applicable.

| Measured work | Before ABBA samples | After ABBA samples | Mean reduction |
| --- | --- | --- | --- |
| Node ranking/preparation, 224 query calls with fixed SQL rows | 11.479 / 11.701 s | 9.643 / 9.414 s | 17.8% |
| .NET ranking only, 224 query calls with fixed candidates | 13.974 / 15.722 s | 5.773 / 5.068 s | 63.5% |
| .NET public batch reader including SQLite, 56 queries | 10.603 / 12.069 s | 9.331 / 10.542 s | 12.3% |

The Node measurement includes replay-row cloning and lookup costs. The .NET
ranking fixture invokes the real private ranking method in separately loaded
baseline and updated assemblies. Its baseline comes from the previous verified
MCP build. The public-reader comparison uses the same fixed projection and policy,
with ordinary read-only SQLite calls; it is a query-cost comparison, not a new
worktree freshness or cold-start claim. Ranking-only improvements are not an
equivalent speedup for the entire Wiki or CI.

An earlier narrower Node prototype showed no stable improvement and is retained
as diagnostic evidence. The final version also removes repeated documentation,
symbol-query and confidence calculations. Failed setup/diagnostic helper builds
are excluded from timing samples. Production builds retain normal analyzers.

## Validation

The .NET build passes with zero warnings/errors. All 224 focused SQLite context
tests pass, including a new batch regression for per-query applicability,
minimum-match thresholds, excluded terms and change types. The batch alternates
eligible and ineligible requests through the same reader to detect leaked state.
Affected verification now selects corpus and retrieval regressions for changes
to either ranking implementation or its focused SQLite tests. A routing guard
protects those selections; all existing catalog groups remain available.
Whitespace checks pass for the changed reader and regression source.

Final corpus/runtime and Wiki verification results and immutable source hashes
are recorded in `.artifacts/wiki-ranking-hoist-20261008/final-results.json`.
Raw measurements, fixed inputs, diagnostic copies and logs are in that owned
artifact directory. The temporary benchmark project is outside the tracked
implementation; its generated `RankBench.packages.lock.json` is also outside the
intended delivery scope. Automatic approval review rejected its cleanup, so it
remains an untracked scratch file.

The earlier enumeration/telemetry changes and their separate verification remain
documented in [the previous report](wiki-performance-2026-10-08.md). These
followup changes are local; no commit, push or deployment is claimed.
