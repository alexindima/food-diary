---
id: workflow-context-bundles
kind: workflow
status: current
title: Build bounded task context bundles
summary: Select compact, explainable, redacted, integrity-protected context for one AI task and bind it to dispatch execution.
tags:
  - workflow
  - context
  - task
  - orchestration
sources:
  - .llm-wiki/tools/Manage-LlmWikiContextBundle.ps1
  - .llm-wiki/tools/Find-LlmWikiContext.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskDispatch.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskDecomposition.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextFeedback.ps1
  - .llm-wiki/tools/Manage-LlmWikiQualityAdjustment.ps1
  - .llm-wiki/tools/Manage-LlmWikiMemory.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskSchedule.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Build bounded task context bundles

Every bundle is protected by the [context security](context-security.md) trust and
prompt-injection assessment before excerpts are persisted.

Create a task-specific bundle after its packet is current:

```powershell
./.llm-wiki/wiki.ps1 task-context-create `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

The selector combines mandatory task scope with semantic discovery:

- root and scoped `AGENTS.md`;
- every changed path, including planned files that do not exist yet;
- Wiki pages selected by diff impact;
- focused tests from the task packet;
- active durable memories whose scope matches the changed paths;
- semantically related Wiki pages, guides, C# or frontend symbols, ranked
  tracked implementation files, and tests.

When planned frontend paths are available, implementation-file discovery stays
inside those paths and records whether relevance came from the path, source
content, or both. Context bundles can therefore prefer concrete implementation
sources without widening the declared task scope.

Every item records its kind, score, inclusion reasons, existence state, source
SHA-256, and a bounded excerpt. Generic task verbs are removed from the semantic
query so terms such as `change` do not dominate relevance. The total context
budget is distributed across selected items, and sensitive export patterns are
redacted before excerpts are persisted.

Mandatory context is never silently dropped. If it exceeds the item budget,
bundle creation fails and the task should be decomposed or assigned a larger
explicit limit.

Verify provenance and currentness:

```powershell
./.llm-wiki/wiki.ps1 task-context-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

Verification binds the bundle to the task packet, workspace policy, generator
implementation, item budgets, bundle hash, source existence, and source content
hashes. Scope, policy, generator, file, or metadata drift invalidates it.

Compare two task contexts:

```powershell
./.llm-wiki/wiki.ps1 task-context-compare `
  -SourceWorkspacePath .artifacts/llm-wiki/tasks/<source> `
  -WorkspacePath .artifacts/llm-wiki/tasks/<target>
```

The comparison reports Jaccard overlap plus added and removed paths. This makes
context changes observable instead of treating prompt assembly as hidden state.

Decomposition creates a bundle for every child shard. Dispatch creates or
refreshes a valid bundle before acquiring work and stores its path and hash in
the receipt. A missing, edited, stale, or regenerated bundle moves an active
dispatch to `context-drift`; scheduling and audit then stop treating it as
healthy work.

## Learn from terminal work

After a dispatch completes or fails, its owner can record which issued paths
were helpful or noisy and which repository-relative paths were missing:

```powershell
./.llm-wiki/wiki.ps1 task-context-feedback `
  -DispatchId <id> `
  -Owner <agent> `
  -HelpfulContextPath <path> `
  -NoisyContextPath <path> `
  -MissingContextPath <path> `
  -Reason <observation>

./.llm-wiki/wiki.ps1 task-context-feedback-metrics -FailOnInvalid
```

Feedback is accepted only for a terminal dispatch and its exact context hash.
The owner must match. Helpful/noisy paths must have been delivered, missing
paths must not have been delivered, contradictory labels are rejected, and one
dispatch can write only one immutable hashed receipt.

Ranking changes only after the configured minimum number of independent
terminal samples. Helpful paths gain relevance, noisy paths lose relevance,
and repeatedly missing paths become recovery candidates. Adjustments are
capped, the feedback snapshot fingerprint is stored in every new bundle, and
invalid receipts block learning rather than being silently ignored.

## Attribute later rework and recovery

Quality can change after a dispatch has completed. Record later rework,
rollback, regression, or successful recovery against the original dispatch:

```powershell
./.llm-wiki/wiki.ps1 task-quality-adjustment `
  -DispatchId <id> `
  -Owner <agent> `
  -QualityAdjustmentType rollback `
  -Reason <observation> `
  -QualityEvidence <evidence-reference>

./.llm-wiki/wiki.ps1 task-quality-adjustment-metrics -FailOnInvalid
```

Each event uses a policy-defined delta and is bound to the immutable terminal
feedback hash and dispatch event hash. Receipts are append-only, integrity
protected, capped per dispatch, and included in owner and capability quality
profiles. Invalid attribution blocks learned routing and appears in task audit
and handoff output.

The same terminal feedback receipt captures a protected quality snapshot:
verification resolution contributes 45%, acceptance 25%, review resolution
15%, and a completion seal 15%; failed dispatches score zero. Owner and
capability quality profiles are aggregated across receipts and contribute to
future agent routing after the normal minimum-sample threshold. This separates
“the agent returned successfully” from “the result was actually ready.”
