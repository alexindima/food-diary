---
id: module.primary-backend
kind: module
status: current
sources:
  - FoodDiary.Application.Runtime/AGENTS.md
  - Shared/FoodDiary.Application.Contracts/AGENTS.md
  - Shared/FoodDiary.Audit.Contracts/AGENTS.md
  - Shared/FoodDiary.Authentication.Contracts/AGENTS.md
  - Shared/FoodDiary.Email.Contracts/AGENTS.md
  - Shared/FoodDiary.Nutrition.Contracts/AGENTS.md
  - Shared/FoodDiary.Outbox.Management.Contracts/AGENTS.md
  - Shared/FoodDiary.Domain.Primitives/AGENTS.md
  - docs/adr/0027-retire-shared-domain-assemblies.md
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
| Domain behavior and invariants | Owning module Domain project; generic values and guards in `FoodDiary.Domain.Primitives`. The former central and Nutrition assemblies are retired under ADR 0027. |
| Application-facing ports and models | Owning module contracts plus narrow shared contract projects under `Shared/` |
| Cross-cutting application execution pipeline | `FoodDiary.Application.Runtime` |
| Business use cases | Owning module Application project under `Modules/<Module>/Application`; legacy assembly names may remain |
| Billing use cases | `Modules/Billing/Application` |
| Marketing use cases | `Modules/Marketing/Application` |
| Body measurements | `Modules/BodyMetrics` application, Domain, ports, repositories and mappings; Users-owned identity seam |
| AI use cases, usage and prompt ownership | `Modules/Ai` application, ports, Domain, persistence model and adapters; Users profile contracts, shared context and external Integrations provider seams |
| Admin orchestration and impersonation | `Modules/Admin`: application, ports, independent impersonation domain, adapters and explicit persistence model; Identity-owned email templates, Users-owned roles/audit and shared SSO seams retain their owners |

| Product catalog and mutation ownership | `Modules/Products` Domain, application, ports/contracts, persistence model, adapters and focused tests; accepted Users/USDA dependencies and the shared Recipe composition lock remain explicit |
| Meal diary aggregate ownership | `Modules/Meals/Domain` owns Meal, items, AI sessions/items, IDs and meal-only value types; one-way Meal.User references Users-owned User/UserId; shared context and migrations remain central |
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
