namespace FoodDiary.Web.Api.Options;

public sealed class ApiOutputCacheOptions {
    public const string SectionName = "OutputCache";

    public AdminAiUsageCacheOptions AdminAiUsage { get; init; } = new();
    public UserScopedCacheOptions UserScoped { get; init; } = new();

    public static bool HasValidAdminAiUsage(ApiOutputCacheOptions options) =>
        options.AdminAiUsage.ExpirationSeconds > 0;

    public static bool HasValidUserScoped(ApiOutputCacheOptions options) =>
        options.UserScoped.ExpirationSeconds > 0;

    public sealed class AdminAiUsageCacheOptions {
        public int ExpirationSeconds { get; init; } = 15;
    }

    public sealed class UserScopedCacheOptions {
        public int ExpirationSeconds { get; init; } = 5;
    }
}
