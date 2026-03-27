using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using FoodDiary.Web.Api.Extensions;

namespace FoodDiary.Web.Api.Options;

public sealed class CorsOptionsSetup(IOptions<ApiCorsOptions> apiCorsOptions) : IConfigureOptions<CorsOptions> {
    public void Configure(CorsOptions options) {
        var origins = apiCorsOptions.Value.Origins;
        if (origins.Length == 0) {
            origins = ["http://localhost:4200", "http://localhost:4300"];
        }

        options.AddPolicy(ApiCompositionConstants.CorsPolicyName, policy => {
            policy
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    }
}
