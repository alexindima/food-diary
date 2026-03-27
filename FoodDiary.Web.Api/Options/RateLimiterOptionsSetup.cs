using System.Threading.RateLimiting;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using FoodDiary.Web.Api.Extensions;

namespace FoodDiary.Web.Api.Options;

public sealed class RateLimiterOptionsSetup(IOptions<ApiRateLimitingOptions> rateLimitingOptions)
    : IConfigureOptions<RateLimiterOptions> {
    public void Configure(RateLimiterOptions options) {
        var settings = rateLimitingOptions.Value;

        options.OnRejected = async (context, cancellationToken) => {
            var httpContext = context.HttpContext;
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ApiTelemetry.RateLimitRejectionCounter.Add(
                1,
                new KeyValuePair<string, object?>("http.request.method", httpContext.Request.Method),
                new KeyValuePair<string, object?>("url.path", httpContext.Request.Path.Value));

            await httpContext.Response.WriteAsJsonAsync(new ApiErrorHttpResponse(
                "RateLimit.Exceeded",
                "Too many requests. Try again later.",
                httpContext.TraceIdentifier), cancellationToken);
        };

        options.AddPolicy<string>(PresentationPolicyNames.AuthRateLimitPolicyName, context =>
            CreatePartition(settings.Auth, $"auth:{GetPartitionKey(context)}"));
        options.AddPolicy<string>(PresentationPolicyNames.AiRateLimitPolicyName, context =>
            CreatePartition(settings.Ai, $"ai:{GetPartitionKey(context)}"));
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

    private static string GetPartitionKey(HttpContext context) {
        var userId = context.User.GetUserGuid();
        if (userId.HasValue && userId.Value != Guid.Empty) {
            return $"user:{userId.Value:D}";
        }

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor)) {
            return $"ip:{forwardedFor}";
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{remoteIp ?? "unknown"}";
    }
}
