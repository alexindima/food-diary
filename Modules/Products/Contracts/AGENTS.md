# Products Contracts

Stable Product lookup/overview projections and semantic USDA linking capability consumed by other modules. No infrastructure or application implementation dependencies, no aggregate-returning API.

IProductSnapshotReadService returns immutable batch scalar snapshots for related
Meals/Recipes hydration. It preserves missing-ID behavior and does not perform
visibility authorization; callers retain their existing aggregate access checks.
