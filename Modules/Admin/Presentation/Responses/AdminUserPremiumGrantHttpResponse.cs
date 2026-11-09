namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminUserPremiumGrantHttpResponse(DateTime? EndsAtUtc, string? Reason);
