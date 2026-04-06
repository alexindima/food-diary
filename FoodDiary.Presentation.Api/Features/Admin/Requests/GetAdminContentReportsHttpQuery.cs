namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminContentReportsHttpQuery(
    string? Status = null,
    int Page = 1,
    int Limit = 20);
