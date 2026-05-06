using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class CorsOptionsSetupTests {
    [Fact]
    public void Configure_WhenOriginsAreEmpty_UsesLocalhostDefaults() {
        var setup = new CorsOptionsSetup(MsOptions.Create(new ApiCorsOptions { Origins = [] }));
        var options = new CorsOptions();

        setup.Configure(options);

        var policy = options.GetPolicy(ApiCompositionConstants.CorsPolicyName);
        Assert.NotNull(policy);
        Assert.Equal(["http://localhost:4200", "http://localhost:4300"], policy.Origins);
        Assert.True(policy.SupportsCredentials);
        Assert.Contains("GET", policy.Methods);
        Assert.Contains("PATCH", policy.Methods);
        Assert.Contains("Authorization", policy.Headers);
        Assert.Contains("X-Correlation-Id", policy.ExposedHeaders);
    }

    [Fact]
    public void Configure_WhenOriginsAreConfigured_UsesConfiguredOrigins() {
        var setup = new CorsOptionsSetup(MsOptions.Create(new ApiCorsOptions {
            Origins = ["https://app.example", "https://admin.example"],
        }));
        var options = new CorsOptions();

        setup.Configure(options);

        var policy = options.GetPolicy(ApiCompositionConstants.CorsPolicyName);
        Assert.NotNull(policy);
        Assert.Equal(["https://app.example", "https://admin.example"], policy.Origins);
    }

    [Fact]
    public void HasValidOrigins_ReturnsFalseForMalformedOrigins() {
        Assert.False(ApiCorsOptions.HasValidOrigins(new ApiCorsOptions {
            Origins = ["https://app.example", "not a uri"],
        }));
    }
}
