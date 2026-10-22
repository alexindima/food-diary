# Backend coverage diagnostics and helpers

Use a new task directory below the current checkout's `.artifacts/`. Examples use
`python` and `pwsh`; resolve actual installed Python 3.10+ and PowerShell 7+ tools.
No helper installs tools, builds the solution, edits IDE settings or contacts a
provider. Inputs, output paths, test scope and production filters remain explicit.

## Compare reports

```powershell
python .agents/skills/fooddiary-backend-coverage/scripts/analyze_reports.py local.json `
  --baseline reference.xml --repo . --include 'FoodDiary.*' `
  --exclude '*.Tests' --exclude '*.IntegrationTests' --exclude 'FoodDiary.Testing' `
  --output .artifacts/my-coverage/comparison.json
```

These are analysis-only filters. Review the intended production assembly set;
neither a namespace glob nor a historical project count proves complete scope.
The helper reports missing/added assemblies and changed statement totals. It
rejects ambiguous identities and inconsistent trees instead of guessing how to
combine different framework/build variants. Unresolved MSBuild property expressions
need an evaluated project-to-assembly identity check outside the helper.

## Freeze and run selected built test assemblies

Build the intended checkout/configuration first, retaining the command and result.
Use root `.artifacts/` for isolated builds as the root guide specifies. Resolve the
current dotCover executable and inspect `dotCover help cover` when its CLI differs.

```powershell
$tests = @('absolute/path/to/Owner.Tests.dll')
./.agents/skills/fooddiary-backend-coverage/scripts/run_tests.ps1 `
  -TestAssembly $tests -OutputDirectory .artifacts/my-coverage/run -Mode both `
  -DotCoverExecutable 'absolute/path/to/dotCover.exe' `
  -RunSettings coverage.runsettings -ExcludeAssembly '*.Tests' `
  -SourceFile 'path/to/changed/source.cs'
```

Pass the current approved runtime filters, not just the abbreviated example.
`-FfmpegPath` sets `FOODDIARY_TEST_FFMPEG` for this script's child processes and
restores its prior value. Other prerequisites are inherited; verify Docker and
PowerShell availability only for suites that require them. The runner does not
add JIT flags, suppress test failures or replace the existing repository gates.

Input hashing covers selected DLL/PDB/deps/runtimeconfig files and FoodDiary DLL/PDB
copies in those test outputs. Different copies fail before execution. Explicit
source files are hashed too; missing source selection is disclosed. The runner
does not freeze third-party dependencies or establish that binaries are freshly
built from source. Supplement its receipts when those identities matter.

## Validate actual execution

Read `execution.json` for actual exit codes; do not substitute zero after a failure.

```powershell
python .agents/skills/fooddiary-backend-coverage/scripts/check_run.py `
  --trx .artifacts/my-coverage/run/normal-results --process-exit-code 0
python .agents/skills/fooddiary-backend-coverage/scripts/check_run.py `
  --trx .artifacts/my-coverage/run/profile-results --process-exit-code 0 --mode profile `
  --collector-log .artifacts/my-coverage/run/profile.log `
  --snapshot .artifacts/my-coverage/run/profile.dcvr `
  --baseline-trx .artifacts/my-coverage/run/normal-results
```

For intentional skips, supply `--skip-reasons` with reviewed current test names:

```json
{
  "skippedTests": {"Exact fully qualified test display name": "Current prerequisite and evidence"},
  "profileOnlySkips": {"Exact latency test display name": "Passes ordinary; its attribute skips profilers"}
}
```

Ordinary/profile comparison uses identity multisets, not aggregate totals alone.
Profile-only skips require an ordinary baseline where that same case passed. Empty
TRX, incomplete counters, failed processes, collector errors and unreviewed skips
fail validation. Passing this helper still does not establish production scope,
source/build identity or a valid cross-platform merge.
Skip counts come from individual result rows: VSTest/xUnit can leave the aggregate
`Counters.notExecuted` at zero even when `UnitTestResult` contains skipped cases.

## When Rider differs from CLI

Confirm the actual coverage session/view and compare runtime filters, test DLLs and
symbols. Read the current session's profiler logs; green test results do not prove
successful instrumentation. Search for `Inconsistent PDB`,
`CORE_API_E_UNRECOVERABLE_ERROR` and `Coverage session finished with errors`.

A reproduced incident involved invalid statement offsets while instrumenting
`FsCheck.Xunit.dll` and `FsCheck.dll`. Tests passed, but the profiler stopped
collecting coverage for that host. If current logs identify that same failure,
compare runs over the same binaries with `FsCheck*` excluded from **runtime
instrumentation**. Confirm tests still run and the missing production members become
covered. This is a conditional workaround, not a permanent exclusion for FoodDiary
code. A result-tree filter cannot repair failed instrumentation. The historical
member count, SDK version and machine paths are not invariants.

In Rider, runtime filters are under Settings > Build, Execution, Deployment >
dotCover > Filtering > Do not analyze code in assemblies. Preserve existing filters
and do not edit IDE state as part of a read-only investigation. JetBrains distinguishes
[runtime and result filters](https://www.jetbrains.com/help/rider/dotCover__Setting_up_Coverage_Filters.html)
and documents [missing-assembly coverage diagnostics](https://dotnettools-support.jetbrains.com/hc/en-us/articles/206545919-Coverage-info-for-some-assemblies-is-missing).

For skipped media tests, inspect their `FOODDIARY_TEST_FFMPEG` guard. For PowerShell
tests, verify `pwsh` is visible to the IDE's test process, which can have a different
PATH from the agent shell. Re-read current attributes before changing prerequisites.
Keep intentional profiler timing skips and isolated Linux networking separate.

## Cross-platform evidence

Read current native test fixtures and verify network isolation before running them.
Do not route public-looking test addresses to real public services. Keep both raw
snapshots. A merged report requires matching application sources, production scope,
assembly/symbol identities and member statement totals; explain any deliberate debug
path normalization. Do not reuse historical snapshots after source changes or call
a Windows-only result equivalent to a Windows/Linux union. These helpers analyze
and validate evidence; they do not automate native setup or snapshot merging.
