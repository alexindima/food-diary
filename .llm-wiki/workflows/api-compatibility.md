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
  - tests/FoodDiary.Web.Api.IntegrationTests/PresentationBoundaryIntegrationTests.cs
  - .llm-wiki/tools/Test-LlmWikiApiCompatibility.ps1
---

# Review API compatibility

After regenerating API contract snapshots, compare them with the intended base:

```powershell
./.llm-wiki/wiki.ps1 api-compat -BaseRef origin/master -FailOnBreaking
```

The guard understands both a raw OpenAPI document and this repository's compact
`Endpoints` contract snapshot. The full compact snapshot records component
schema properties used by request and response bodies in addition to query-parameter
name, location, requiredness, type, format, and default. The guard classifies
removed parameters, newly required parameters, requiredness increases, and
shape changes as breaking; new optional parameters are additive. It also compares serialized key sets in
`payload-contract-snapshots.json`, so response-field additions remain visible
when the compact endpoint snapshot has no component schemas. It reports removed paths, operations, documented
responses, and incompatible parameter changes as breaking. New paths, operations, optional parameters,
documented responses, and component schemas are additive. It is a
focused compatibility gate, not a substitute for integration tests or review of
schema semantics, authorization, error shapes, and status-code behavior.

Structural and behavioral compatibility are reported separately. For example,
adding a documented `413` response is not schema-breaking, but it is marked as
a `behavioral-restriction` because request sizes accepted by the previous
version may now be rejected. Such restrictions require an explicit behavior and
rollout review even when `-FailOnBreaking` remains green.

For authentication-provider linking, review the anonymous login operation and
the authenticated linking operation together. A new linking route may be
additive while newly documented `409` outcomes still require frontend handling
and updated focused/full OpenAPI snapshots.

Raw OpenAPI schema comparison classifies optional properties as additive and
removed properties, newly required properties, type/format/reference changes,
array item changes, and nullability changes as breaking.

For legacy compact snapshots without component schemas, the guard also compares
changed `*HttpModel.cs`, `*HttpRequest.cs`, and `*HttpResponse.cs`
primary-constructor properties. Nullable or defaulted
additions are additive; required additions, removals, requiredness changes, and
type changes are breaking. DTO findings include their source path as
provenance and complement rather than replace runtime serialization snapshots.
DTO declarations are parsed by the repository Roslyn extractor, including
attribute-based serialized names; method locals and comments cannot become
false HTTP properties.
