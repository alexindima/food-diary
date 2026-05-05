using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class ApiHostOptionsConfigurationTests {
    [Fact]
    public void AddApiServices_BindsHostOptionsFromConfiguration() {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
                ["Jwt:SecretKey"] = "change-me-local-jwt-secret-min-32",
                ["Jwt:Issuer"] = "FoodDiaryApi",
                ["Jwt:Audience"] = "FoodDiaryClient",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["TelegramBot:ApiSecret"] = "",
                ["Cors:Origins:0"] = "http://localhost:4200",
                ["ForwardedHeaders:ForwardLimit"] = "2",
                ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.10",
                ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/24",
                ["RateLimiting:Auth:PermitLimit"] = "7",
                ["RateLimiting:Auth:WindowSeconds"] = "90",
                ["RateLimiting:Ai:PermitLimit"] = "11",
                ["RateLimiting:Ai:WindowSeconds"] = "120",
                ["OutputCache:AdminAiUsage:ExpirationSeconds"] = "30",
                ["UserLoginEventCleanup:RetentionDays"] = "90",
                ["UserLoginEventCleanup:BatchSize"] = "250",
                ["UserLoginEventCleanup:PollIntervalHours"] = "12",
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using var provider = services.BuildServiceProvider();

        var cors = provider.GetRequiredService<IOptions<ApiCorsOptions>>().Value;
        var forwardedHeaders = provider.GetRequiredService<IOptions<ApiForwardedHeadersOptions>>().Value;
        var rateLimiting = provider.GetRequiredService<IOptions<ApiRateLimitingOptions>>().Value;
        var outputCache = provider.GetRequiredService<IOptions<ApiOutputCacheOptions>>().Value;
        var userLoginEventCleanup = provider.GetRequiredService<IOptions<UserLoginEventCleanupOptions>>().Value;
        var forwardedHeadersOptions = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>>().Value;

        Assert.Equal(["http://localhost:4200"], cors.Origins);
        Assert.Equal(2, forwardedHeaders.ForwardLimit);
        Assert.Equal(["10.0.0.10"], forwardedHeaders.KnownProxies);
        Assert.Equal(["10.0.0.0/24"], forwardedHeaders.KnownNetworks);
        Assert.Equal(7, rateLimiting.Auth.PermitLimit);
        Assert.Equal(90, rateLimiting.Auth.WindowSeconds);
        Assert.Equal(11, rateLimiting.Ai.PermitLimit);
        Assert.Equal(120, rateLimiting.Ai.WindowSeconds);
        Assert.Equal(30, outputCache.AdminAiUsage.ExpirationSeconds);
        Assert.Equal(90, userLoginEventCleanup.RetentionDays);
        Assert.Equal(250, userLoginEventCleanup.BatchSize);
        Assert.Equal(12, userLoginEventCleanup.PollIntervalHours);
        Assert.Equal(2, forwardedHeadersOptions.ForwardLimit);
        Assert.Contains(forwardedHeadersOptions.KnownProxies, ip => ip.ToString() == "10.0.0.10");
        Assert.Contains(forwardedHeadersOptions.KnownIPNetworks, network => network.BaseAddress.ToString() == "10.0.0.0" && network.PrefixLength == 24);
    }
}
