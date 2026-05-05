namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminUserLoginEventsHttpQuery(
    int Page = 1,
    int Limit = 20,
    Guid? UserId = null,
    string? Search = null);
