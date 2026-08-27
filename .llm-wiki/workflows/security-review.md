---
id: workflow-security-review
kind: workflow
status: current
title: Compile security-review evidence
summary: Rank security-sensitive source boundaries, test-reference signals, runtime declarations, and privacy leads without turning navigation evidence into a security verdict.
tags:
  - workflow
  - security
  - testing
  - runtime
sources:
  - .llm-wiki/tools/Find-LlmWikiSecurityReview.ps1
  - .llm-wiki/policies/context-search-ranking.json
  - .llm-wiki/generated/quality-index.json
  - .llm-wiki/generated/runtime-topology.json
  - .llm-wiki/generated/sensitive-data-index.json
---

# Compile security-review evidence

```powershell
./.llm-wiki/wiki.ps1 security
./.llm-wiki/wiki.ps1 security -Query 'Mailgun webhook replay idempotency'
./.llm-wiki/wiki.ps1 security -Query 'WebPush SSRF DNS rebinding'
```

The default command runs a small curated discovery set for outbound WebPush
validation, webhook replay/authenticity, browser token persistence/CSP, and
nginx transport configuration. A query narrows the same evidence compiler to
one concern. Results combine ranked current-source context, security-oriented
critical-symbol test references, repository runtime declarations, and privacy
inventory leads.

This command is not a vulnerability scanner. A context candidate is a place to
inspect, not a finding. A direct test-name reference is not proof that a
security property is executed or asserted. Compose and source declarations do
not prove effective production exposure, IAM/grants, DNS behavior, certificate
validation, or replay/idempotency. Validate leads in current code and tests,
then obtain runtime/provider evidence before reaching a security conclusion.
