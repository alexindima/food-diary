# Module Guidelines

## Scope

Rules for all logical modules under `Modules/`. A module-specific `AGENTS.md` may add stricter ownership rules.

## Physical boundaries

- Keep module-owned HTTP controllers, request/response DTOs, mappings, and presentation-only processors in the module's `Presentation` project.
- A module Presentation project may reference the shared `FoodDiary.Presentation.Api` HTTP kernel and its own Application, Contracts, or Domain contracts. Cross-module Presentation references must reflect an existing composite HTTP response and must not grant access to foreign persistence or write adapters.
- A response type genuinely reused by another module may live in a dependency-free `Presentation.Contracts` project owned by the defining module; do not duplicate the type or introduce a reverse Presentation-project reference merely to reuse its wire shape.
- Keep shared filters, binders, result/error mapping, policies, and host-neutral SignalR primitives in `FoodDiary.Presentation.Api`.
- Keep middleware, environment configuration, authentication setup, Swagger configuration, telemetry exporters, and executable composition in `FoodDiary.Web.Api`.
- Every module Presentation assembly must be registered explicitly by the Web API composition root so MVC controller discovery cannot depend on accidental transitive references.
- Presentation projects must not reference Infrastructure or executable host projects.

## Verification

- Preserve route templates, authorization attributes, status codes, payload shapes, API versions, and Swagger-visible contracts during physical moves.
- Add or update architecture tests whenever the allowed Presentation dependency graph changes.
- Run the central Presentation tests and Web API integration/Swagger suites after moving controllers between assemblies.
