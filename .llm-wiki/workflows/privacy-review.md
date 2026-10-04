---
id: workflow-privacy-review
kind: workflow
status: current
title: Review sensitive data lifecycle
summary: Find candidate sensitive fields and review collection, authorization, storage, sharing, logging, export, retention, and deletion.
tags:
  - workflow
  - privacy
  - sensitive-data
sources:
  - .llm-wiki/generated/sensitive-data-index.json
  - .llm-wiki/tools/Find-LlmWikiSensitiveData.ps1
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/tools/Test-LlmWikiSensitiveDataQueries.ps1
  - docs/backend/PERSONAL_DATA_LIFECYCLE.md
  - docs/privacy/PRIVACY_RELEASE_CHECKLIST.md
---

# Review sensitive data lifecycle

Context CLI and MCP share a read-only ranking core; Node alone writes the local source-derived SQLite graph. Command coalescing retains no query payload in telemetry and does not cancel an owner when a waiter cancels. Publication receipt states do not replace privacy source review.

```powershell
./.llm-wiki/wiki.ps1 privacy -PrivacyCategory credential
./.llm-wiki/wiki.ps1 privacy -PrivacyCategory logging
./.llm-wiki/wiki.ps1 privacy -PrivacyCategory boundaries -Query Export
./.llm-wiki/wiki.ps1 privacy `
  -PlannedPath 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result'
./.llm-wiki/wiki.ps1 privacy -NoImplicitScope
./.llm-wiki/wiki.ps1 privacy -RepositoryWide
```

The default `all` view no longer emits an arbitrary repository-wide first
page. It scopes itself to a non-wiki Git diff when available; otherwise it
returns summary counts and a copyable scoping hint. Explicit planned paths are
ranked first, while related cross-layer candidates require multiple matching
terms. For example, an AI photo path can still surface the external OpenAI
image boundary without flooding the result with every image-named field.
Use `-NoImplicitScope` for deterministic automation that must ignore unrelated
working-tree changes and require an explicit query or planned path.
Use `-RepositoryWide` when the broad inventory is intentional; it cannot be
combined with `-PlannedPath`/`-ScopePath`.
Broad natural-language audit intent is expanded to this bounded inventory rather
than filtered by generic words such as "audit", "project", or "vulnerability".
The JSON result reports `queryMode`, selection status, candidate/returned counts,
and an abstention/recovery hint when a focused filter is empty.

SQLite is the only compiled-index query provider. Generated JSON snapshots remain
inputs for the sole Node projection writer and reviewable Git artifacts. Queries
validate exact source hashes, select bounded records in SQL, and report explicit
recovery errors when preparation fails. Backend-only preparation works without
TypeScript; frontend code-graph discovery requires the locked npm dependencies.
Direct behavior tests cover identity, selection, scope, freshness and output bounds.

For a changed field or flow, review purpose/minimization, consent or lawful
basis, ownership/authorization, encryption and secret handling, cache/queue/log
copies, provider sharing, export, retention/deletion, backups, telemetry, and
user-facing disclosure. Confirm every candidate against source semantics.
Plain fields named `Token` are classified as credential candidates as well as
more specific access, refresh, and hash forms; callers must still confirm the
field's semantics in source.
Plain `Amount` names require an explicit monetary name or a billing/payment
path context, so food quantities and unrelated measurements do not create
financial-review noise.
Constructors, `CancellationToken` primitives, Roslyn syntax nodes, and SQL/query
text constants are excluded before classification; these are parser artifacts,
not credential, identity, or financial fields.

External identity credentials used to bridge an anonymous login attempt into an
authenticated linking request should remain in memory only. Do not place them
in URLs, router state persisted across reloads, browser storage, logs,
telemetry, queues, or error messages.

The index also reports `externalTransfers`: clients in module Infrastructure
or legacy Integrations folders that combine
an absolute external HTTP destination with image, prompt, description, text,
food, nutrition, or similar sensitive parameters. Treat these entries as
provider-sharing review leads. Verify the actual payload, consent, retention,
logging, metadata, and provider policy in source.
