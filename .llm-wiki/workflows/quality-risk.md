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
