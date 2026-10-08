# Wiki JSON fingerprint normalization

The instruction outcome validator repeatedly fingerprints events and their
source sets. A controlled 1000-event profile recorded 2002 calls: serialization
took 292 ms, regex normalization 3469 ms and SHA-256 hashing 183 ms. PowerShell
replacement delegates dominated this part of validation even when their regexes
could not match.

`Get-LlmWikiJsonFingerprint` now checks for the exact ordinal `\u` marker after
the existing HTML escaping. When it is absent, both Unicode normalization regexes
are provably unable to match. Otherwise the original regexes and replacement
delegates run unchanged. Serialization, escape handling, SHA-256 and every
journal schema, event, source, chain, policy and threshold check remain intact.
Persisted fingerprint values and default public outputs stay compatible.

## Measurements

Counterbalanced ABBA runs execute the real instruction manager with
`metrics -IncludeCandidates` on identical 1000-event histories and policy/source
bytes. The manager itself is byte-identical between variants; only the shared
JSON helper differs. Complete JSON responses match exactly in every run.

| Variant | Samples |
| --- | --- |
| Original helper | 8.879 / 5.629 s |
| Marker fast path | 1.290 / 1.074 s |

The first baseline includes additional cold-start cost. Comparing the later
baseline with both optimized samples gives approximately 4.4–5.2 times faster
execution for this journal operation. This does not establish an equivalent
speedup for full Wiki verification, handoff or CI. Measurements are local to
Windows PowerShell runtimes, and whole-suite parallel timings are diagnostic.

Request-local source-set and complexity-band memoization prototypes showed no
stable additional improvement over the fast path in counterbalanced comparisons
and were removed.

## Compatibility and verification

Independent baseline comparisons cover 92 values in PowerShell 5 and 7, including
Unicode, emoji, control characters, HTML, apostrophes, nested values and literal
Unicode-looking text with varying backslash counts. Portable regressions also
check fixed canonical text and hashes. Outcome regressions preserve invalid and
restored journal behavior, changed policy and current instruction-source checks.

Shared-helper changes now select portability and journal regressions through the
existing verification-cache group and invalidate dependent stage receipts. No
focused group or legacy audit coverage is removed.

Raw profiles, counterbalanced measurements, platform parity logs and final
integrated gate results are retained under
`.artifacts/wiki-instruction-validation-20261008/`. The final source identity and
gate status are recorded in `final-results.json` after verification. Changes
remain local; production and the other active checkout are outside this work.
