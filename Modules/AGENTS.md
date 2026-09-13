# Module Guidelines

## Scope

Rules for all logical modules under `Modules/`. A module-specific `AGENTS.md` may add stricter ownership rules.

## Physical boundaries

- Place new .NET projects in sibling directories; do not put a `.csproj` beneath another project's directory. Existing physical nesting is tracked by `PhysicalProjectLayoutTests`; remove its exact legacy entry when relocating a project, and do not add exceptions for new projects.

- Keep module-owned HTTP controllers, request/response DTOs, mappings, and presentation-only processors in the module's `Presentation` project.
- A module Presentation project may reference the shared `FoodDiary.Presentation.Api` HTTP kernel and its own Application, Contracts, or Domain contracts. Do not reference another module's controller-bearing Presentation assembly; consume its Presentation.Contracts and pure Presentation.Mappings when composing responses.
- A response type genuinely reused by another module may live in a `Presentation.Contracts` project that depends only on other wire DTO contracts owned by the defining module; do not duplicate the type or introduce a reverse Presentation-project reference merely to reuse its wire shape.
- Keep shared filters, binders, result/error mapping, policies, and host-neutral SignalR primitives in `FoodDiary.Presentation.Api`.
- Keep middleware, environment configuration, authentication setup, Swagger configuration, telemetry exporters, and executable composition in `FoodDiary.Web.Api`.
- Every module Presentation assembly must be registered explicitly by the Web API composition root so MVC controller discovery cannot depend on accidental transitive references.
- Presentation projects must not reference Infrastructure or executable host projects.
- Module-owned controller and HTTP-mapping tests belong under `Modules/<Module>/tests/` in a Presentation test project that references the owning module Presentation assembly and actual consumed narrow Contracts/Domain.Contracts owners listed in the exact dependency matrix. Keep shared filters, binders, conventions, composite endpoints, and cross-module HTTP tests in the central Presentation test project.

## Application service extraction

- Keep logic specific to one command/query in its existing handler by default, using private methods when helpful. Do not add a service, interface and DI registration solely to keep a handler thin.
- Extract a service when it owns a cohesive operation reused by multiple production callers, a substantial independent algorithm, an integration boundary or a distinct lifecycle (for example, an application workflow invoked by a background worker).
- Evaluate reuse per operation, not per class: several unrelated methods each called by one handler do not establish reuse. Tests and DI registrations are not production callers.
- During review, flag handlers that only forward to a same-module application service with one implementation and one caller per operation. Treat this as a simplification candidate, not an automatic ban on single-consumer services; preserve justified ports, adapters and workflows.
- When inlining, retain owner contract boundaries, validation, defaults, mapping, cancellation and privacy behavior. Remove the obsolete interface and DI registration and adapt existing behavioral tests.

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

Reusable response mappers belong to owner `Presentation.Mappings` projects. They reference only narrow application/scalar contracts, DTO contracts and pure mappers. Keep request-to-command mapping in Presentation; do not expose controllers or handlers through reusable layers. See docs/adr/0039-presentation-contracts-and-mappings.md.

Module runtime DbContext types must use `FoodDiary.Modules.<Module>.Infrastructure.Persistence`, regardless of legacy RootNamespace values retained for other types. RuntimeContextNamespaceTests enforces this across module Infrastructure sources. Keep provider CLR names and migration models separate from this naming rule.
