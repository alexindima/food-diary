using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ApiHostOptionsConfigurationTests {
    [Fact]
    public void ApiRateLimitingOptions_ResourceAndSecretPolicies_RequireValidWindows() {
        var valid = new ApiRateLimitingOptions();
        var invalidSecretVerification = new ApiRateLimitingOptions {
            SecretVerification = new ApiRateLimitingOptions.FixedWindowPolicyOptions(),
        };
        var invalidBilling = new ApiRateLimitingOptions {
            Billing = new ApiRateLimitingOptions.FixedWindowPolicyOptions(),
        };
        var invalidExport = new ApiRateLimitingOptions {
            Export = new ApiRateLimitingOptions.FixedWindowPolicyOptions(),
        };

        Assert.Multiple(
            () => Assert.True(ApiRateLimitingOptions.HasValidSecretVerification(valid)),
            () => Assert.True(ApiRateLimitingOptions.HasValidBilling(valid)),
            () => Assert.True(ApiRateLimitingOptions.HasValidExport(valid)),
            () => Assert.False(ApiRateLimitingOptions.HasValidSecretVerification(invalidSecretVerification)),
            () => Assert.False(ApiRateLimitingOptions.HasValidBilling(invalidBilling)),
            () => Assert.False(ApiRateLimitingOptions.HasValidExport(invalidExport)));
    }

    [Fact]
    public void AddApiServices_WithoutCorsOrigins_FailsOptionsValidation() {
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
            })
            .Build();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ApiCorsOptions>>().Value);

        Assert.Contains("Cors:Origins", exception.Message, StringComparison.Ordinal);
    }

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
                ["ForwardedHeaders:AllowedHosts:0"] = "fooddiary.club",
                ["HttpsRedirection:Enabled"] = "true",
                ["RateLimiting:Auth:PermitLimit"] = "7",
                ["RateLimiting:Auth:WindowSeconds"] = "90",
                ["RateLimiting:Ai:PermitLimit"] = "11",
                ["RateLimiting:Ai:WindowSeconds"] = "120",
                ["RateLimiting:ClientTelemetry:PermitLimit"] = "61",
                ["RateLimiting:ClientTelemetry:WindowSeconds"] = "121",
                ["RateLimiting:MarketingAttribution:PermitLimit"] = "31",
                ["RateLimiting:MarketingAttribution:WindowSeconds"] = "91",
                ["RateLimiting:TestDelivery:PermitLimit"] = "4",
                ["RateLimiting:TestDelivery:WindowSeconds"] = "92",
                ["RateLimiting:Wearable:PermitLimit"] = "8",
                ["RateLimiting:Wearable:WindowSeconds"] = "93",
                ["RateLimiting:FoodData:PermitLimit"] = "29",
                ["RateLimiting:FoodData:WindowSeconds"] = "94",
                ["RateLimiting:SecretVerification:PermitLimit"] = "3",
                ["RateLimiting:SecretVerification:WindowSeconds"] = "95",
                ["RateLimiting:Billing:PermitLimit"] = "9",
                ["RateLimiting:Billing:WindowSeconds"] = "96",
                ["RateLimiting:Export:PermitLimit"] = "6",
                ["RateLimiting:Export:WindowSeconds"] = "97",
                ["OutputCache:AdminAiUsage:ExpirationSeconds"] = "30",
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        ApiCorsOptions cors = provider.GetRequiredService<IOptions<ApiCorsOptions>>().Value;
        ApiForwardedHeadersOptions forwardedHeaders = provider.GetRequiredService<IOptions<ApiForwardedHeadersOptions>>().Value;
        ApiHttpsRedirectionOptions httpsRedirection = provider.GetRequiredService<IOptions<ApiHttpsRedirectionOptions>>().Value;
        ApiRateLimitingOptions rateLimiting = provider.GetRequiredService<IOptions<ApiRateLimitingOptions>>().Value;
        ApiOutputCacheOptions outputCache = provider.GetRequiredService<IOptions<ApiOutputCacheOptions>>().Value;
        ForwardedHeadersOptions forwardedHeadersOptions = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>>().Value;

        Assert.Equal(["http://localhost:4200"], cors.Origins);
        Assert.Equal(2, forwardedHeaders.ForwardLimit);
        Assert.Equal(["10.0.0.10"], forwardedHeaders.KnownProxies);
        Assert.Equal(["10.0.0.0/24"], forwardedHeaders.KnownNetworks);
        Assert.Equal(["fooddiary.club"], forwardedHeaders.AllowedHosts);
        Assert.True(httpsRedirection.Enabled);
        Assert.Equal(7, rateLimiting.Auth.PermitLimit);
        Assert.Equal(90, rateLimiting.Auth.WindowSeconds);
        Assert.Equal(11, rateLimiting.Ai.PermitLimit);
        Assert.Equal(120, rateLimiting.Ai.WindowSeconds);
        Assert.Equal(61, rateLimiting.ClientTelemetry.PermitLimit);
        Assert.Equal(121, rateLimiting.ClientTelemetry.WindowSeconds);
        Assert.Equal(31, rateLimiting.MarketingAttribution.PermitLimit);
        Assert.Equal(91, rateLimiting.MarketingAttribution.WindowSeconds);
        Assert.Equal(4, rateLimiting.TestDelivery.PermitLimit);
        Assert.Equal(92, rateLimiting.TestDelivery.WindowSeconds);
        Assert.Equal(8, rateLimiting.Wearable.PermitLimit);
        Assert.Equal(93, rateLimiting.Wearable.WindowSeconds);
        Assert.Equal(29, rateLimiting.FoodData.PermitLimit);
        Assert.Equal(94, rateLimiting.FoodData.WindowSeconds);
        Assert.Equal(3, rateLimiting.SecretVerification.PermitLimit);
        Assert.Equal(95, rateLimiting.SecretVerification.WindowSeconds);
        Assert.Equal(9, rateLimiting.Billing.PermitLimit);
        Assert.Equal(96, rateLimiting.Billing.WindowSeconds);
        Assert.Equal(6, rateLimiting.Export.PermitLimit);
        Assert.Equal(97, rateLimiting.Export.WindowSeconds);
        Assert.Equal(30, outputCache.AdminAiUsage.ExpirationSeconds);
        Assert.Equal(2, forwardedHeadersOptions.ForwardLimit);
        Assert.True(forwardedHeadersOptions.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
        Assert.True(forwardedHeadersOptions.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
        Assert.Equal(["fooddiary.club"], forwardedHeadersOptions.AllowedHosts);
        Assert.Contains(forwardedHeadersOptions.KnownProxies, ip => string.Equals(ip.ToString(), "10.0.0.10", StringComparison.Ordinal));
        Assert.Contains(forwardedHeadersOptions.KnownIPNetworks, network => string.Equals(network.BaseAddress.ToString(), "10.0.0.0", StringComparison.Ordinal) && network.PrefixLength == 24);
    }

    [Fact]
    public void AddApiServices_BindsDataProtectionOptionsFromConfiguration() {
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
                ["DataProtection:ApplicationName"] = "FoodDiary.Tests",
                ["DataProtection:KeyRingPath"] = "/tmp/fooddiary-tests/data-protection-keys",
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        ApiDataProtectionOptions options = provider.GetRequiredService<IOptions<ApiDataProtectionOptions>>().Value;

        Assert.Equal("FoodDiary.Tests", options.ApplicationName);
        Assert.Equal("/tmp/fooddiary-tests/data-protection-keys", options.KeyRingPath);
    }

    [Fact]
    public void ApiDataProtectionOptions_HasValidApplicationName_ReturnsExpectedResult() {
        Assert.True(ApiDataProtectionOptions.HasValidApplicationName(new ApiDataProtectionOptions {
            ApplicationName = "FoodDiary.Web.Api",
        }));
        Assert.False(ApiDataProtectionOptions.HasValidApplicationName(new ApiDataProtectionOptions {
            ApplicationName = " ",
        }));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void ApiOutputCacheOptions_HasValidUserScoped_ReturnsExpectedResult(int expirationSeconds, bool expected) {
        var options = new ApiOutputCacheOptions {
            UserScoped = new ApiOutputCacheOptions.UserScopedCacheOptions {
                ExpirationSeconds = expirationSeconds,
            },
        };

        bool valid = ApiOutputCacheOptions.HasValidUserScoped(options);

        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData(1, 60, 0, true)]
    [InlineData(0, 60, 0, false)]
    [InlineData(1, 0, 0, false)]
    [InlineData(1, 60, -1, false)]
    public void ApiRateLimitingOptions_HasValidTestDelivery_ReturnsExpectedResult(
        int permitLimit,
        int windowSeconds,
        int queueLimit,
        bool expected) {
        var options = new ApiRateLimitingOptions {
            TestDelivery = new ApiRateLimitingOptions.FixedWindowPolicyOptions {
                PermitLimit = permitLimit,
                WindowSeconds = windowSeconds,
                QueueLimit = queueLimit,
            },
        };

        bool valid = ApiRateLimitingOptions.HasValidTestDelivery(options);

        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData(10, 60, 0, true)]
    [InlineData(0, 60, 0, false)]
    [InlineData(10, 0, 0, false)]
    [InlineData(10, 60, -1, false)]
    public void ApiRateLimitingOptions_HasValidWearable_ReturnsExpectedResult(
        int permitLimit,
        int windowSeconds,
        int queueLimit,
        bool expected) {
        var options = new ApiRateLimitingOptions {
            Wearable = new ApiRateLimitingOptions.FixedWindowPolicyOptions {
                PermitLimit = permitLimit,
                WindowSeconds = windowSeconds,
                QueueLimit = queueLimit,
            },
        };

        bool valid = ApiRateLimitingOptions.HasValidWearable(options);

        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData(30, 60, 0, true)]
    [InlineData(0, 60, 0, false)]
    [InlineData(30, 0, 0, false)]
    [InlineData(30, 60, -1, false)]
    public void ApiRateLimitingOptions_HasValidFoodData_ReturnsExpectedResult(
        int permitLimit,
        int windowSeconds,
        int queueLimit,
        bool expected) {
        var options = new ApiRateLimitingOptions {
            FoodData = new ApiRateLimitingOptions.FixedWindowPolicyOptions {
                PermitLimit = permitLimit,
                WindowSeconds = windowSeconds,
                QueueLimit = queueLimit,
            },
        };

        bool valid = ApiRateLimitingOptions.HasValidFoodData(options);

        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData("fooddiary.club", true)]
    [InlineData("localhost", true)]
    [InlineData("*", false)]
    [InlineData("", false)]
    [InlineData(" fooddiary.club", false)]
    public void ApiForwardedHeadersOptions_HasValidAllowedHosts_ReturnsExpectedResult(
        string host,
        bool expected) {
        var options = new ApiForwardedHeadersOptions { AllowedHosts = [host] };

        bool valid = ApiForwardedHeadersOptions.HasValidAllowedHosts(options);

        Assert.Equal(expected, valid);
    }

    [Fact]
    public void AddApiServices_WithInvalidTestDeliveryRateLimit_FailsOptionsValidation() {
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
                ["RateLimiting:TestDelivery:PermitLimit"] = "0",
            })
            .Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ApiRateLimitingOptions>>().Value);
        Assert.Contains("RateLimiting:TestDelivery", exception.Message, StringComparison.Ordinal);
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

}
