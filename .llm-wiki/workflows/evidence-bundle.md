---
id: workflow.evidence-bundle
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Manage-LlmWikiEvidence.ps1
  - .llm-wiki/tools/Recover-LlmWikiEvidenceExecution.ps1
  - .llm-wiki/tools/LlmWikiEvidenceCommands.ps1
  - .llm-wiki/tools/New-LlmWikiEvidenceLineage.ps1
  - .llm-wiki/tools/Test-LlmWikiEvidenceLineage.ps1
  - .llm-wiki/tools/LlmWikiSubstantiveReview.ps1
  - .llm-wiki/tools/Test-LlmWikiBehaviorWorkflow.ps1
  - .llm-wiki/tools/Get-LlmWikiContentFingerprint.ps1
  - .llm-wiki/tools/Manage-LlmWikiEvidenceCache.ps1
  - .llm-wiki/tools/Import-LlmWikiEvidenceReceipts.ps1
  - .llm-wiki/tools/LlmWikiVerificationReceipts.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationReceipts.ps1
  - .llm-wiki/tools/Invoke-LlmWikiTaskChecks.ps1
  - .llm-wiki/tools/Resolve-LlmWikiRecordedCheckResult.ps1
  - .llm-wiki/tools/Test-LlmWikiChangePolicy.ps1
  - .llm-wiki/policies/change-policies.json
---

# Development Evidence Bundle

Evidence bundles record what a change required, what was verified, and which
review decisions remain unresolved. They live under ignored `.artifacts/` and
are not committed.

If a command finished but publication was interrupted, retain its original log
and `.log.execution.json`. Use `wiki.ps1 evidence-recover -EvidencePath <bundle>
-Id <check> -ExecutionRecordPath <original-record>` to restore its actual outcome
without rerunning it. Recovery checks the canonical command, checkout, original
log hash and unchanged whole-repository fingerprints. It preserves failed exit
codes, rejects stale or modified artifacts and rolls back evidence if publication
fails. A manually written pass label does not recover an execution.

