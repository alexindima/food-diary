---
id: workflow-task-workspace
kind: workflow
status: current
title: Start a governed AI task workspace
summary: Atomically initialize the complete set of planning, scope, acceptance, evidence, and review artifacts for a change.
tags:
  - workflow
  - task
  - bootstrap
  - governance
sources:
  - .llm-wiki/tools/Initialize-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Update-LlmWikiTaskEvidence.ps1
  - .llm-wiki/tools/Test-LlmWikiEvidenceLineage.ps1
  - .llm-wiki/tools/Manage-LlmWikiEvidenceCache.ps1
  - .llm-wiki/tools/Invoke-LlmWikiTaskChecks.ps1
  - .llm-wiki/tools/Complete-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskWorkspaces.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskGraph.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskSchedule.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskLease.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/tools/Export-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Import-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiWorkspacePolicy.ps1
  - .llm-wiki/tools/Compare-LlmWikiTaskPolicy.ps1
  - .llm-wiki/tools/Sync-LlmWikiTaskPolicy.ps1
  - .llm-wiki/policies/workspace-policies.json
  - .llm-wiki/tools/Manage-LlmWikiTaskJournal.ps1
  - .llm-wiki/tools/Test-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Update-LlmWikiTaskWorkspace.ps1
  - .llm-wiki/tools/Get-LlmWikiChangePacket.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskContract.ps1
  - .llm-wiki/tools/Manage-LlmWikiChangeManifest.ps1
  - .llm-wiki/tools/Manage-LlmWikiAcceptanceMatrix.ps1
  - .llm-wiki/tools/Manage-LlmWikiEvidence.ps1
---

# Start a Governed AI Task Workspace

Create the complete task workspace with one command:

```powershell
./.llm-wiki/wiki.ps1 task-start `
  -Objective "Safely evolve the fasting command" `
  -Criterion "Existing consumers remain compatible" `
  -Criterion "Invalid input is rejected" `
  -ChangedPath FoodDiary.Application/Fasting/Commands/StartFasting/StartFastingCommand.cs `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command
```

The command creates:

- compiled change packet;
- task scope contract;
- implementation manifest;
- acceptance matrix;
- evidence bundle;
- initial review report;
- workspace descriptor linking every artifact.

Initialization is staged in a temporary sibling directory and moved into place
only after every artifact succeeds. Existing workspaces are never overwritten.
When no changed paths exist yet, provide one or more `-AllowedPath` regular
expressions to define the intended scope.

Inspect actionable progress at any time:

```powershell
./.llm-wiki/wiki.ps1 task-status `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command
```

The status reports scope drift, fingerprint drift, unresolved acceptance
criteria, checks, reviews, blocking readiness dimensions, and a concrete
next-action list. Use `-Format Json` for automation and `-FailOnBlocked` as a
gate.

Refresh derived context after the implementation changes:

```powershell
./.llm-wiki/wiki.ps1 task-refresh `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command
```

Refresh updates only the compiled packet, review report, and workspace
metadata. It deliberately preserves the task contract, manifest, acceptance
decisions, and collected evidence.

Preview or execute the required automated checks:

```powershell
./.llm-wiki/wiki.ps1 task-run `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -DryRun

./.llm-wiki/wiki.ps1 task-run `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -FailOnFailure
```

`task-run` executes pending checks by default, writes one log per check, records
the result in evidence, and refreshes the workspace. `-CheckId` selects a
subset, `-IncludePassed` reruns successful checks, and `-ContinueOnFailure`
collects all outcomes instead of stopping after the first failure.

Commands are never trusted merely because they appear in `evidence.json`.

Create `context-bundle.json` with `task-context-create` when a task needs a
bounded working set. `task-context-verify` detects source, scope, policy,
generator, or bundle drift, while `task-context-compare` exposes how two task
contexts differ.

For policy-driven deduplication and fail-fast ordering, compile an immutable
verification plan with `task-verification-plan`, validate it with
`task-verification-verify`, and execute or preview it with
`task-verification-run`. The plan covers every required check exactly once and
is invalidated by task scope or policy drift.
Before execution, the runner requires an exact match with the current policy
and a narrow command-family allowlist. A modified or injected command is
rejected even in dry-run mode.

Finish only after every strict readiness dimension passes:

```powershell
./.llm-wiki/wiki.ps1 task-finish `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command

./.llm-wiki/wiki.ps1 task-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -FailOnInvalid
```

`task-finish` refuses conditional or blocked workspaces. For a ready workspace
it performs a final refresh, records Git HEAD and readiness, hashes every
governance artifact, and writes `completion.json` plus a human-readable
`completion.md`. An existing seal is never overwritten.

`task-verify` recomputes both the completion fingerprint and every sealed
artifact hash. It detects missing, modified, or substituted artifacts after
completion and can act as a CI gate with `-FailOnInvalid`.

