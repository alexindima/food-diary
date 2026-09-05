namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminUserLoginSummaryHttpQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);
