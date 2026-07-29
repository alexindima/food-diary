---
id: workflow-quality-risk
kind: workflow
status: current
title: Review structural hotspots and test gaps
summary: Prioritize complex files, critical symbols without direct test references, and explicit debt markers.
tags:
  - workflow
  - quality
  - testing
sources:
  - .llm-wiki/generated/quality-index.json
  - .llm-wiki/tools/Find-LlmWikiQualityRisk.ps1
---

# Review structural hotspots and test gaps

```powershell
./.llm-wiki/wiki.ps1 hotspots -Limit 20
./.llm-wiki/wiki.ps1 test-gaps -Query Billing
./.llm-wiki/wiki.ps1 debt
```

Use hotspots to choose review depth and refactoring candidates. Use test gaps to
find nearby tests and verify whether behavior is covered indirectly before adding
new tests. Never describe name-reference matching as real code coverage.

For account linking, cover the success path, provider validation failure, email
mismatch, identity owned by another user, idempotent retry, and refusal to
replace a different linked identity. Frontend coverage should include the
explanation state, post-password linking, success/failure navigation, and
accessible status announcement.
