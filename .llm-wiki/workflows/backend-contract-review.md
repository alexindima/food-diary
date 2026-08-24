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
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/tools/Test-LlmWikiBackendContractSqlParity.ps1
  - FoodDiary.Application.Abstractions/AGENTS.md
---

# Review Backend Contract Consumers

Query the changed type and inspect production consumers separately from test coverage. For in-process contracts, review constructors, properties, generic constraints, nullability, implementers, and DI registration.
Consumer edges distinguish `compile`, `mapping`, `serializer`, `http`, and
`test-fixture` usage. Prefer mapping/serializer/HTTP evidence for an additive
optional DTO field instead of treating every transitive compile consumer as an
equally likely behavioral dependency.

For HTTP, message, or client-package boundaries, also review serialized names and types, required/default behavior, enum evolution, unknown fields, mixed-version compatibility, deployment order, and rollback.

```powershell
./.llm-wiki/wiki.ps1 contracts -BackendContractView production -Query <type>
./.llm-wiki/wiki.ps1 contracts -BackendContractView tests -Query <type>
./.llm-wiki/wiki.ps1 brief -ChangedPath <contract-path>
```

The query command reads the refreshed SQLite `query_documents` projection by
default. Contract and consumer records preserve their source ordinal, while
`production`, `tests`, `ambiguous`, and `unconsumed` filters execute in SQL
before payload transport. A missing or stale projection fails explicitly with
the `graph-build` recovery command; `-CompiledIndexSource Json` is an explicit
parity/diagnostic baseline and is never selected automatically. The required
`backend-contract-query` smoke compares all seven views exactly, verifies the
source hash and payload reduction, and enforces a latency envelope. Source hash
comparison normalizes CRLF/LF so the same committed index remains current in an
isolated cross-platform snapshot.

Credential-bearing account-link commands need an additional security pass:
confirm current-user scoping, provider-identity uniqueness, email ownership,
idempotent retries, and refusal to replace a different existing identity.
