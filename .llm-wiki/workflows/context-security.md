---
id: workflow-context-security
kind: workflow
status: current
title: Protect agent context from instruction injection
summary: Classify source trust, detect prompt-injection patterns, quarantine untrusted instructions, and bind the assessment to context bundles.
sources:
  - .llm-wiki/tools/Manage-LlmWikiContextSecurity.ps1
  - .llm-wiki/tools/Manage-LlmWikiContextBundle.ps1
  - .llm-wiki/policies/workspace-policies.json
---

# Protect agent context from instruction injection

Context sources are not equally authoritative. `AGENTS.md` files are explicit agent
instructions. Wiki and project documentation are governed context. Source files,
tests, generated output, and task artifacts are data unless a policy trust zone
says otherwise.

```powershell
./.llm-wiki/wiki.ps1 task-context-security-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-security-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>
./.llm-wiki/wiki.ps1 task-context-security-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid
```

Context-bundle creation scans the exact selected source set. High-risk instruction
overrides, role overrides, secret-exfiltration requests, and tool coercion found in
non-authoritative sources are replaced with quarantine markers. The file remains in
the bundle as evidence and retains its source hash; quarantining never silently
changes repository content.

`context-security.json` binds the packet, policy, scanner implementation, source
hashes, trust classification, findings, and summary into an integrity-protected
assessment. `context-bundle.json` records that assessment hash and per-item trust.
