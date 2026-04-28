namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminBillingHttpQuery(
    int Page = 1,
    int Limit = 20,
    string? Provider = null,
    string? Status = null,
    string? Kind = null,
    string? Search = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);
