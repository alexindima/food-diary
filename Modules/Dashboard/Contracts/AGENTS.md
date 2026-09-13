# Dashboard cross-module contracts

Own IDashboardStatisticsReadService, DashboardStatisticsBucketReadModel and the
public Dashboard snapshot/result graph plus GetDietologistClientDashboardQuery.
Preserve CLR namespaces, optional fields, dates and cancellation semantics. Consume
contributor models through their narrow owner contracts; no whole Application,
Domain, Infrastructure or Presentation dependencies. DashboardUserContextModel
remains internal implementation data in Application.
