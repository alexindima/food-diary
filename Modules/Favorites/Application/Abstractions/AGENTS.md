# Favorites application ports

Own the three slices' repository interfaces, persistence read models and errors.
`IFavoriteMealSourceReadService` is a consumed source-data port implemented by
Meals, not permission to acquire a Meal aggregate. Keep legacy namespaces,
signatures, cancellation/default forwarding and user-scoped access intact.

Do not reference central Application.Abstractions, application implementations,
Infrastructure or HTTP. Public favorite read services and projections belong to
`Modules/Favorites/Contracts`; foreign applications must not acquire repository
ports. The central Errors facade delegates here for source compatibility only.
