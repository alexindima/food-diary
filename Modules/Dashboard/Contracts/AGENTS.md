# Dashboard cross-module contracts

Own ReadDashboardStatisticsQuery, DashboardStatisticsBucketReadModel and the
public Dashboard snapshot/result graph plus GetDietologistClientDashboardQuery.
Keep folder-aligned namespaces; preserve optional fields, dates and cancellation semantics. Consume
contributor models through their narrow owner contracts; no whole Application,
Domain, Infrastructure or Presentation dependencies. DashboardUserContextModel
remains internal implementation data in Application.

ReadDashboardStatisticsQuery is a trusted composition read over an explicitly supplied UserId. Callers retain their authorization and date-validation responsibility. The internal statistics provider port belongs to Application.Abstractions.
