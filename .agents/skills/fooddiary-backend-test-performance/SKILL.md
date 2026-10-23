---
name: fooddiary-backend-test-performance
description: Measure and improve FoodDiary .NET test-suite runtime using comparable executions and preserved behavior. Use for slow backend tests or fixture performance; coverage percentages belong to fooddiary-backend-coverage.
---

# FoodDiary backend test performance

Read `Tooling/Testing/AGENTS.md`, the owning test guide and
`docs/TESTING_STRATEGY.md`. Separate restore/build, discovery, fixture startup,
individual execution and cleanup before choosing an optimization.

Freeze measurement inputs: test identities/outcomes, binaries and source
revision/delta, configuration, framework, filters, parallelism, runner, Docker
images and profiler/coverage state. Use repository-level `--artifacts-path`.
Do not compare profiled/unprofiled or cold/warm executions without stating that
difference. Match final binaries to current sources before using `--no-build`.

Capture actual exit codes, TRX and raw stdout/stderr. Take enough comparable runs
to distinguish an improvement from machine load; report samples and median/range
without imposing an arbitrary universal repetition count. Reuse the existing
backend coverage execution/check helpers when their assembly-based mode fits;
do not create another runner merely for timing.

Inspect the bottleneck. Prefer owned fixtures, readiness signals, correct pooling
and disposal, and removal of redundant setup. Preserve provider-backed
integration, transaction isolation, migrations and cross-module saves. A faster
suite with fewer tests, more skips, weaker assertions or hidden failures does not
establish an improvement.

Inspect the exact prerequisites of Docker performance attributes; timing checks
may skip under a profiler. Recheck timing without coverage and behavior with the
owning integration suite. Resolve process identity before stopping anything;
a stale log alone does not prove termination.

Finish with matched before/after test identities/outcomes, measurement inputs,
observed timing change, behavior checks and remaining variability. Follow the
applicable Wiki and architecture checks for fixture or project changes.
