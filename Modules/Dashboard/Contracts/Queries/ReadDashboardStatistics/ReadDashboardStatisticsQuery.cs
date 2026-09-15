using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics;

// Trusted composition read: callers own authorization for the supplied user.
public sealed record ReadDashboardStatisticsQuery(
    UserId UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays) : IQuery<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>>;