`evidence-review -Id substantive-code-review -Status completed -Reason <scope>
-ReviewContent <object>` records requirement conclusions and code-quality findings
inside this same bundle. It does not replace policy review obligations. Source,
scope, HEAD and acceptance definition/mapping fingerprints prevent stale reuse;
open behavior gaps and major/critical findings invalidate delivery evidence.
The content schema and small-route usage are documented in
[the canonical review guide](../../docs/ai/CODE_REVIEW.md#substantive-review-record).

Required checks retain the canonical policy command in `command`; the actual
invocation is recorded separately in `executionCommand` and its lineage. Output
location and logger flags do not redefine a test. `--no-build` needs a successful
build receipt for the exact current repository state and the same test project
or FoodDiary solution. Changed projects, filters and shell chains cannot satisfy
the original requirement.

An unknown `evidence-check` ID with an explicit command becomes a
`supplementalChecks` entry. Supplemental observations have their own content and
execution lineage, but never satisfy a required check or its reusable cache.
Use a distinct ID; do not rename an obligation to avoid it. `evidence-artifact
-Id` identifies an existing review obligation, while `acceptance-map -ReviewId`
accepts multiple review IDs. Command-specific help shows these distinctions.

```powershell
./.llm-wiki/wiki.ps1 evidence-init

./.llm-wiki/wiki.ps1 evidence-run -Id architecture-tests

./.llm-wiki/wiki.ps1 evidence-check `
  -Id architecture-tests `
  -Status passed `
  -DurationSeconds 18

./.llm-wiki/wiki.ps1 evidence-review `
  -Id api-contract-snapshots `
  -Status not-applicable `
  -Reason "No Swagger-visible surface changed."

./.llm-wiki/wiki.ps1 evidence-artifact `
  -Id frontend-visual-evidence `
  -OutputPath .artifacts/browser/mobile-layout.png `
  -EvidenceKind screenshot `
  -Reason "Responsive interaction verified at the mobile breakpoint."

./.llm-wiki/wiki.ps1 evidence-validate
./.llm-wiki/wiki.ps1 task-evidence-import `
  -WorkspacePath .artifacts/llm-wiki/tasks/my-task
./.llm-wiki/wiki.ps1 task-lineage `
  -WorkspacePath .artifacts/llm-wiki/tasks/my-task `
  -FailOnInvalid

./.llm-wiki/wiki.ps1 handoff
```

SQLite is the only compiled-index query provider. Generated JSON snapshots remain
inputs for the sole Node projection writer and reviewable Git artifacts. Queries
validate exact source hashes, select bounded records in SQL, and report explicit
recovery errors when preparation fails. Backend-only preparation works without
TypeScript; frontend code-graph discovery requires the locked npm dependencies.
Direct behavior tests cover identity, selection, scope, freshness and output bounds.

`not-applicable` always requires a reason. Use
`passed-with-known-baseline-failures` only when the check ran and its reason names
the failures plus the evidence that they predate the change. It is resolved
evidence, but remains visibly distinct from a clean pass and is never eligible for
executed-check cache reuse. Validation succeeds only when all
policy-required checks and review obligations are resolved. The generated
Markdown summary is suitable for task handoff or a pull-request description,
but it remains supporting evidence rather than a substitute for CI logs.

`evidence-artifact` hashes an existing screenshot, browser log, accessibility
report, or video inside the repository workspace, links it to a pending review
obligation, and completes that review with lineage-backed provenance. Summaries
show the artifact path and SHA-256, so browser work performed earlier in the
task no longer has to be represented only by a manual text attestation.

`evidence-run` executes the trusted command declared by the matched repository
policy and records its exit status and duration automatically. It resolves the
current policy again immediately before execution, requires an exact
ID/command match, and applies a narrow command-family allowlist. Editing
`evidence.json` therefore cannot introduce an arbitrary shell command.

Every resolved check and review carries a lineage envelope. It records the
repository commit, base ref, source policy rule, canonical definition and
command fingerprints, dependency paths and their content hashes, change-policy
fingerprint, runtime name/version, execution outcome or attestation reason,
and a sealed compatibility fingerprint. Manual statuses and not-applicable
reviews use the same provenance contract as executed checks.
Derived navigation and review-registry files under `.llm-wiki/generated/` and
`.llm-wiki/reviews/` are excluded from implementation dependency fingerprints,
so refreshing Wiki bookkeeping cannot invalidate otherwise current proof.

`task-lineage` and `evidence-validate` recompute the current policy,
requirement definition, dependency path set, and file-content fingerprint.
Missing, edited, or context-incompatible lineage makes the evidence invalid.
Compatible entries are reported as reusable; refresh archives their previous
compatibility fingerprint whenever a source change invalidates them.

`task-evidence-import` restores pending checks from content-addressed verification
receipts that match the current repository fingerprint and canonical command. Use
`-DryRun` to preview the import. Task refresh performs the same import
automatically after evidence invalidation.

Direct `evidence-run` captures an immutable execution log and the repository
fingerprint measured before and after the command. A schema 2 verification
receipt binds that original execution record, current executed-check lineage,
canonical command and log hash. Import copies the verified log and preserves
the actual exit status and duration without rerunning the command. The source
workspace need not be sealed for this receipt import. Changed execution inputs,
commands or logs prevent reuse; schema 1 manual receipts remain manual checks
and cannot prove a behavioral phase.

Executed task checks bind lineage to the SHA-256 hash of their captured log.
The separate task-cache reuse route is deliberately narrower than verification
receipt import and general lineage compatibility:

```powershell
./.llm-wiki/wiki.ps1 task-cache-find `
  -WorkspacePath .artifacts/llm-wiki/tasks/target `
  -CheckId architecture-tests

./.llm-wiki/wiki.ps1 task-cache-reuse `
  -WorkspacePath .artifacts/llm-wiki/tasks/target `
  -CheckId architecture-tests `
  -DryRun

./.llm-wiki/wiki.ps1 task-cache-reuse `
  -WorkspacePath .artifacts/llm-wiki/tasks/target `
  -CheckId architecture-tests
```

Only a `passed` executed check from a valid sealed workspace is eligible.
Manual checks, reviews, failed runs, unsealed workspaces, changed commands,
different policy/runtime/platform/content fingerprints, missing logs, and
log-hash mismatches are excluded. Reuse copies the verified log into the target,
preserves source completion and compatibility fingerprints in a reuse chain,
records a journal decision, revalidates target lineage, and rolls back evidence,
journal, and copied log together on failure.

The task runner intentionally excludes `wiki-verify` from verification telemetry.
That check validates the tracked telemetry registry, so recording itself would
mutate a verified input and invalidate the check during the same workspace refresh.
Its status, duration, log hash, and evidence lineage remain recorded normally.
Task-run success is derived from that recorded terminal evidence status rather
than ambient process `$LASTEXITCODE`, which can be overwritten by lineage work.
