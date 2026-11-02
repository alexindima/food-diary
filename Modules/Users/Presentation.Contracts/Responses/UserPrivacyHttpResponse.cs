namespace FoodDiary.Modules.Users.Presentation.Contracts.Responses;

public sealed record UserPrivacyHttpResponse(string NoticeVersion, DateTime? HealthAcceptedAtUtc, DateTime? AiAcceptedAtUtc, DateTime? MarketingAcceptedAtUtc, DateTime? HealthDeclinedAtUtc);
