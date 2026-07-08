using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;

public sealed record GetAdminDashboardSummaryQuery(int RecentLimit = 5)
    : IQuery<Result<AdminDashboardSummaryModel>>;
