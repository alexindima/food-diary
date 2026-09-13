using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDashboardSummary;

public sealed record GetAdminDashboardSummaryQuery(int RecentLimit = 5)
    : IQuery<Result<AdminDashboardSummaryModel>>;
