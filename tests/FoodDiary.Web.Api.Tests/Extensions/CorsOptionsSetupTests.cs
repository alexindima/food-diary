using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class CorsOptionsSetupTests {
    [Fact]
    public void DevelopmentConfiguration_DefinesOnlyLocalhostOrigins() {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"))
            .Build();

        string[] origins = configuration
            .GetSection($"{ApiCorsOptions.SectionName}:Origins")
            .Get<string[]>()!;

        Assert.Equal(["http://localhost:4200", "http://localhost:4300"], origins);
    }

    [Fact]
    public void Configure_UsesOnlyConfiguredOrigins() {
        string[] configuredOrigins = ["https://fooddiary.club", "https://admin.fooddiary.club"];
        var setup = new CorsOptionsSetup(MsOptions.Create(new ApiCorsOptions { Origins = configuredOrigins }));
        var options = new CorsOptions();

        setup.Configure(options);

        CorsPolicy? policy = options.GetPolicy(ApiCompositionConstants.CorsPolicyName);
        Assert.NotNull(policy);
        Assert.Multiple(
            () => Assert.Equal(configuredOrigins, policy.Origins),
            () => Assert.True(policy.SupportsCredentials),
            () => Assert.Contains("GET", policy.Methods, StringComparer.Ordinal),
            () => Assert.Contains("PATCH", policy.Methods, StringComparer.Ordinal),
            () => Assert.Contains("Authorization", policy.Headers, StringComparer.Ordinal),
            () => Assert.DoesNotContain("X-Api-Version", policy.Headers, StringComparer.OrdinalIgnoreCase),
            () => Assert.Contains("X-Correlation-Id", policy.ExposedHeaders, StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(InvalidOriginSets))]
    public void HasValidOrigins_WhenOriginsAreNotCanonical_ReturnsFalse(string[] origins) {
        Assert.False(ApiCorsOptions.HasValidOrigins(new ApiCorsOptions { Origins = origins }));
    }

    [Theory]
    [InlineData("https://fooddiary.club")]
    [InlineData("https://admin.fooddiary.club:8443")]
    [InlineData("http://localhost:4200")]
    public void HasValidOrigins_WhenOriginIsCanonical_ReturnsTrue(string origin) {
        Assert.True(ApiCorsOptions.HasValidOrigins(new ApiCorsOptions { Origins = [origin] }));
    }

    public static TheoryData<string[]> InvalidOriginSets => new() {
        { [] },
        { ["not a uri"] },
        { ["ftp://fooddiary.club"] },
        { ["https://user@fooddiary.club"] },
        { ["https://fooddiary.club/path"] },
        { ["https://fooddiary.club?source=test"] },
        { ["https://fooddiary.club#fragment"] },
        { ["https://fooddiary.club/"] },
        { [" https://fooddiary.club"] },
        { ["https://fooddiary.club", "HTTPS://FOODDIARY.CLUB"] },
    };
}
