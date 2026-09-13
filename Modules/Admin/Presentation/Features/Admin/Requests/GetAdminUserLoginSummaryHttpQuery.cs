namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record GetAdminUserLoginSummaryHttpQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);
