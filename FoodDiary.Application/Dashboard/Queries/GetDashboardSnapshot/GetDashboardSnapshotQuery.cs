using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Dashboard.Models;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

public record GetDashboardSnapshotQuery(
    Guid? UserId,
    DateTime Date,
    int Page,
    int PageSize,
    string Locale,
    int TrendDays) : IQuery<Result<DashboardSnapshotModel>>, IUserRequest;
