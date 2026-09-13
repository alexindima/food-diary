using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDashboardOverview;

public sealed record GetAdminDashboardOverviewQuery(DateOnly? From = null, DateOnly? To = null, bool AllTime = false)
    : IQuery<Result<AdminDashboardOverviewModel>>;
