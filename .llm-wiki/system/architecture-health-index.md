---
id: system.architecture-health-index
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiArchitectureHealthIndex.ps1
sources:
  - .llm-wiki/generated/architecture-health-index.json
  - tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs
  - docs/architecture/module-dependencies.json
---

# Architecture Health Index

This generated index compares actual project references with the executable dependency matrix and detects ungoverned production projects and module-cycle nodes.

It also aggregates review candidates from other graphs: ambiguous or unconsumed backend contracts, selectors without a static template consumer, direct-spec gaps, critical symbols without direct test references, and explicit debt markers.

```powershell
./.llm-wiki/wiki.ps1 health -HealthView drift
./.llm-wiki/wiki.ps1 health -HealthView dead-candidates
./.llm-wiki/wiki.ps1 health -HealthView test-gaps
```

Candidate lists are evidence for investigation, not proof that code is dead. Routes, dynamic component creation, reflection, serialization, dependency injection, generated consumers, and external packages can create valid runtime use that text indexing cannot observe.
