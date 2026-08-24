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
  - .llm-wiki/tools/Manage-LlmWikiCodeGraph.ps1
  - .llm-wiki/tools/Get-LlmWikiCompiledIndexMigration.ps1
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
Each result is explicitly classified as `direct-test-reference-absent`, carries
medium confidence and identifies its evidence as static symbol-name matching.
Integration, dynamic, reflection-based, or differently named tests may still
cover the behavior; `test-gaps` is an investigation queue, never proof of
missing execution coverage.

Standalone quality queries already use the SQLite `query_documents` projection;
the generated JSON is retained as its projection source, not as an automatic
runtime fallback. The compiled-index migration report classifies this route as
fully migrated alongside task-brief impact selection.

For account linking, cover the success path, provider validation failure, email
mismatch, identity owned by another user, idempotent retry, and refusal to
replace a different linked identity. Frontend coverage should include the
explanation state, post-password linking, success/failure navigation, and
accessible status announcement.

When account linking becomes user-discoverable in settings, add direct component
and facade tests for connected, unconnected, loading/unavailable, success, and
failure states. Pair those tests with desktop/mobile rendered evidence and with
payload/OpenAPI snapshots for any provider-status field added to the user
response.
