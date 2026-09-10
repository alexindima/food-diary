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
  - .llm-wiki/tools/LlmWikiModuleTestRoots.ps1
  - .llm-wiki/tools/LlmWikiProjectLookup.ps1
  - .llm-wiki/tools/Test-LlmWikiProjectLookup.ps1
  - .llm-wiki/tools/Test-LlmWikiToolStartup.ps1
  - .llm-wiki/tools/Get-LlmWikiCoveragePlan.ps1
  - .llm-wiki/tools/LlmWikiVerificationReceipts.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationReceipts.ps1
  - .llm-wiki/policies/change-policies.json
---

# Generate a change-aware test plan

The fast graph plan recognizes `Test-*.ps1` scripts as tests and discovers an
existing same-directory `Test-<name>.ps1` companion for standard PowerShell
implementation verbs. These are navigation candidates, not execution evidence.

Changes to `.github/workflows/ci-tests.yml` select `BuildWorkflowGuardrailTests`
and its filtered architecture-test command. The fast graph plan also retains
this test path. An empty full plan explicitly warns that coverage evidence is
missing; it does not imply that verification can be skipped.

For line or branch coverage work with an exact test file, compile reproducible
commands before editing:

```powershell
./.llm-wiki/wiki.ps1 coverage-plan `
  -PlannedPath 'Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs' `
  -Query 'cover the reported uncovered branches'
```

The result includes the focused `dotnet test` command, the repository XPlat
coverage command, and a dotCover invocation with an explicit target working
directory, assembly filters, and integration-test guidance.

When invoked through a task brief or change packet, the test planner reuses the
already classified diff and policy result instead of rescanning the same paths.
The selected `CompiledIndexSource` is propagated through nested diff, brief, and
implementation-plan calls; a cold backend-only checkout therefore remains on
the JSON baseline instead of unexpectedly requiring SQLite midway through the
plan.

```powershell
./.llm-wiki/wiki.ps1 test-plan
```

Use `-ProposedPath <path>` before code exists; it may be repeated or supplied as
an array. The planner combines proposed and changed paths and preserves the
effective proposed paths in JSON output. Empty or whitespace-only proposed paths
do not select the repository root for recursive test discovery. The planner preserves the
same focused selection rules. When it resolves an existing Angular spec, its
focused command uses the project's `test:ci:*` script with Angular's supported
`--include` option. An existing backend test-project directory also contributes
its C# tests and exact `.csproj`; that project is emitted as a required focused
command even when it lives under a module-owned `Modules/<Module>/tests` tree.

The plan combines focused existing test files, executable commands, and scenario
prompts for backend, HTTP contracts, authorization, persistence, migrations,
frontend states, localization, security, and observability. Scenario prompts
still require the implementer to choose concrete inputs and assertions from the
changed behavior.

Repository-wide audit intent selects a representative assessment plan instead
of pretending the current Git diff is the audit scope. It includes architecture,
authentication, webhook signature/freshness and replay, idempotency/concurrency,
outbox/recovery, provider-backed persistence and migration safety, container
supply-chain, MailRelay/MailInbox, and frontend auth/data-flow tests. Explicit
scenarios cover architecture, security/privacy, reliability, contracts/data,
client/CI/operations, webhook authenticity, migrations, deployment, and
dependency inventory. Commands omit `--no-restore` where a cold checkout must be
usable and publish detected .NET, npm, Docker, and provider-test prerequisites.

Focused tests are ranked by evidence: explicitly changed tests first, then tests
inside an explicitly planned test directory, a changed production file's sibling
spec, specs belonging to direct selector
consumers, tests that directly reference changed declared types before common
method names, behavior-
specific tests selected from intent such as idempotency/retry/replay, neighboring
test classes, and finally broad downstream context. Planned paths receive the
same symbol analysis as already changed paths. JSON includes
`focusedTestDetails` with the rank reason. Use `-Compact` to retain actionable
tests, commands, and scenarios while reducing context volume.

Consumer discovery batches large changed-symbol sets into bounded Git grep
patterns. Large refactors therefore keep the same direct-consumer evidence
without exceeding the Windows process command-line limit.
Nearest-project lookup memoizes visited directories within that discovery pass,
including directories with no owning project. Nested projects still take
precedence; every new invocation starts with an empty lookup so project creation
and removal are observed without relying on persisted cache invalidation.
Neighbor-test discovery also reads each directory's first four candidate files
once per invocation. Each direct test still applies its own self-exclusion to
that list, preserving the candidate order and the final union of neighbors.

When a changed or planned Wiki tool uses a known repository antipattern, the
planner searches the whole tool family and returns every match in
`repositoryAntipatterns`. This prevents a fix from stopping after the first
duplicate fixed-depth repository-root traversal.

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
  -EvidenceCommand 'dotnet test Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/FoodDiary.Modules.Admin.Application.Tests.csproj --no-restore' `
  -Status passed -DurationSeconds 24 -CoverageScope application-contract
```

Receipts live under the Git directory and are bound to the command, HEAD, and
the current non-generated worktree fingerprint. A later `test-plan` marks an
exact matching command as `satisfied` and exposes duration and coverage scope;
source changes make the receipt stale automatically. Use `verification-list`
to inspect current and stale receipts.