List all local AI task workspaces:

```powershell
./.llm-wiki/wiki.ps1 task-list
./.llm-wiki/wiki.ps1 task-list -Detailed -Format Json
```

The fast view reads stored artifacts and reports `in-progress`, `sealed`,
`invalid-seal`, or `incomplete`, together with outstanding acceptance,
automated checks, reviews, changed-path count, and last activity. `-Detailed`
recomputes live readiness and next actions for active workspaces. Abandoned
atomic-staging directories are reported separately instead of being treated as
valid tasks.

Create a bounded context package when handing the task to another agent,
developer, or session:

```powershell
./.llm-wiki/wiki.ps1 task-handoff `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -Limit 20 `
  -OutputPath .artifacts/llm-wiki/tasks/fasting-command/handoff.md
```

The handoff carries the objective, authority reminder, continuity fingerprint,
live readiness, scope and drift, applicable instructions, high-value context
pages, acceptance state, checks, reviews, next actions, and exact resume
commands. `-Limit` bounds path/page expansion so the result stays suitable for
an LLM context window; `-Format Json` provides the same contract to automation.

Export a portable, privacy-filtered handoff contract:

```powershell
./.llm-wiki/wiki.ps1 task-export `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command

./.llm-wiki/wiki.ps1 task-export-verify `
  -ExportPath .artifacts/llm-wiki/exports/fasting-command.task-export.json `
  -FailOnInvalid
```

The export is a whitelist rather than a copy of the workspace: arbitrary
files, check logs, migration backups, and source contents are excluded.
Potential credentials, tokens, private keys, embedded URL credentials, and
email addresses are redacted recursively from every exported string. The
package records redaction categories and is sealed with SHA-256;
`task-export-verify` independently recomputes the seal and scans for remaining
sensitive patterns. `-FailOnSensitive` refuses to write when any redaction was
needed, and existing exports require explicit `-Overwrite`.

Resume a verified export as a fresh local task:

```powershell
./.llm-wiki/wiki.ps1 task-import `
  -ImportPath .artifacts/llm-wiki/exports/fasting-command.task-export.json `
  -WorkspacePath .artifacts/llm-wiki/tasks/resumed-fasting `
  -DryRun

./.llm-wiki/wiki.ps1 task-import `
  -ImportPath .artifacts/llm-wiki/exports/fasting-command.task-export.json `
  -WorkspacePath .artifacts/llm-wiki/tasks/resumed-fasting
```

Import verifies the export before reading it and builds fresh packet,
manifest, acceptance, evidence, and review artifacts from the current
repository. Previously satisfied criteria and checks are deliberately reset:
portable evidence is context, not proof about the new environment. Source
provenance and continuity SHA are stored in `workspace.json`; journal entries
are replayed with source IDs, and open source blockers remain blockers.
Creation uses a hidden staging workspace and an atomic final move. Truncated
scope is rejected unless `-AllowPartialScope` is explicit, and `-SkipJournal`
omits source journal replay while retaining the import provenance event.

Workspace governance defaults live in one versioned policy:

```powershell
./.llm-wiki/wiki.ps1 workspace-policy -FailOnInvalid
```

`.llm-wiki/policies/workspace-policies.json` owns the current workspace format
and artifact schema versions, audit SLAs, export limits and path boundary,
redaction patterns and replacement modes, plus import path/staging/fail-closed
defaults. Init, doctor, migration, list, audit, export, verification, and
import all read the same validated policy. The main `verify` command validates
this contract before running any other wiki gate; malformed regexes, duplicate
redaction IDs, unsupported replacement modes, missing artifact keys, unsafe
import defaults, or a schema bump without a migration step fail immediately.

Every schema-v4 workspace records both the exact policy fingerprint and the
accepted policy snapshot that governed its initialization or last explicit
policy acceptance. `task-doctor`,
`task-list`, and `task-audit` expose a mismatch as `policy-drift`, separately
from corruption and schema migration:

```powershell
./.llm-wiki/wiki.ps1 task-policy-impact `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command

