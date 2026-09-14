namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminUserLoginSummaryHttpQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);
