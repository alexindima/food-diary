# Dashboard logical module

Dashboard owns snapshot read composition, projection ports and optimized read adapters.
It owns no contributing aggregate, Domain, DbSet, EF configuration or PersistenceModel.
Keep all SQL/LINQ batching, tenant predicates, UTC/date semantics, snapshot sections,
fallback paths and cancellation unchanged during extraction.

Projects use canonical FoodDiary.Modules.Dashboard identities and folder-aligned namespaces, including tests. Application.Abstractions is a sibling project.
Contracts contains ReadDashboardStatisticsQuery, its bucket model, public snapshot/result models and client-dashboard query;
Statistics and WeeklyCheckIn reference it directly; nutrition calculations use Meals contracts.
Central Application.Abstractions does not re-export this project. Never reference Dashboard
Application from Statistics; use its stable Contracts seam.

Infrastructure depends only on application/scalar contracts; it has no EF Core or central Infrastructure reference.
Body and meal SQL projections belong to host ReadModel.Composition, which modules must never reference. Hosts call AddDashboardModule for Application
and AddDashboardReadServices after AddInfrastructure for optimized reads. The latter
preserves concrete/interface scoped aliases and replaces fallback read registrations.
HTTP transport lives in `Modules/Dashboard/Presentation`; shared PostgreSQL fixtures, migrations, snapshot and the Presentation kernel remain central.

See docs/ai/dashboard-ownership-inventory.md for retained seams and tests.

## Refactoring guardrails

- Keep all projects as siblings; namespace and physical layout are checked by MigratedModuleNamespaceTests and PhysicalProjectLayoutTests.
- Put single-use query orchestration directly in its handler; retain only genuinely shared operations, independent algorithms, authorization capabilities and technical ports.
- External statistics consumers dispatch ReadDashboardStatisticsQuery; IDashboardStatisticsReadService is an internal Application.Abstractions projection port. Never route that adapter back through Statistics queries. Authorization remains with the caller. DashboardSnapshotBuilder is shared by the ordinary and dietologist snapshots.
