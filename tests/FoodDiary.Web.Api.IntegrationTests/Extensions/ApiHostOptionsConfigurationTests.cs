using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ApiHostOptionsConfigurationTests {
    [Fact]
    public void AddApiServices_BindsHostOptionsFromConfiguration() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
                ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
                ["Jwt:Issuer"] = "FoodDiaryApi",
                ["Jwt:Audience"] = "FoodDiaryClient",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
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
        using ServiceProvider provider = services.BuildServiceProvider();

        ApiCorsOptions cors = provider.GetRequiredService<IOptions<ApiCorsOptions>>().Value;
        ApiForwardedHeadersOptions forwardedHeaders = provider.GetRequiredService<IOptions<ApiForwardedHeadersOptions>>().Value;
        ApiRateLimitingOptions rateLimiting = provider.GetRequiredService<IOptions<ApiRateLimitingOptions>>().Value;
        ApiOutputCacheOptions outputCache = provider.GetRequiredService<IOptions<ApiOutputCacheOptions>>().Value;
        UserLoginEventCleanupOptions userLoginEventCleanup = provider.GetRequiredService<IOptions<UserLoginEventCleanupOptions>>().Value;
        ForwardedHeadersOptions forwardedHeadersOptions = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>>().Value;

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
        Assert.Contains(forwardedHeadersOptions.KnownProxies, ip => string.Equals(ip.ToString(), "10.0.0.10", StringComparison.Ordinal));
        Assert.Contains(forwardedHeadersOptions.KnownIPNetworks, network => string.Equals(network.BaseAddress.ToString(), "10.0.0.0", StringComparison.Ordinal) && network.PrefixLength == 24);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void ApiOutputCacheOptions_HasValidUserScoped_ReturnsExpectedResult(int expirationSeconds, bool expected) {
        var options = new ApiOutputCacheOptions {
            UserScoped = new ApiOutputCacheOptions.UserScopedCacheOptions {
                ExpirationSeconds = expirationSeconds
            }
        };

        bool valid = ApiOutputCacheOptions.HasValidUserScoped(options);

        Assert.Equal(expected, valid);
    }

    [Fact]
    public void AddApiServices_WithInvalidUserScopedOutputCache_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
                ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
                ["Jwt:Issuer"] = "FoodDiaryApi",
                ["Jwt:Audience"] = "FoodDiaryClient",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
                ["TelegramBot:ApiSecret"] = "",
                ["Cors:Origins:0"] = "http://localhost:4200",
                ["OutputCache:AdminAiUsage:ExpirationSeconds"] = "15",
                ["OutputCache:UserScoped:ExpirationSeconds"] = "0",
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ApiOutputCacheOptions>>().Value);
        Assert.Contains("OutputCache:UserScoped:ExpirationSeconds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddApiServices_ConfiguresHttpLoggingOptions() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
                ["Jwt:SecretKey"] = "integration-tests-jwt-secret-key-123",
                ["Jwt:Issuer"] = "FoodDiaryApi",
                ["Jwt:Audience"] = "FoodDiaryClient",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
                ["TelegramBot:ApiSecret"] = "",
                ["Cors:Origins:0"] = "http://localhost:4200",
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        HttpLoggingOptions options = provider.GetRequiredService<IOptions<HttpLoggingOptions>>().Value;

        Assert.Equal(
            HttpLoggingFields.RequestMethod |
            HttpLoggingFields.RequestPath |
            HttpLoggingFields.ResponseStatusCode |
            HttpLoggingFields.Duration,
            options.LoggingFields);
        Assert.Contains("X-Correlation-Id", options.RequestHeaders);
    }
}
