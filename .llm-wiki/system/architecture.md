---
id: system.architecture
kind: system
status: current
sources:
  - docs/ARCHITECTURE.md
  - docs/BACKEND_MODULE_MAP.md
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
  - tests/FoodDiary.ArchitectureTests/BackendModuleManifestTests.cs
  - tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs
---

# System Architecture

FoodDiary combines a primary modular monolith with separately deployed mail
services and adapter hosts. The complete runtime inventory and layer rules live
in [the architecture document](../../docs/ARCHITECTURE.md).

## Runtime Shape

- `FoodDiary.Web.Api` hosts the primary ASP.NET Core API.
- `FoodDiary.Web.Client` is the Angular web client.
- `FoodDiary.JobManager` runs scheduled and background work.
- `FoodDiary.Telegram.Bot` is a separate transport adapter.
- MailRelay and MailInbox have separate hosts and databases.
- PostgreSQL, RabbitMQ, and Redis provide persistence, messaging, and caching.

## Boundary Model

Primary backend dependency direction is inward:

```text
Host -> Presentation / Application modules / Infrastructure / Integrations
Presentation -> Application modules
Application modules -> Abstractions / Domain
Infrastructure and Integrations -> Abstractions / Domain
```

The executable project-reference allowlist is enforced by
[`ProjectDependencyMatrixTests`](../../tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs).
The folder-module API graph is stored in
[`module-dependencies.json`](../../docs/architecture/module-dependencies.json).
The unified inventory, ownership, cross-layer mappings, physical isolation and
enforceability for all 39 folder modules plus Billing and Marketing live in
[`backend-modules.json`](../../docs/architecture/backend-modules.json). Generated
module pages keep business API edges, abstraction contracts and host/composition
consumers separate and explicitly label analysis limitations.

## Placement Rule

Use the [backend module map](../../docs/BACKEND_MODULE_MAP.md) before placing
backend code. Hosts are composition roots, presentation owns HTTP transport,
application owns use cases, infrastructure owns persistence implementations,
integrations owns external adapters, and domain owns business invariants.