./.llm-wiki/wiki.ps1 task-policy-sync `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -DryRun

./.llm-wiki/wiki.ps1 task-policy-sync `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -AcceptPolicyImpact
```

The impact report compares policy leaves by stable JSON path, classifies each
addition, removal, or changed value by governance area and severity, marks
whether it affects the active task, and derives the checks that should be
repeated. Redaction-policy changes are critical; workspace, audit, and export
changes affect active work; import-only changes remain visible without
pretending that they invalidate the current implementation.

Sync is allowed only for an unsealed workspace on the latest schema when every
doctor check except the fingerprint already passes. It updates the descriptor,
stores the newly accepted snapshot, records semantic impact and old/new
fingerprints in an append-only decision, reruns the doctor, and rolls both
files back on failure. Sealed workspaces retain their historical policy
provenance and must be resumed as a new task instead.
When at least one change affects the active task, the mutating command also
requires explicit `-AcceptPolicyImpact`; a dry run never requires acceptance.

Task refresh performs dependency-aware evidence invalidation:

```powershell
./.llm-wiki/wiki.ps1 task-refresh `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -DryRun

./.llm-wiki/wiki.ps1 task-refresh `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command
```

The preview reports checks, reviews, and acceptance criteria that would become
stale without modifying the workspace. A real refresh compares each obligation
by source policy rule, definition, and matched-path set. Unaffected completed
evidence is retained; new, removed, or changed obligations are rebuilt;
criteria mapped to invalidated evidence return to `pending`; and unanchored
resolutions are conservatively reopened when the packet changes. Every reason
is appended to `evidence.json` invalidation history with old/new packet
fingerprints. Packet, evidence, acceptance, review report, and descriptor
updates roll back together if any refresh stage fails. The append-only task
journal is never rewritten by refresh.

Resolved evidence is retained only when its lineage still matches the content
hash of the policy-rule paths it covers. A same-path source edit therefore
invalidates stale evidence even when the rule and changed-path set are
unchanged.

Parallel workspaces are compiled into an executable dependency graph. Exact
path overlap blocks merge; shared boundaries require coordination; module and
contract relationships produce deterministic prerequisite edges and merge
waves. Use `task-graph -FailOnBlocked` before merging concurrent AI tasks.

`task-schedule` turns the graph into conflict-free agent lanes. Atomic
task leases reserve those lanes, expire without manual recovery, and are
visible in list, audit, and handoff context.

Record durable task-local knowledge as append-only journal events:

```powershell
./.llm-wiki/wiki.ps1 task-note `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -JournalType decision `
  -Text "Keep the public command shape stable" `
  -Reason "Production consumers depend on it"

./.llm-wiki/wiki.ps1 task-note `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -JournalType blocker `
  -Text "Compatibility has not been demonstrated"

./.llm-wiki/wiki.ps1 task-resolve-note `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -NoteId J-0002 `
  -Resolution "Production and test consumers were reviewed"
```

Journal types are `decision`, `assumption`, `blocker`, `learning`, and `note`.
Resolution appends a new event instead of rewriting history. Open blockers
become a non-compensating `task-journal` readiness blocker, are included in
handoffs and task-list summaries, and prevent task completion. `task-journal
-Check -FailOnInvalid` validates event ordering, IDs, types, and resolution
targets. The journal itself is part of the completion seal.

Diagnose cross-artifact corruption before trusting a workspace:

```powershell
./.llm-wiki/wiki.ps1 task-doctor `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -FailOnInvalid
```

The doctor verifies required files and JSON schemas, canonical artifact paths,
objective and Git-base consistency, current/initial fingerprints, the manifest
plan fingerprint, acceptance mapping references, evidence coverage, journal
event integrity, abandoned temporary files, and the completion seal when
present. `task-list` runs the same fast doctor and surfaces a corrupt workspace
as `incomplete` or `invalid-seal` instead of trusting its descriptor.

Upgrade an older unsealed workspace before resuming it:

```powershell
./.llm-wiki/wiki.ps1 task-migrate `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command `
  -DryRun

./.llm-wiki/wiki.ps1 task-migrate `
  -WorkspacePath .artifacts/llm-wiki/tasks/fasting-command
```

Workspace schema v2 introduced the stable format name and per-artifact schema
versions; v3 adds policy fingerprint provenance. Migration is stepwise and
idempotent, refuses
non-canonical descriptor paths and future versions, and will not mutate a
sealed workspace. Unless explicitly disabled at the tool level, it stores the
original mutable metadata under `.migration-backups/` before writing. If
validation fails, the descriptor and any newly created journal are rolled
back. `task-list` reports old workspaces as `migration-required`.

Audit long-lived workspaces against explicit freshness SLAs:

```powershell
./.llm-wiki/wiki.ps1 task-audit `
  -StaleAfterDays 7 `
  -EvidenceMaxAgeDays 3

./.llm-wiki/wiki.ps1 task-audit `
  -FailOnAttention `
  -Format Json
```

The audit classifies each task as `healthy`, `attention`, `stale`, `sealed`,
`migration-required`, or `invalid`. It measures inactivity, compiled-context
age, and the age of resolved evidence; verifies that the task Git base still
resolves; and detects when repository HEAD moved after the current packet was
compiled. Results include exact remediation commands. `-AsOfUtc` makes SLA
evaluation deterministic for automation and tests, while `-FailOnAttention`
turns every non-healthy, non-sealed state into a CI failure.
