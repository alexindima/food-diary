# Meals logical module

MealRecognitionsController owns recognition creation and undo endpoints under
the existing meals route. Keep ordinary CRUD in MealsController and preserve
the shared authorized-controller contract and endpoint request limits.

Meals owns aggregates, invariants and events in Domain; scalar IDs and meal enums
live in Domain.Contracts. Use canonical project and folder namespaces. Keep foreign
User/Product/Recipe/Image relationships scalar and preserve database constraints,
nutrition snapshots and source fallback behavior. Historical migrations remain central.
Hosts compose AddMealsModule; JobManager composes AddMealsPersistence only.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

## Consumer boundary

Own GetMealsQuery and its MealModel, MealItemModel, MealAiSessionModel and MealAiItemModel projections consumed by Dashboard. Meals.Contracts owns activity/export queries and technical read capabilities. Keep handlers, aggregate policy and MealOverviewModel in Application. See `Service.Contracts/AGENTS.md` and ADR 0033.

Activity and export consumers dispatch Contracts queries through ISender. Favorites uses its consumer-owned IFavoriteMealSourceReadService implemented by FavoriteMealSourceReadService. Keep paging/favorite enrichment shared in MealReadSupport; do not restore a forwarding MealReadService.
