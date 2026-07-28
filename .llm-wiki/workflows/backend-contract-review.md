---
id: workflow-backend-contract-review
kind: workflow
status: current
title: Review backend contract consumers
summary: Trace DTO, command, query, event, and interface changes through production and test consumers.
tags:
  - workflow
  - contracts
  - compatibility
sources:
  - .llm-wiki/generated/backend-contract-index.json
  - .llm-wiki/tools/Find-LlmWikiBackendContract.ps1
  - FoodDiary.Application.Abstractions/AGENTS.md
---

# Review Backend Contract Consumers

Query the changed type and inspect production consumers separately from test coverage. For in-process contracts, review constructors, properties, generic constraints, nullability, implementers, and DI registration.

For HTTP, message, or client-package boundaries, also review serialized names and types, required/default behavior, enum evolution, unknown fields, mixed-version compatibility, deployment order, and rollback.

```powershell
./.llm-wiki/wiki.ps1 contracts -BackendContractView production -Query <type>
./.llm-wiki/wiki.ps1 contracts -BackendContractView tests -Query <type>
./.llm-wiki/wiki.ps1 brief -ChangedPath <contract-path>
```
