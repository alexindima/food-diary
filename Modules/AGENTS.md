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
- Module-owned controller and HTTP-mapping tests belong under `Modules/<Module>/tests/` in a Presentation test project that references the owning module Presentation assembly and actual consumed narrow Contracts/Domain.Contracts owners listed in the exact dependency matrix. Keep shared filters, binders, conventions, composite endpoints, and cross-module HTTP tests in the central Presentation test project.

## Verification

- Preserve route templates, authorization attributes, status codes, payload shapes, API versions, and Swagger-visible contracts during physical moves.
- Add or update architecture tests whenever the allowed Presentation dependency graph changes.
- Run the central Presentation tests and Web API integration/Swagger suites after moving controllers between assemblies.

## Aggregate and persistence isolation

Application API and owner service-contract dependencies must form an acyclic graph.
Consumer ports may be implemented by the supplying owner; shared technical contracts
belong in narrow Shared projects. The legacy Images.Contracts assembly is ID-only.
No module Domain may retain a foreign aggregate navigation, including in a backing
field or collection. Keep scalar IDs and immutable read snapshots; define unchanged
relational constraints in PersistenceModel. Infrastructure reads foreign sets through
immediate AsNoTracking projections and requests mutations through owner ports.
FD0015 blocks foreign tracking/writes; FD0016 requires an exact reviewed fingerprint
for shared-context save, transaction and tracker escape APIs. See ADR 0031.

Application and Presentation projects never reference a foreign whole Application implementation. Actual cross-module requests/results belong to the owning Contracts project. Scalar Meals/Favorites IDs and Meals enums belong to their Domain.Contracts seams.
