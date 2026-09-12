# Dashboard optimized read adapters

Own Persistence/Dashboard projection readers and loaders. Use module Infrastructure namespaces; preserve
internal visibility, scoped aliases, query shape/count, ordering, paging, tenant
filters and cancellation. No write repository, aggregate or EF mapping ownership.
Shared FoodDiaryDbContext remains central; reference central Infrastructure one-way.
AddDashboardReadServices installs optimized readers after central infrastructure.

DashboardMealItemsLoader maps IMealItemDisplayReadService results only. Do not reintroduce direct Product/MealItem queries or snapshot/quality rules into that loader.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.
