# Dispatch registry reuse in audit and handoff

Baseline: local commit `3dce67e13`. Audit and handoff previously read/validated the
dispatch registry directly, then repeated that work inside dispatch metrics.
Both now request the existing `-IncludeDispatchRegistry` view from metrics,
extract the validated registry and remove the internal member before publishing
their ordinary output. Registry classification and metrics share one invocation
and one clock value. Every later invocation reads and validates again.

No validation, history retention, invalid-receipt attention, SLO calculation or
required handoff assessment is removed. The change adds no persistent or global
cache. It uses the existing documented metrics option rather than accepting
arbitrary prevalidated input from a caller.

An owned mini-repository uses the real dispatch manager and metrics code with
200 valid terminal receipts. Only policy delivery and an empty lease source are
controlled collaborators. At a fixed UTC time, ABBA composition measurements
were 2.384/2.112 seconds before and 1.228/1.447 after, with exact registry and
metrics JSON parity. The measured portion is about 40.5% faster; this is not a
general claim of the same speedup for the entire audit or handoff. An initial
fixture encoded event times with a local offset, failed hash validation and was
excluded; the corrected fixture follows the manager's UTC event representation.

Handoff regressions execute its real public composition script and assert one
metrics call without a direct dispatch read. Audit regressions preserve invalid
dispatch and SLO attention, reject registry failures and ensure the internal
registry does not appear in public metrics. Existing metrics regressions retain
out-of-window/invalid records, UTC bucket boundaries and fresh reads. Consumer
changes select those regressions through the facade group.

Raw measurements and final verification are under
`.artifacts/wiki-audit-reuse-20261008/`. `final-results.json` records source
identity, platform checks and final status. Production and the main `D:\FD`
checkout are outside this followup's mutation scope.
