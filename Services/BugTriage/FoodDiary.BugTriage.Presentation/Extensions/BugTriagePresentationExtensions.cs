using Microsoft.AspNetCore.Builder;
using System.Threading.RateLimiting;
using FoodDiary.BugTriage.Presentation.Options;
using FoodDiary.BugTriage.Presentation.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.BugTriage.Presentation.Extensions;

public static class BugTriagePresentationExtensions {
    public static IServiceCollection AddBugTriagePresentation(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<BugTriageHttpOptions>().Bind(configuration.GetSection("BugTriageHttp"))
            .Validate(static o => !string.IsNullOrWhiteSpace(o.ApiKey) && o.ApiKey.Length is >= 32 and <= 256 &&
                o.LeaseDuration >= TimeSpan.FromMinutes(1) && o.LeaseDuration <= TimeSpan.FromHours(2) &&
                o.MaxAttempts is >= 1 and <= 10, "A BugTriage key and bounded lease settings are required.")
            .ValidateOnStart();
        services.AddScoped<BugTriageAuthorizationFilter>();
        services.AddControllers().AddApplicationPart(typeof(BugTriagePresentationExtensions).Assembly);
        services.AddRateLimiter(o => {
            o.RejectionStatusCode = 429;
            o.AddFixedWindowLimiter("bugtriage", limiter => {
                limiter.PermitLimit = 120;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });
        return services;
    }
}
