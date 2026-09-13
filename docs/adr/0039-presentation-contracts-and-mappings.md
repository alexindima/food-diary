# ADR 0039: Separate HTTP contracts and response mappings

- Status: Accepted
- Date: 2026-09-13
- Extends: ADR 0038

## Context

Sixteen module Presentation references exposed foreign controller assemblies to
reuse response DTOs and model-to-response mappings. Moving DTOs alone would leave
the mapping dependencies. The user selected separate assemblies for DTOs and pure
mappers instead of combining them or duplicating transformations in consumers.

## Decision

Owner Presentation.Contracts assemblies contain immutable wire DTOs. They may
reference other Presentation.Contracts when a response composes their DTOs, but
never application models, mappers, controllers, ASP.NET or persistence.

Owner Presentation.Mappings assemblies contain pure response transformations and
may reference narrow application/scalar contracts, DTO contracts and other pure
mapping assemblies. They cannot reference whole Application, Domain, Presentation,
Infrastructure or executable projects. No I/O, DI or request dispatch belongs here.
Request-to-command mappings and endpoint-specific response composition stay in
Presentation. Owners continue registering their controller assemblies explicitly.

BodyMetrics, Cycles, Dashboard, Fasting, Favorites, Hydration, Meals, Notifications,
Tdee and Users each gain the two reusable layers. Only reused transformations and
their DTO closure move; existing Dietologist DTO contracts retain their role.
Thus DTO ownership remains independent of mapper implementation, without copying
mapping code or introducing runtime indirection.

Dashboard's public snapshot graph and GetDietologistClientDashboardQuery move to
Dashboard.Contracts so the shared mapper and Dietologist endpoint do not acquire
Dashboard.Application transitively. Internal DashboardUserContextModel, handlers,
validation, read adapters and authorization remain with their current owners.

## Compatibility and consequences

CLR namespaces, response fields, nullability, enum/date encodings, collection
ordering and endpoint behavior are preserved. Moving declarations changes assembly
identity and requires rebuilding/deploying hosts and consumers together. No database
migration, controller registration change or network call is introduced.

Twenty small projects are the explicit cost of the stricter separation. Contracts
may compose other contracts, so the graph is not completely disconnected. Existing
Abstractions can still expose owner repository ports transitively; these response
mappers use model types only and do not gain permission to mutate aggregates.

## Enforcement

PresentationContractBoundaryTests prohibits foreign controller references and
runtime/implementation dependencies in reusable layers. ProjectDependencyMatrixTests
records exact edges. Docker restore/source copies and lockfiles include the new
projects. Existing module response tests and full Swagger contract snapshots protect
wire compatibility; controller-bearing assembly registrations remain unchanged.
