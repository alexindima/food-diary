# Dashboard cross-module contracts

Own IDashboardStatisticsReadService and DashboardStatisticsBucketReadModel. Preserve
legacy CLR namespaces, optional fields, dates and CancellationToken. Depend only on
Users Domain.Contracts for UserId and shared Results; never on Dashboard/Statistics implementation.
