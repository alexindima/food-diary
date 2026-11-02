namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserPrivacyStateModel(string NoticeVersion, DateTime? HealthAcceptedAtUtc, DateTime? AiAcceptedAtUtc, DateTime? MarketingAcceptedAtUtc, DateTime? HealthDeclinedAtUtc = null);
