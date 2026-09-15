using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Contracts.Queries.GetDietologistClientDashboard;

public sealed record GetDietologistClientDashboardQuery(
    Guid? UserId,
    Guid ClientUserId,
    DateTime Date,
    DateTime? DateTo,
    int Page,
    int PageSize,
    string Locale,
    int TrendDays) : IQuery<Result<DashboardSnapshotModel>>, IUserRequest;
