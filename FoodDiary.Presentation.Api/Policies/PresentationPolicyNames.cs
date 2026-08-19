namespace FoodDiary.Presentation.Api.Policies;

public static class PresentationPolicyNames {
    public const string AuthRateLimitPolicyName = "PresentationAuthRateLimit";
    public const string AiRateLimitPolicyName = "PresentationAiRateLimit";
    public const string WebhookRateLimitPolicyName = "PresentationWebhookRateLimit";
    public const string ClientTelemetryRateLimitPolicyName = "PresentationClientTelemetryRateLimit";
    public const string MarketingAttributionRateLimitPolicyName = "PresentationMarketingAttributionRateLimit";
    public const string TestDeliveryRateLimitPolicyName = "PresentationTestDeliveryRateLimit";
    public const string WearableRateLimitPolicyName = "PresentationWearableRateLimit";
    public const string FoodDataRateLimitPolicyName = "PresentationFoodDataRateLimit";
    public const string SecretVerificationRateLimitPolicyName = "PresentationSecretVerificationRateLimit";
    public const string BillingRateLimitPolicyName = "PresentationBillingRateLimit";
    public const string ExportRateLimitPolicyName = "PresentationExportRateLimit";
    public const string AdminAiUsageCachePolicyName = "PresentationAdminAiUsageCache";
    public const string UserScopedCachePolicyName = "PresentationUserScopedCache";
}
