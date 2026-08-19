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

    public FixedWindowPolicyOptions Webhook { get; init; } = new() {
        PermitLimit = 300,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions YooKassaWebhook { get; init; } = new() {
        PermitLimit = 30,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions ClientTelemetry { get; init; } = new() {
        PermitLimit = 60,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions MarketingAttribution { get; init; } = new() {
        PermitLimit = 30,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions TestDelivery { get; init; } = new() {
        PermitLimit = 5,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions Wearable { get; init; } = new() {
        PermitLimit = 10,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions FoodData { get; init; } = new() {
        PermitLimit = 30,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions SecretVerification { get; init; } = new() {
        PermitLimit = 5,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions Billing { get; init; } = new() {
        PermitLimit = 10,
        WindowSeconds = 60,
    };

    public FixedWindowPolicyOptions Export { get; init; } = new() {
        PermitLimit = 5,
        WindowSeconds = 60,
    };

    public static bool HasValidAuth(ApiRateLimitingOptions options) => options.Auth.IsValid();

    public static bool HasValidAi(ApiRateLimitingOptions options) => options.Ai.IsValid();

    public static bool HasValidWebhook(ApiRateLimitingOptions options) => options.Webhook.IsValid();

    public static bool HasValidYooKassaWebhook(ApiRateLimitingOptions options) => options.YooKassaWebhook.IsValid();

    public static bool HasValidClientTelemetry(ApiRateLimitingOptions options) => options.ClientTelemetry.IsValid();

    public static bool HasValidMarketingAttribution(ApiRateLimitingOptions options) => options.MarketingAttribution.IsValid();

    public static bool HasValidTestDelivery(ApiRateLimitingOptions options) => options.TestDelivery.IsValid();

    public static bool HasValidWearable(ApiRateLimitingOptions options) => options.Wearable.IsValid();

    public static bool HasValidFoodData(ApiRateLimitingOptions options) => options.FoodData.IsValid();

    public static bool HasValidSecretVerification(ApiRateLimitingOptions options) => options.SecretVerification.IsValid();

    public static bool HasValidBilling(ApiRateLimitingOptions options) => options.Billing.IsValid();

    public static bool HasValidExport(ApiRateLimitingOptions options) => options.Export.IsValid();

    public sealed class FixedWindowPolicyOptions {
        public int PermitLimit { get; init; }
        public int WindowSeconds { get; init; }
        public int QueueLimit { get; init; }

        public bool IsValid() => PermitLimit > 0 && WindowSeconds > 0 && QueueLimit >= 0;
    }
}
