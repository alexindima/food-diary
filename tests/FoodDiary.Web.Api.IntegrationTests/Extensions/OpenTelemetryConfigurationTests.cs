using FoodDiary.Web.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class OpenTelemetryConfigurationTests {
    [Fact]
    public void AddApiServices_WithoutOtlpEndpoint_DoesNotRegisterTelemetryProviders() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(otlpEndpoint: null);

        services.AddLogging();
        services.AddApiServices(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<TracerProvider>());
        Assert.Null(provider.GetService<MeterProvider>());
    }

    [Fact]
    public void AddApiServices_WithValidOtlpEndpoint_RegistersTelemetryProviders() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("http://localhost:4317");

        services.AddLogging();
        services.AddApiServices(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<TracerProvider>());
        Assert.NotNull(provider.GetService<MeterProvider>());
    }

    [Fact]
    public void AddApiServices_WithInvalidOtlpEndpoint_Throws() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("not-a-uri");

        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddApiServices(configuration));
        Assert.Equal("OpenTelemetry:Otlp:Endpoint must be a valid absolute URI.", exception.Message);
    }

    private static IConfiguration CreateConfiguration(string? otlpEndpoint) {
        var values = new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
            ["JwtSettings:SecretKey"] = "change-me-local-jwt-secret-min-32",
            ["JwtSettings:Issuer"] = "FoodDiaryApi",
            ["JwtSettings:Audience"] = "FoodDiaryClient",
            ["JwtSettings:ExpirationMinutes"] = "60",
            ["JwtSettings:RefreshTokenExpirationDays"] = "7",
            ["TelegramBot:ApiSecret"] = "",
            ["OpenTelemetry:Otlp:Endpoint"] = otlpEndpoint,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
