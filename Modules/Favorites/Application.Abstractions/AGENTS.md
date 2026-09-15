# Favorites application ports

Own the three slices' repository interfaces, persistence read models and errors.
`IFavoriteMealSourceReadService` belongs to Favorites Contracts and is implemented by
Meals, not permission to acquire a Meal aggregate. Keep
signatures, cancellation/default forwarding and user-scoped access intact.

Do not reference central Application.Abstractions, application implementations,
Infrastructure or HTTP. Public favorite read services and projections belong to
`Modules/Favorites/Contracts`; foreign applications must not acquire repository
ports. The central Errors facade is retired; direct callers preserve the existing error contract.

Consumer-owned Result source ports cover Meals, Products and Recipes. Their owners supply scoped source models and original errors; keep foreign aggregates and foreign repository writes out of these contracts.

All three source-data ports and source DTOs belong to Contracts, not these internal ports.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
