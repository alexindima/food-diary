---
id: index
kind: index
status: current
sources:
  - AGENTS.md
  - docs/README.md
  - docs/ARCHITECTURE.md
  - docs/BACKEND_MODULE_MAP.md
---

# FoodDiary Knowledge Index

FoodDiary is a modular monolith with separately deployed supporting services.
This index routes agents to compiled context; the linked sources remain
authoritative.

## System

- [Knowledge model](system/knowledge-model.md) — authority, provenance, and
  update rules for this wiki.
- [Architecture](system/architecture.md) — deployable units, layers, and the
  main dependency boundaries.
- [Repository catalog](system/repository-catalog.md) — generated project,
  module, HTTP, test, and documentation inventory.
- [C# symbol index](system/csharp-symbol-index.md) — handlers, validators,
  repositories, services, domain types, and DI registrations.
- [Frontend index](system/frontend-index.md) — Angular features, routes,
  symbols, tests, and localization pairs.

## Areas

- [Primary backend](modules/primary-backend.md) — core FoodDiary backend
  projects and module interaction.
- [Application module index](generated/modules/index.md) — generated pages for
  all modules with dependencies, consumers, endpoints, source areas, and tests.
- [Mail services](modules/mail-services.md) — MailRelay and MailInbox service
  boundaries.
- [Frontend](modules/frontend.md) — Angular application, admin application,
  UI kit, and tour engine.

## Workflows

- [Query repository context](workflows/query-context.md) — build a compact,
  ranked context packet for a module or change type.
- [Review change context](workflows/review-diff.md) — infer affected modules,
  guides, wiki pages, risks, and checks directly from a Git diff.

## Canonical Entrypoints

- [Repository instructions](../AGENTS.md)
- [Documentation index](../docs/README.md)
- [Architecture](../docs/ARCHITECTURE.md)
- [Backend module map](../docs/BACKEND_MODULE_MAP.md)
- [Testing strategy](../docs/TESTING_STRATEGY.md)
- [ADR index](../docs/adr/README.md)

## Query Protocol

1. Open the page matching the task area.
2. Follow its `sources` and inline links.
3. Read the nearest applicable `AGENTS.md`.
4. Confirm change-sensitive facts in code, tests, manifests, or configuration.
5. If the wiki conflicts with stronger evidence, follow the stronger evidence
   and mark or update the wiki page.
