# Dashboard logical module

Dashboard owns snapshot read composition, projection ports and optimized read adapters.
It owns no contributing aggregate, Domain, DbSet, EF configuration or PersistenceModel.
Keep all SQL/LINQ batching, tenant predicates, UTC/date semantics, snapshot sections,
fallback paths and cancellation unchanged during extraction.

Application preserves FoodDiary.Application.Dashboard assembly and CLR namespaces.
Contracts contains only the stable statistics read service and bucket model;
Statistics, Cycles, WeeklyCheckIn, Gamification and Tdee reference it directly.
Central Application.Abstractions does not re-export this project. Never reference Dashboard
Application from Statistics: Dashboard's mediator fallback already consumes Statistics.

Infrastructure references the shared central DbContext one-way. Central Infrastructure
must not reference this adapter project. Hosts call AddDashboardModule for Application
and AddDashboardReadServices after AddInfrastructure for optimized reads. The latter
preserves concrete/interface scoped aliases and replaces fallback read registrations.
HTTP transport lives in `Modules/Dashboard/Presentation`; shared PostgreSQL fixtures, migrations, snapshot and the Presentation kernel remain central.

See docs/ai/dashboard-ownership-inventory.md for retained seams and tests.
