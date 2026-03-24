namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminUsersHttpQuery(
    int Page = 1,
    int Limit = 20,
    string? Search = null,
    bool IncludeDeleted = false);
