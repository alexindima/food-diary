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
---

# Review Architecture Drift and Removal Candidates

Dependency violations, ungoverned production projects, and module cycles are enforced failures. Update the matrix only when the dependency is intentional and architecturally justified.

Unreferenced selectors and contracts are investigation candidates only. Before removal, search routes, dynamic imports, dependency injection, reflection, serializers, message type names, external client packages, templates, tests, and documentation. Remove a candidate only with focused compilation/tests and observable behavior evidence.
