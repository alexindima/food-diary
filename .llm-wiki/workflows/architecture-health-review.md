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
  - tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs
  - docs/architecture/backend-modules.json
---

# Review Architecture Drift and Removal Candidates

Dependency violations, ungoverned production projects, and module cycles are enforced failures. Update the matrix only when the dependency is intentional and architecturally justified.

Module fan-in/fan-out hotspots are classified separately. `review-candidate`
means inspect coupling and public-surface growth; it is not automatically a
policy violation. The index also reports the unified 41-module inventory and
the role of each hotspot, so read composers are not judged like aggregate owners.

Unreferenced selectors and contracts are investigation candidates only. Before removal, search routes, dynamic imports, dependency injection, reflection, serializers, message type names, external client packages, templates, tests, and documentation. Remove a candidate only with focused compilation/tests and observable behavior evidence.

Adding an in-process authentication command that reuses existing application
services should not require new project edges. Confirm this through the
architecture-health index and architecture tests rather than treating a clean
handler dependency list as sufficient proof.
