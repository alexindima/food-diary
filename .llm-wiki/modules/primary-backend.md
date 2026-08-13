---
id: module.primary-backend
kind: module
status: current
sources:
  - FoodDiary.Application.Runtime/AGENTS.md
  - FoodDiary.Application.Abstractions/AGENTS.md
  - FoodDiary.Domain/AGENTS.md
  - FoodDiary.Infrastructure/AGENTS.md
  - FoodDiary.Integrations/AGENTS.md
  - FoodDiary.Presentation.Api/AGENTS.md
  - FoodDiary.Web.Api/AGENTS.md
  - docs/BACKEND_MODULE_MAP.md
  - docs/backend/BACKEND_MODULE_OWNERSHIP.md
  - docs/architecture/backend-modules.json
---

# Primary Backend

The primary backend is feature-first within an explicitly layered modular
monolith. Read the scoped `AGENTS.md` for every project touched by a change.

## Project Responsibilities

| Concern | Project |
| --- | --- |
| Domain behavior and invariants | `FoodDiary.Domain` |
| Application-facing ports and models | `FoodDiary.Application.Abstractions` |
| Cross-cutting application execution pipeline | `FoodDiary.Application.Runtime` |
| Business use cases | Owning `FoodDiary.Application.<Feature>` project |
| Extracted billing use cases | `FoodDiary.Application.Billing` |
| Extracted marketing use cases | `FoodDiary.Application.Marketing` |
| EF Core and technical implementations | `FoodDiary.Infrastructure` |
| External providers and service clients | `FoodDiary.Integrations` |
| HTTP and SignalR transport | `FoodDiary.Presentation.Api` |
| Composition, middleware, and hosting | `FoodDiary.Web.Api` |

The detailed placement table is canonical in the
[backend module map](../../docs/BACKEND_MODULE_MAP.md).

## Module Interaction

Sharing a `DbContext` does not grant shared write ownership. Cross-module
mutations go through the owning module, while composed reads use explicit
projection or read-service contracts. Ownership and the interaction allowlist
are defined in
[`BACKEND_MODULE_OWNERSHIP.md`](../../docs/backend/BACKEND_MODULE_OWNERSHIP.md).
The machine-readable inventory and cross-layer vocabulary mappings are defined
in [`backend-modules.json`](../../docs/architecture/backend-modules.json). Use
the generated module page to distinguish business API dependencies,
abstraction-contract dependencies, host consumers and boundary enforceability.

Prefer the narrowest repository contract suitable for a use case:

- read models for projections and summaries;
- lookup repositories for existence checks;
- read repositories for domain aggregate workflows;
- write repositories for tracked mutations.

## Common Change Path

For a backend feature, inspect in order:

1. Domain invariant and ownership.
2. Application command/query and abstractions.
3. Infrastructure or integration implementation.
4. Presentation request, response, mapping, and controller.
5. Host registration only when composition changes.
6. Unit, integration, contract snapshot, and architecture tests.
