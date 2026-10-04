---
id: workflow-architecture-health-review
kind: workflow
status: current
title: Review architecture drift and removal candidates
summary: Enforce dependency direction and investigate unreferenced code without unsafe automatic deletion.
tags:
  - workflow
  - architecture
  - drift
  - dead-code
sources:
  - .llm-wiki/generated/architecture-health-index.json
  - .llm-wiki/tools/Find-LlmWikiArchitectureHealth.ps1
  - .llm-wiki/tools/Measure-LlmWikiStandaloneIndexRoutes.ps1
  - .llm-wiki/tools/Get-LlmWikiCompiledIndexMigration.ps1
  - Tooling/tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs
  - docs/architecture/backend-modules.json
---

# Review Architecture Drift and Removal Candidates

Published index presence, complete generation, and full verification have separate evidence. Use `catalog -CheckFreshness -Format Json` for publication receipts; a graph's current snapshot fingerprint alone does not certify this architecture projection.

Dependency violations, ungoverned production projects, and module cycles are enforced failures. Update the matrix only when the dependency is intentional and architecturally justified.

Module fan-in/fan-out hotspots are classified separately. `review-candidate`
means inspect coupling and public-surface growth; it is not automatically a
policy violation. The index also reports the unified module inventory and
the role of each hotspot, so read composers are not judged like aggregate owners.

Unreferenced selectors and contracts are investigation candidates only. Before removal, search routes, dynamic imports, dependency injection, reflection, serializers, message type names, external client packages, templates, tests, and documentation. Remove a candidate only with focused compilation/tests and observable behavior evidence.

Angular standalone components referenced by `component` or `loadComponent` in
`*.routes.ts` are counted as routed components and excluded from the
selector-unreferenced list. The remaining list can still contain dynamic dialog,
portal, registry, or reflective consumers and therefore remains a removal lead,
never an automatic deletion list.

Adding an in-process authentication command that reuses existing application
services should not require new project edges. Confirm this through the
architecture-health index and architecture tests rather than treating a clean
handler dependency list as sufficient proof.

SQLite is the only compiled-index query provider. Generated JSON snapshots remain
inputs for the sole Node projection writer and reviewable Git artifacts. Queries
validate exact source hashes, select bounded records in SQL, and report explicit
recovery errors when preparation fails. Backend-only preparation works without
TypeScript; frontend code-graph discovery requires the locked npm dependencies.
Direct behavior tests cover identity, selection, scope, freshness and output bounds.

`./.llm-wiki/wiki.ps1 health -HealthView all` returns every health category in
one response. Use a narrower view when only dependency drift, allowances,
untracked projects, cycles, ambiguous contracts, dead candidates, specification
gaps, test gaps, or debt markers are relevant.
