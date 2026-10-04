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

The refreshed quality projection follows the ranking-core extraction and new guard, scheduler, confidence, and cancellation regressions. Its direct-reference signals remain static navigation evidence; use executed test results to assess actual coverage.

```powershell
./.llm-wiki/wiki.ps1 hotspots -Limit 20
./.llm-wiki/wiki.ps1 test-gaps -Query Billing
./.llm-wiki/wiki.ps1 debt
./.llm-wiki/wiki.ps1 hotspots -QualityArea Wiki
```

Use hotspots to choose review depth and refactoring candidates. Use test gaps to
find nearby tests and verify whether behavior is covered indirectly before adding
new tests. Never describe name-reference matching as real code coverage.
Each result is explicitly classified as `direct-test-reference-absent`, carries
medium confidence and identifies its evidence as static symbol-name matching.
Integration, dynamic, reflection-based, or differently named tests may still
cover the behavior; `test-gaps` is an investigation queue, never proof of
missing execution coverage.

The default facade view is product-only and excludes `.llm-wiki/` records so a
repository review is not dominated by Wiki implementation complexity. The same
index still measures the Wiki's own non-test PowerShell tools: select them with
`-QualityArea Wiki`, or use `-QualityArea All` for a combined view. As with
application symbols, a missing direct reference is an investigation lead rather
than proof that no behavioral coverage exists.

SQLite is the only compiled-index query provider. Generated JSON snapshots remain
inputs for the sole Node projection writer and reviewable Git artifacts. Queries
validate exact source hashes, select bounded records in SQL, and report explicit
recovery errors when preparation fails. Backend-only preparation works without
TypeScript; frontend code-graph discovery requires the locked npm dependencies.
Direct behavior tests cover identity, selection, scope, freshness and output bounds.

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
