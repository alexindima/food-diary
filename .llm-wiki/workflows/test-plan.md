---
id: workflow-test-plan
kind: workflow
status: current
title: Generate a change-aware test plan
summary: Select focused tests, required commands, and behavior/risk scenarios from the current diff.
tags:
  - workflow
  - testing
  - planning
sources:
  - docs/TESTING_STRATEGY.md
  - .llm-wiki/tools/Get-LlmWikiTestPlan.ps1
  - .llm-wiki/tools/Get-LlmWikiCoveragePlan.ps1
  - .llm-wiki/tools/LlmWikiVerificationReceipts.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationReceipts.ps1
  - .llm-wiki/policies/change-policies.json
---

# Generate a change-aware test plan

For line or branch coverage work with an exact test file, compile reproducible
commands before editing:

```powershell
./.llm-wiki/wiki.ps1 coverage-plan `
  -PlannedPath 'tests/FoodDiary.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs' `
  -Query 'cover the reported uncovered branches'
```

The result includes the focused `dotnet test` command, the repository XPlat
coverage command, and a dotCover invocation with an explicit target working
directory, assembly filters, and integration-test guidance.

When invoked through a task brief or change packet, the test planner reuses the
already classified diff and policy result instead of rescanning the same paths.

```powershell
./.llm-wiki/wiki.ps1 test-plan
```

Use `-ProposedPath <path>` before code exists; it may be repeated or supplied as
an array. The planner combines proposed and changed paths and preserves the
proposed paths in JSON output. When it resolves an existing Angular spec, its
focused command uses the project's `test:ci:*` script with Angular's supported
`--include` option.

The plan combines focused existing test files, executable commands, and scenario
prompts for backend, HTTP contracts, authorization, persistence, migrations,
frontend states, localization, security, and observability. Scenario prompts
still require the implementer to choose concrete inputs and assertions from the
changed behavior.

Focused tests are ranked by evidence: explicitly changed tests first, then a
changed production file's sibling spec, specs belonging to direct selector
consumers, tests that directly reference changed symbols, and finally broad
downstream context. JSON includes
`focusedTestDetails` with the rank reason. Use `-Compact` to retain actionable
tests, commands, and scenarios while reducing context volume.

Each focused test also carries an execution priority: `required` for changed
tests and direct siblings, `recommended` for direct component consumers and
symbol references, and `contextual` for broad downstream evidence.

Focused Angular commands are derived from the actual `package.json` script and
the project's `angular.json` test builder. `--include` is emitted only for the
verified `@angular/build:unit-test` builder; otherwise the planner falls back to
the full project test script and says why in `commandEvidence`.

Commands are grouped by obligation. `required` covers triggered policy and direct
owners, `recommended` covers close consumers, and `full-regression` is the broad
safety net normally delegated to pre-push or CI. The causal reason is printed
with every command.

When more than three production projects reference a changed contract, the
planner replaces noisy per-project builds with one recommended
`composition-confidence` solution build. Focused Application and Architecture
tests remain required for abstraction-boundary changes.

Record a completed check without creating tracked evidence files:

```powershell
./.llm-wiki/wiki.ps1 verification-record `
  -EvidenceCommand 'dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --no-restore' `
  -Status passed -DurationSeconds 24 -CoverageScope application-contract
```

Receipts live under the Git directory and are bound to the command, HEAD, and
the current non-generated worktree fingerprint. A later `test-plan` marks an
exact matching command as `satisfied` and exposes duration and coverage scope;
source changes make the receipt stale automatically. Use `verification-list`
to inspect current and stale receipts.
