using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Options;

public sealed class OutputCacheOptionsSetup(IOptions<ApiOutputCacheOptions> outputCacheOptions)
    : IConfigureOptions<OutputCacheOptions> {
    public void Configure(OutputCacheOptions options) {
        ApiOutputCacheOptions settings = outputCacheOptions.Value;
        options.AddPolicy(PresentationPolicyNames.AdminAiUsageCachePolicyName, builder => builder
            .Cache()
            .Expire(TimeSpan.FromSeconds(settings.AdminAiUsage.ExpirationSeconds))
            .SetVaryByQuery("*")
            .Tag("admin-ai-usage"));
        options.AddPolicy(PresentationPolicyNames.UserScopedCachePolicyName, builder => builder
            .With(static context => HasUserCacheIdentity(context))
            .Cache()
            .Expire(TimeSpan.FromSeconds(settings.UserScoped.ExpirationSeconds))
            .SetVaryByQuery("*")
            .VaryByValue(static context => GetUserCacheVaryByValue(context))
            .Tag("user-scoped"));
    }

    private static bool HasUserCacheIdentity(OutputCacheContext context) =>
        context.HttpContext.User.GetUserGuid().HasValue;

    private static KeyValuePair<string, string> GetUserCacheVaryByValue(HttpContext context) =>
        new("user-id", context.User.GetUserGuid()?.ToString("D") ?? "missing");
}
