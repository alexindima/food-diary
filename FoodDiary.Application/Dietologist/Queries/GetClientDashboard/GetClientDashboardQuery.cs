using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dashboard.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public record GetClientDashboardQuery(
    Guid? UserId,
    Guid ClientUserId,
    DateTime Date,
    int Page,
    int PageSize,
    string Locale,
    int TrendDays) : IQuery<Result<DashboardSnapshotModel>>, IUserRequest;
