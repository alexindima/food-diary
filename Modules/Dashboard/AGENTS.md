# Dashboard logical module

Dashboard owns snapshots, projection ports and dashboard-specific DTOs. It owns no
contributing aggregate, Domain, DbSet, EF configuration or PersistenceModel.
Use canonical project identities and folder namespaces. Projects remain siblings.

Application registers the scoped DashboardStatisticsReadService adapter over Meals
nutrition contracts through AddDashboardModule. No Dashboard Infrastructure assembly
or separate AddDashboardReadServices registration remains. Body and meal SQL readers
belong to host ReadModel.Composition; modules must never reference that assembly.
Preserve scoped aliases, sequential shared-context reads, user/date predicates and
the single weekly read for one-day snapshots.

Contracts owns the public snapshot graph and client-dashboard query. Statistics and
WeeklyCheckIn consume ReadMealNutritionStatisticsQuery and its model from Meals.Contracts.
Never route dashboard's statistics adapter through Statistics. Callers own authorization.
Retain shared snapshot/section builders; keep single-use orchestration in handlers.

HTTP transport stays in Presentation. Application tests remain module-owned;
cross-module EF projection and composition tests live in Platform/tests.
See docs/adr/0049-owner-nutrition-reads-and-dashboard-composition.md.
