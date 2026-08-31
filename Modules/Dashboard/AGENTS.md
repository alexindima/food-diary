# Dashboard logical module

Dashboard owns snapshot read composition, projection ports and optimized read adapters.
It owns no contributing aggregate, Domain, DbSet, EF configuration or PersistenceModel.
Keep all SQL/LINQ batching, tenant predicates, UTC/date semantics, snapshot sections,
fallback paths and cancellation unchanged during extraction.

Application preserves FoodDiary.Application.Dashboard assembly and CLR namespaces.
Contracts contains only the stable statistics read service and bucket model; central
Application.Abstractions references it one-way for existing consumers including
Statistics, Cycles, WeeklyCheckIn, Gamification and Tdee. Never reference Dashboard
Application from Statistics: Dashboard's mediator fallback already consumes Statistics.

Infrastructure references the shared central DbContext one-way. Central Infrastructure
must not reference this adapter project. Hosts call AddDashboardModule for Application
and AddDashboardReadServices after AddInfrastructure for optimized reads. The latter
preserves concrete/interface scoped aliases and replaces fallback read registrations.
HTTP transport, shared PostgreSQL fixtures, migrations and snapshot remain central.

See docs/ai/dashboard-ownership-inventory.md for retained seams and tests.
