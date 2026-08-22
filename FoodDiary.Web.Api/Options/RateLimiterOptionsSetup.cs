using System.Globalization;
using System.Threading.RateLimiting;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using FoodDiary.Web.Api.Extensions;

namespace FoodDiary.Web.Api.Options;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class RateLimiterOptionsSetup(IOptions<ApiRateLimitingOptions> rateLimitingOptions)
    : IConfigureOptions<RateLimiterOptions> {
    public void Configure(RateLimiterOptions options) {
        ApiRateLimitingOptions settings = rateLimitingOptions.Value;

        options.OnRejected = async (context, cancellationToken) => {
            HttpContext httpContext = context.HttpContext;
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter)) {
                double seconds = Math.Ceiling(retryAfter.TotalSeconds);
                httpContext.Response.Headers.RetryAfter = Math.Max(1, seconds).ToString(CultureInfo.InvariantCulture);
            }
            ApiTelemetry.RateLimitRejectionCounter.Add(
                1,
                new KeyValuePair<string, object?>("http.request.method", httpContext.Request.Method),
                new KeyValuePair<string, object?>("url.path", TelemetryPrivacyProcessor.ResolveRouteLabel(httpContext)));

            await httpContext.Response.WriteAsJsonAsync(new ApiErrorHttpResponse(
                "RateLimit.Exceeded",
                "Too many requests. Try again later.",
                httpContext.TraceIdentifier), cancellationToken).ConfigureAwait(false);
        };

        options.AddPolicy<string>(PresentationPolicyNames.AuthRateLimitPolicyName, context =>
            CreatePartition(settings.Auth, $"auth:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.AiRateLimitPolicyName, context =>
            CreatePartition(settings.Ai, $"ai:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.WebhookRateLimitPolicyName, context =>
            CreateWebhookPartition(settings, context));
        options.AddPolicy<string>(PresentationPolicyNames.ClientTelemetryRateLimitPolicyName, context =>
            CreatePartition(settings.ClientTelemetry, $"client-telemetry:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.MarketingAttributionRateLimitPolicyName, context =>
            CreatePartition(settings.MarketingAttribution, $"marketing-attribution:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.TestDeliveryRateLimitPolicyName, context =>
            CreatePartition(settings.TestDelivery, $"test-delivery:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.WearableRateLimitPolicyName, context =>
            CreatePartition(settings.Wearable, $"wearable:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.FoodDataRateLimitPolicyName, context =>
            CreatePartition(settings.FoodData, $"food-data:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.SecretVerificationRateLimitPolicyName, context =>
            CreatePartition(settings.SecretVerification, $"secret-verification:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.BillingRateLimitPolicyName, context =>
            CreatePartition(settings.Billing, $"billing:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.ExportRateLimitPolicyName, context =>
            CreatePartition(settings.Export, $"export:{GetPartitionKey(context)}"));
    }

    private static RateLimitPartition<string> CreatePartition(
        ApiRateLimitingOptions.FixedWindowPolicyOptions settings,
        string partitionKey) {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = settings.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
            });
    }

    private static RateLimitPartition<string> CreateWebhookPartition(
        ApiRateLimitingOptions settings,
        HttpContext context) {
        string? provider = context.Request.RouteValues["provider"]?.ToString();
        return string.Equals(provider, "yookassa", StringComparison.OrdinalIgnoreCase)
            ? CreatePartition(settings.YooKassaWebhook, "webhook:provider:yookassa")
            : CreatePartition(settings.Webhook, $"webhook:{GetPartitionKey(context)}");
    }

    private static string GetPartitionKey(HttpContext context) {
        Guid? userId = context.User.GetUserGuid();
        if (userId.HasValue && userId.Value != Guid.Empty) {
            return $"user:{userId.Value:D}";
        }

        string? remoteIp = context.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        return $"ip:{remoteIp ?? "unknown"}";
    }
}
