# Dashboard cross-module contracts

Own DashboardStatisticsBucketReadModel, the public snapshot/result graph and
GetDietologistClientDashboardQuery. Keep folder-aligned namespaces and preserve
optional fields, dates and cancellation. Consume contributors through narrow owner
contracts; no whole Application, Domain, Infrastructure or Presentation references.
DashboardUserContextModel remains an internal Application implementation type.

General nutrition reads belong to Meals.Contracts.ReadMealNutritionStatisticsQuery.
The dashboard bucket is its snapshot shape, not an intermediate Statistics or
WeeklyCheckIn contract. Internal adapter ports stay in Application.Abstractions.
