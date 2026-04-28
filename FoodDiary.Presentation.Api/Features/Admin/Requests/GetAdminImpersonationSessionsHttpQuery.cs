namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminImpersonationSessionsHttpQuery(
    int Page = 1,
    int Limit = 20,
    string? Search = null);
