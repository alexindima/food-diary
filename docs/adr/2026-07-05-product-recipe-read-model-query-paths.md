# Product And Recipe Read Model Query Paths

## Status
Accepted

## Context
Product and recipe list screens, overview payloads, recent items, and explore results need compact projection data. Loading full domain aggregates for these paths couples query handlers to aggregate persistence shape, risks accidental eager loading, and makes usage-count checks depend on navigation collections.

Product and recipe command paths still need aggregate repositories for write-oriented validation and mutation. Some read-side payloads also need behavior-compatible projection logic, including recipe nutrition summaries for auto-calculated recipes when persisted totals are not available yet.

## Decision
- Product query handlers use `IProductOverviewReadService` for list, overview, recent, and by-id read payloads.
- Recipe query handlers use `IRecipeOverviewReadService` for list, overview, recent, explore, and by-id read payloads.
- Product and recipe aggregate read repositories remain narrow and write-oriented: load aggregates by id or ids, and expose explicit usage-count queries.
- Command validators and handlers use count projections for product and recipe usage checks instead of loading navigation collections and counting them in memory.
- Recipe overview projections preserve existing API behavior by calculating nutrition summaries from projected ingredients for auto-calculated recipes when stored totals are absent.
- Architecture tests guard product and recipe query handlers from aggregate repository dependencies and guard command paths from usage checks based on loaded collections.

## Consequences
- Query handlers depend on stable application read-service abstractions instead of EF aggregate repository details.
- Infrastructure can optimize product and recipe read payloads independently from command aggregate loading.
- Aggregate repositories become smaller and less likely to grow list-screen-specific include graphs.
- New product or recipe read endpoints should prefer dedicated read services when they return projection data.
- New product or recipe command usage checks should use explicit count queries rather than loaded collection counts.
