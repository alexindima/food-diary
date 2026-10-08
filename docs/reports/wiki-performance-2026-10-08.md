# Wiki performance followup, 2026-10-08

Baseline: `7ec58395dc7abcc7d523f82b6a9cbcc4b7f46664` in the existing 4072 worktree.
This is a measured followup to the earlier tool audit, not a new claim that every
current source has received another complete review. The earlier 410-file ledger
describes its own checkpoint. Production and the active BodyMetrics workspace
were inspected read-only; their files were not changed.

## Changes and measurements

Sensitive-data generation now excludes `tests`, `obj`, `bin`, `.artifacts`,
`TestResults` and `Migrations` before descending into their directories. The
shared inventory accepts `-Force` to preserve this generator's hidden/system
file behavior. Other callers retain their default behavior. Directory links
remain excluded. Tests cover Unicode, hidden/system files and source identity.

Counterbalanced enumeration returned exactly the same 4,450 paths and ordering:
before 15.874/2.007 seconds, after 1.302/1.068 seconds. The first sample was cold.
Complete generated JSON also matched. Warm full-generator samples were 5.301
seconds before and 5.131 after; parsing variation prevents attributing a
proportional overall speedup to the enumeration change.

Telemetry validation retains one serialized result per process. Each invocation
rereads the registry, policy and validator and compares their complete text with
ordinal equality. Writes, alternate paths and any input edits invalidate reuse.
Malformed JSON remains an error. Returned values are detached from cached data;
retention still rejects recording without discarding history. The cache stores
existing telemetry fields and hashes, not raw commands or queries.

Eight validations of a fixed valid 1,000-event journal took 4.33/2.33 seconds
before and 1.39/1.27 after in an ABBA comparison with exact public output parity.
This improves repeated unchanged reads, including filled-journal record attempts;
successful appends still require fresh validation on the next invocation.

Retention now has a specific error identifier. The facade reports it once per
verify run while continuing every record attempt. Other errors remain visible,
and child-stage failures still fail verification. No history is automatically
archived. Regression fixtures exercise the actual facade stage function.

## Other investigated lanes

| Lane | Evidence and decision |
| --- | --- |
| Index parsing/cache | Hashing all 4,450 inputs took 1.532 seconds; parsing varies strongly between cold and warm runs. A new whole-generator cache has no demonstrated stable benefit. Existing exact-input index checks remain in use. |
| Verification preparation | Six actual stage fingerprints were measured twice: 0.16–0.23 seconds each. Preserve fresh runtime/Git/content checks rather than adding another stale snapshot cache. |
| Context recall/ranking | A fixed 100-query lexical SQL experiment replaced window-based distinct-path selection with grouped minimum ranks. All candidate rows/order matched; ABBA times were 5.805/5.884 before and 5.934/5.802 after. No stable improvement, so the canonical SQL is unchanged. The earlier thin-identity experiment likewise showed unstable timings. Recall pools, corpus cases and thresholds remain intact. |
| Task composition/journals | Empty-collection audit with a fixed 1,000-event journal preserved its complete output. ABBA times were 3.232/1.361 before and 1.483/1.266 after. These samples do not establish a general audit speedup. Telemetry reuse removes one repeated validation in verify-plus-metrics composition; caching all audit facts would need coherent registry/policy/clock invalidation. Existing compact handoff remains in place. |
| Evidence lifecycle | Current sources confirm that acceptance and lineage use policy-owned required check IDs. Supplemental manual checks need an explicit definition/provenance contract spanning initialization, refresh, mapping and lineage. Treating `-AffectedOnly` as equivalent to a full check would change evidence scope. These are compatibility changes, not demonstrated performance fixes, and remain separate followups. |
| Warning usefulness | Specific retention noise is collapsed once per run with recovery attempts preserved. Other telemetry failures and stage failures remain observable. |

The SDK session's 121–215 second sensitive-generator observations were recorded
under concurrent .NET builds and memory pressure. They identify a candidate,
not a controlled baseline for claiming the same improvement here.

## CI and deployment evidence

[CI run 37670553248](https://github.com/alexindima/food-diary/actions/runs/37670553248)
passed all 37 focused groups and the Core, Workspace and Orchestration audit
shards at `a87e311f29ab696469666c48f82799a2f3a6cd41`.
Focused job: 8:37; Core: 3:24; Workspace: 5:45; Orchestration: 2:32.
The complete Wiki gate finished in 9:07 versus 7:54 for the supplied earlier run;
different revisions/workloads prevent attributing that difference to one change.
Wiki finished 18:04 before the rest of CI, so it was outside that run's critical
path. Largest focused groups: context-search evaluations 222.59 seconds and
code-graph core 147.96 seconds.

[Linked deployment](https://github.com/alexindima/food-diary/actions/runs/37674038306)
also succeeded. Read-only SSH confirmed the same production API revision and
`Healthy` readiness during the deployment inspection. These observations concern
that deployed revision; the new changes in this report are local.

## Validation evidence

The source-inventory and telemetry-input regressions pass on installed Windows
PowerShell 7.6.5 and 5.1. The telemetry fixture checks same-size/time case-only
tampering, detached invalid results, policy/implementation changes, alternate
paths, malformed JSON, retention rejection and archive/reinitialization recovery.
Facade fixtures check warning collapse, distinct errors, new runs, all record
attempts and failed child stages. Routing includes the new regression in the
existing verification-cache group and its fingerprint inputs.

Raw owned measurements and final verification logs are under
`.artifacts/wiki-max-performance-20261008/`; deployment evidence is under
`.artifacts/wiki-deploy-review-20261008/`. Final status and immutable source hashes
are recorded in `final-results.json` alongside the logs. Linux execution and
production gains from these local changes are not claimed.
