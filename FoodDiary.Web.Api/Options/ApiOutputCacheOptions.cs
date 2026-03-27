namespace FoodDiary.Web.Api.Options;

public sealed class ApiOutputCacheOptions {
    public const string SectionName = "OutputCache";

    public AdminAiUsageCacheOptions AdminAiUsage { get; init; } = new();

    public static bool HasValidAdminAiUsage(ApiOutputCacheOptions options) =>
        options.AdminAiUsage.ExpirationSeconds > 0;

    public sealed class AdminAiUsageCacheOptions {
        public int ExpirationSeconds { get; init; } = 15;
    }
}
