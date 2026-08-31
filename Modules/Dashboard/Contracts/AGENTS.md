# Dashboard cross-module contracts

Own IDashboardStatisticsReadService and DashboardStatisticsBucketReadModel. Preserve
legacy CLR namespaces, optional fields, dates and CancellationToken. Depend only on
central UserId/Domain and Results; never on Dashboard/Statistics implementation.
