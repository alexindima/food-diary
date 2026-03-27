using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Options;

public sealed class OutputCacheOptionsSetup(IOptions<ApiOutputCacheOptions> outputCacheOptions)
    : IConfigureOptions<OutputCacheOptions> {
    public void Configure(OutputCacheOptions options) {
        var settings = outputCacheOptions.Value;
        options.AddPolicy(PresentationPolicyNames.AdminAiUsageCachePolicyName, builder => builder
            .Cache()
            .Expire(TimeSpan.FromSeconds(settings.AdminAiUsage.ExpirationSeconds))
            .SetVaryByQuery("*")
            .Tag("admin-ai-usage"));
    }
}
