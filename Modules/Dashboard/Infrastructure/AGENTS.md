# Dashboard optimized read adapters

Own Persistence/Dashboard projection readers and loaders. Use module Infrastructure namespaces; preserve
internal visibility, scoped aliases, query shape/count, ordering, paging, tenant
filters and cancellation. No write repository, aggregate or EF mapping ownership.
Shared FoodDiaryDbContext remains central; reference central Infrastructure one-way.
AddDashboardReadServices installs optimized readers after central infrastructure.
