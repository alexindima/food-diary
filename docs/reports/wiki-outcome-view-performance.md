# Combined outcome views for handoff

Baseline: local commit `c177b3130`. Handoff requested outcome metrics and then
health/candidates separately for model routing, context strategies and instruction
effectiveness. Each pair reread and fully validated one journal, rebuilt its
profiles and serialized a second result.

Metrics now accepts `-IncludeHealth` in model/context outcome managers and
`-IncludeCandidates` in the instruction outcome manager. These switches are
valid only for metrics. The companion is derived from the same newly validated
registry and the same profile calculation. Default action outputs and standalone
health/candidates remain unchanged. No persisted or process-wide cache is added.

Handoff extracts the optional view and removes its internal member before
building its ordinary output. Existing workspace-specific verify calls remain
mandatory when saved outcome receipts exist, including compact handoff. Source
hashes for instruction candidates are read at the current call rather than
reused from an earlier request. No thresholds, cohorts, health verdicts,
retention or mutation behavior are relaxed.

## Measurements

Owned fixtures use the real managers and actual event payload/hash functions.
Each journal contains 300 valid events with multiple model/context profiles and
recent degraded outcomes. A fixed UTC history and identical policy/source bytes
are used for the old and new implementations. ABBA compares complete metrics and
companion JSON, not only selected counters.

| Journal pair | Before samples | Combined samples |
| --- | --- | --- |
| Model metrics + health | 0.533 / 0.244 s | 0.179 / 0.176 s |
| Context metrics + health | 0.605 / 0.294 s | 0.247 / 0.201 s |
| Instruction metrics + candidates | 2.789 / 2.123 s | 1.234 / 1.171 s |

The first baseline samples include cold startup. The instruction pair shows the
largest repeatable saving. These are journal-view costs, not a measured equal
speedup for complete handoff, Wiki verification or CI. Raw exact sample values
are in `.artifacts/wiki-outcome-views-20261008/outcome-view-times.json`.

## Verification

Owned regressions compare default and companion output, count the actual
validation/profile functions, preserve invalid-history results and restoration,
and reject companion switches on other actions. They also check missing/empty
history, changed policy thresholds and current instruction hashes. Handoff tests
assert one metrics call per journal and retain separate required workspace
receipt verification and its failure behavior. The existing focused catalog and
fingerprints include the new regression and all three managers.

Final platform, Workspace and affected-gate status, source identity and evidence
hashes are recorded in `.artifacts/wiki-outcome-views-20261008/final-results.json`.
Production and `D:\FD` are outside this followup's mutation scope. The changes
remain local; no publication or deployment is claimed.
