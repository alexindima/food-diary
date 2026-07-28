---
id: workflow-durable-memory
kind: workflow
status: current
title: Promote task learnings into durable memory
summary: Turn evidence-backed task decisions into integrity-protected, scoped, expiring knowledge that future context bundles can reuse.
tags:
  - workflow
  - memory
  - decisions
  - context
sources:
  - .llm-wiki/knowledge/memories.json
  - .llm-wiki/tools/Manage-LlmWikiMemory.ps1
  - .llm-wiki/tools/Manage-LlmWikiTaskJournal.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextBundle.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskHandoff.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskAudit.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Promote task learnings into durable memory

Task journals remain task-local. Promote only a decision or learning that should
constrain later work, has an explicit rationale, and has verification evidence:

```powershell
./.llm-wiki/wiki.ps1 memory-candidates `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>

./.llm-wiki/wiki.ps1 memory-promote `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -NoteId J-0003 `
  -Id preserve-public-command-shape `
  -MemoryScopePath '^FoodDiary\.Application/' `
  -MemoryTag architecture `
  -MemoryEvidence 'architecture-tests passed'
```

Candidate scoring is deterministic: journal type establishes the base score,
an explicit rationale adds confidence, and passed checks or completed reviews
add evidence weight. The result recommends `promote`, `keep-task-local`, or
`reuse-or-supersede`, and is included in task handoffs and fleet audits.

Before promotion, token-set similarity is compared with every active memory.
Entries over the policy threshold are rejected as duplicates. Exceptional
duplicates require both `-AllowMemoryDuplicate` and an explicit `-Reason`, which
is sealed into the promoted event for later review.

The append-only registry records the source workspace, journal entry, packet
fingerprint, evidence, scope regexes, source hashes, review deadline, previous
event hash, and event hash. Only `decision` and `learning` entries can be
promoted. Missing rationale or evidence fails closed.

Inspect and validate the registry:

```powershell
./.llm-wiki/wiki.ps1 memory-list
./.llm-wiki/wiki.ps1 memory-verify -FailOnInvalid
./.llm-wiki/wiki.ps1 memory-relevant `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>
```

Text output is concise for interactive use; `-Format Json` exposes the complete
machine-readable lifecycle view and fingerprints.

A memory becomes stale when a captured source changes or its review deadline
expires. Stale knowledge is reported by task audit and is excluded from context
bundles. Replace obsolete knowledge with a new promoted entry, then preserve
history by superseding the old one:

```powershell
./.llm-wiki/wiki.ps1 memory-supersede `
  -Id preserve-public-command-shape `
  -Reason 'Replaced by the versioned command contract decision.'
```

Relevant active memories are embedded directly into a task context bundle,
redacted with the same policy as source excerpts, and covered by the bundle
hash. This keeps the selected decision compact and prevents unrelated registry
content from leaking into the task.
