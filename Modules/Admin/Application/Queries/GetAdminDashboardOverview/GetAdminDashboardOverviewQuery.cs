using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardOverview;

public sealed record GetAdminDashboardOverviewQuery(DateOnly? From = null, DateOnly? To = null, bool AllTime = false)
    : IQuery<Result<AdminDashboardOverviewModel>>;
