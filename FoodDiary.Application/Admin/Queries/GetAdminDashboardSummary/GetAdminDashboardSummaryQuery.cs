using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;

public sealed record GetAdminDashboardSummaryQuery(int RecentLimit = 5)
    : IQuery<Result<AdminDashboardSummaryResponse>>;
