namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminDashboardOverviewHttpQuery(DateOnly? From = null, DateOnly? To = null, bool AllTime = false);
