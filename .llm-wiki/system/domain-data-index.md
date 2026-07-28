---
id: system.domain-data-index
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiDomainDataIndex.ps1
sources:
  - .llm-wiki/generated/domain-data-index.json
---

# Domain and Data Index

This generated index connects domain types and explicit guard clauses to EF Core mappings, tables, indexes, and relationships.

Query business rules before changing an entity or value object:

```powershell
./.llm-wiki/wiki.ps1 domain -DomainView invariants -Query weight
./.llm-wiki/wiki.ps1 domain -DomainView mappings -Query User
./.llm-wiki/wiki.ps1 domain -DomainView indexes -Query Email
```

The index is navigation evidence, not a substitute for reading the implementation, tests, migrations, and PostgreSQL behavior.
