---
id: workflow-api-compatibility
kind: workflow
status: current
title: Review API compatibility
summary: Classify OpenAPI snapshot changes as additive or potentially breaking before handoff.
tags:
  - workflow
  - api
  - compatibility
sources:
  - tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/openapi-full-contract.json
  - .llm-wiki/tools/Test-LlmWikiApiCompatibility.ps1
---

# Review API compatibility

After regenerating API contract snapshots, compare them with the intended base:

```powershell
./.llm-wiki/wiki.ps1 api-compat -BaseRef origin/master -FailOnBreaking
```

The guard understands both a raw OpenAPI document and this repository's compact
`Endpoints` contract snapshot. It also compares serialized key sets in
`payload-contract-snapshots.json`, so response-field additions remain visible
when the compact endpoint snapshot has no component schemas. It reports removed paths, operations, documented
responses, and newly required parameters as breaking. New paths, operations,
documented responses, and component schemas are additive. It is a
focused compatibility gate, not a substitute for integration tests or review of
schema semantics, authorization, error shapes, and status-code behavior.

For authentication-provider linking, review the anonymous login operation and
the authenticated linking operation together. A new linking route may be
additive while newly documented `409` outcomes still require frontend handling
and updated focused/full OpenAPI snapshots.

Raw OpenAPI schema comparison classifies optional properties as additive and
removed properties, newly required properties, type/format/reference changes,
array item changes, and nullability changes as breaking.
