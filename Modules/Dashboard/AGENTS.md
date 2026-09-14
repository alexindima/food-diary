# Dashboard logical module

Dashboard owns snapshot read composition, projection ports and optimized read adapters.
It owns no contributing aggregate, Domain, DbSet, EF configuration or PersistenceModel.
Keep all SQL/LINQ batching, tenant predicates, UTC/date semantics, snapshot sections,
fallback paths and cancellation unchanged during extraction.

Application preserves FoodDiary.Application.Dashboard assembly and CLR namespaces.
Contracts contains the stable statistics service, bucket model, public snapshot/result models and client-dashboard query;
Statistics and WeeklyCheckIn reference it directly; nutrition calculations use Meals contracts.
Central Application.Abstractions does not re-export this project. Never reference Dashboard
Application from Statistics; use its stable Contracts seam.

Infrastructure depends only on application/scalar contracts; it has no EF Core or central Infrastructure reference.
Body and meal SQL projections belong to host ReadModel.Composition, which modules must never reference. Hosts call AddDashboardModule for Application
and AddDashboardReadServices after AddInfrastructure for optimized reads. The latter
preserves concrete/interface scoped aliases and replaces fallback read registrations.
HTTP transport lives in `Modules/Dashboard/Presentation`; shared PostgreSQL fixtures, migrations, snapshot and the Presentation kernel remain central.

See docs/ai/dashboard-ownership-inventory.md for retained seams and tests.
