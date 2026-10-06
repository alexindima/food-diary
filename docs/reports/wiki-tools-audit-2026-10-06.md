# Wiki tool performance and reliability audit

Status: **in progress**. This is a complete-scope code audit, not a claim that
all tools have been reviewed or optimized.

The current source inventory contains **408 files and 74,954 baseline lines**:
the Wiki facade, every tool/library/regression source under `.llm-wiki/tools`,
the complete Development MCP runtime, and its launcher/CI integration. Generated
navigation is not an authority. Relevant policies, contracts and tests are
inspected with their owning tool.

## Coverage and evidence

- [Inventory](wiki-tools-audit-2026-10-06/inventory.json) records source hashes,
  functions, parameters and expensive-operation anchors. Automated inventory
  does **not** mark a file reviewed.
- [Reviews](wiki-tools-audit-2026-10-06/reviews.json) records complete-source
  inspection and specific performance/reliability observations for each file.
  Current checkpoint: **124 reviewed; 284 still pending**.
- [Findings](wiki-tools-audit-2026-10-06/findings.json) includes concrete triggers,
  consequences, test gaps, remediation and evidence references. All ten fixes
  have passed focused regression checks.
- [Coverage check](wiki-tools-audit-2026-10-06/Test-Coverage.ps1) rejects stale
  reviewed hashes, duplicate/missing scope entries and incomplete claims when
  `-FailOnIncomplete` is supplied. It checks evidence identity, not the truth
  of a reviewer's conclusions.

The baseline is `bca936eeb975345daa4a455a0e5fb14b49a6581a`. The ledger records
worktree content and explicitly distinguishes it from the committed baseline.
The remaining review scope has not been reduced to frequently used tools.

## First shared-runtime pass

1. Process-tree cleanup changed native exit state from failure **37 to 0** on
   both Windows shells. It now preserves failure, success and absent state in
   `finally`, while the owned-child termination test remains mandatory.
2. Identical JSON values containing an apostrophe produced different PS5/PS7
   fingerprints. The correction preserves ordinary existing PS7 hashes and
   converts only actual apostrophe escapes from the legacy serializer.
3. Source enumeration threw in PS5 because `.NET Framework` lacks
   `EnumerationOptions`. A compatible one-directory-at-a-time path now applies
   the same hidden/system/link/exclusion rules and propagates non-permission
   I/O failures. The optimized modern enumeration remains available.
4. Literal `\uABCD` and `\uabcd` strings produced the same JSON fingerprint.
   Escape normalization now distinguishes literal text from actual Unicode
   escape sequences.

The process, portable JSON and source-inventory regressions pass on installed
PowerShell **7.6.5** and **5.1.26100.9549**. An initial legacy source-inventory
test passed its assertions but failed during junction cleanup; nonrecursive
`Directory.Delete` fixed the fixture, and both full reruns passed. These are
Windows results; Linux execution is not claimed.

Counterbalanced modern source-enumeration runs returned exactly the same
**4,406 source paths**. Timings ranged from 1.09 to 2.69 seconds for the reference
and 1.12 to 1.30 seconds for the update; the first reference pass was cold. This
does **not** establish a speedup. Measurements and raw regression logs remain
under `.artifacts/wiki-tool-audit-20261006`.

## Second pass: cache, template scope and receipt integrity

- With `Retain=5`, twelve distinct writes left **5 results and 12 diagnostic
  metadata files**. Metadata is now pruned to the same Retain bound.
- A held reader lease caused retention to throw `IOException` **after the new
  result was successfully published**. Optional cache cleanup can therefore
  fail an otherwise successful query. Delete-sharing reads and deferred sharing-conflict cleanup now preserve publication and retry retention after the lease is released.

Both cases were reproduced in an owned fixture inside this worktree and the complete query-cache regressions pass on PS5/PS7. Conditional template scope moves now invalidate frontend contract navigation, and NUL-safe receipt fingerprints detect tracked/untracked Unicode edits with equal size/time. The focused index-selection and verification-receipt suites pass on PS7. Next,
inspect publication, pruning and caller error handling together, add meaningful
concurrency/retention regressions, and then continue the file-by-file review
through orchestration, generators, queries, governance, graph and MCP transport.

Full-tool audit completion remains unproven until every current source entry
has a fresh complete-source review and each reported finding is validated or
clearly classified as an unresolved suggestion/question. Focused tests do not
prove coverage of the remaining 284 files.

The first shared-runtime publication gate passed all eight stages in 136.21 seconds, including all three selected smoke groups. This proves that first change set only; later fixes receive their own gate. The full 408-source audit is still active.

## Generator and graph-helper pass

All thirteen generators have now received full-source inspection. This pass also covers the small Node graph libraries and the compiled reference, Roslyn and SQLite reader helpers. A cached architecture index describing drift now fails Check, and extracted module pages retain declared business edges. Both corrections have focused regressions. The main 2,987-line graph manager and the large MCP query/ranking implementations remain pending.

The next performance candidates to measure are pre-pruned forced C# inventory, shared generator content/newline maps, bounded per-group SQL result hydration, and eliminating diagnostics-only counts when diagnostics are disabled. They are proposals in the per-file ledger, not claimed speedups.
