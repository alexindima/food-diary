---
id: workflow.evidence-bundle
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Manage-LlmWikiEvidence.ps1
  - .llm-wiki/tools/New-LlmWikiEvidenceLineage.ps1
  - .llm-wiki/tools/Test-LlmWikiEvidenceLineage.ps1
  - .llm-wiki/tools/Get-LlmWikiContentFingerprint.ps1
  - .llm-wiki/tools/Manage-LlmWikiEvidenceCache.ps1
  - .llm-wiki/tools/Invoke-LlmWikiTaskChecks.ps1
  - .llm-wiki/tools/Test-LlmWikiChangePolicy.ps1
  - .llm-wiki/policies/change-policies.json
---

# Development Evidence Bundle

Evidence bundles record what a change required, what was verified, and which
review decisions remain unresolved. They live under ignored `.artifacts/` and
are not committed.

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

./.llm-wiki/wiki.ps1 evidence-validate
./.llm-wiki/wiki.ps1 task-lineage `
  -WorkspacePath .artifacts/llm-wiki/tasks/my-task `
  -FailOnInvalid
./.llm-wiki/wiki.ps1 handoff
```

`not-applicable` always requires a reason. Validation succeeds only when all
policy-required checks and review obligations are resolved. The generated
Markdown summary is suitable for task handoff or a pull-request description,
but it remains supporting evidence rather than a substitute for CI logs.

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

`task-lineage` and `evidence-validate` recompute the current policy,
requirement definition, dependency path set, and file-content fingerprint.
Missing, edited, or context-incompatible lineage makes the evidence invalid.
Compatible entries are reported as reusable; refresh archives their previous
compatibility fingerprint whenever a source change invalidates them.

Executed task checks bind lineage to the SHA-256 hash of their captured log.
Cross-task reuse is deliberately narrower than general lineage compatibility:

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
