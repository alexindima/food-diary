namespace FoodDiary.Web.Api.Options;

public sealed class ApiRateLimitingOptions {
    public const string SectionName = "RateLimiting";

    public FixedWindowPolicyOptions Auth { get; init; } = new() {
        PermitLimit = 5,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions Ai { get; init; } = new() {
        PermitLimit = 10,
        WindowSeconds = 60,
    };

    public static bool HasValidAuth(ApiRateLimitingOptions options) => options.Auth.IsValid();

    public static bool HasValidAi(ApiRateLimitingOptions options) => options.Ai.IsValid();

    public sealed class FixedWindowPolicyOptions {
        public int PermitLimit { get; init; }
        public int WindowSeconds { get; init; }
        public int QueueLimit { get; init; }

        public bool IsValid() => PermitLimit > 0 && WindowSeconds > 0 && QueueLimit >= 0;
    }
}
