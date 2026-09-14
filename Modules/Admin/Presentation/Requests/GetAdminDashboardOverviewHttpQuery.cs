namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminDashboardOverviewHttpQuery(DateOnly? From = null, DateOnly? To = null, bool AllTime = false);
