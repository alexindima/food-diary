using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Contracts.Models;

namespace FoodDiary.Modules.Dashboard.Application.Queries.GetDashboardSnapshot;

public record GetDashboardSnapshotQuery(
    Guid? UserId,
    DateTime Date,
    int Page,
    int PageSize,
    string Locale,
    int TrendDays,
    int? TimeZoneOffsetMinutes = null) : IQuery<Result<DashboardSnapshotModel>>, IUserRequest;
