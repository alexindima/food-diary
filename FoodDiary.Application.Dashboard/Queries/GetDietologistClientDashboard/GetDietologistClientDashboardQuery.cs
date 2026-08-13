using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dashboard.Queries.GetDietologistClientDashboard;

public sealed record GetDietologistClientDashboardQuery(
    Guid? UserId,
    Guid ClientUserId,
    DateTime Date,
    DateTime? DateTo,
    int Page,
    int PageSize,
    string Locale,
    int TrendDays) : IQuery<Result<DashboardSnapshotModel>>, IUserRequest;
