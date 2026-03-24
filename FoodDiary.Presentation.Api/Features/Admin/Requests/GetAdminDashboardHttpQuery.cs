namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminDashboardHttpQuery(
    int Recent = 5);
