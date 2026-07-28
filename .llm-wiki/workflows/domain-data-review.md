---
id: workflow-domain-data-review
kind: workflow
status: current
title: Review domain invariants and persistence contracts
summary: Trace changed domain rules through construction, mutation, persistence, migration, and tests.
tags:
  - workflow
  - domain
  - database
sources:
  - .llm-wiki/generated/domain-data-index.json
  - .llm-wiki/tools/Find-LlmWikiDomainData.ps1
  - FoodDiary.Domain/AGENTS.md
  - FoodDiary.Infrastructure/AGENTS.md
---

# Review Domain Invariants and Persistence Contracts

For domain changes, inspect all constructors, factories, mutation methods, deserialization/persistence paths, and tests. Test valid boundaries and adjacent invalid values. Preserve aggregate ownership and legal state transitions.

For mapping changes, compare the domain property with EF nullability, key, conversion, uniqueness, relationship, delete behavior, and concurrency configuration. Confirm a migration is present when the physical schema changes, then exercise it against the real provider.

Use the generated index to locate connected rules and mappings, then verify against code and tests:

```powershell
./.llm-wiki/wiki.ps1 domain -Query <entity>
./.llm-wiki/wiki.ps1 brief -ChangedPath <path>
./.llm-wiki/wiki.ps1 test-plan -ChangedPath <path>
```
