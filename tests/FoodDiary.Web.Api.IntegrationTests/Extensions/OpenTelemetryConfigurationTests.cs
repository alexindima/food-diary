using FoodDiary.Web.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class OpenTelemetryConfigurationTests {
    [Fact]
    public void AddApiServices_WithoutOtlpEndpoint_DoesNotRegisterTelemetryProviders() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(otlpEndpoint: null);

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
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
        services.AddSingleton<IConfiguration>(configuration);
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
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiServices(configuration);
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<TracerProvider>());
        Assert.Contains(exception.Failures, failure => failure.Contains("OpenTelemetry:Otlp:Endpoint", StringComparison.Ordinal));
    }

    private static IConfiguration CreateConfiguration(string? otlpEndpoint) {
        var values = new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary;Username=postgres;Password=test",
            ["Jwt:SecretKey"] = "change-me-local-jwt-secret-min-32",
            ["Jwt:Issuer"] = "FoodDiaryApi",
            ["Jwt:Audience"] = "FoodDiaryClient",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["TelegramBot:ApiSecret"] = "",
            ["OpenTelemetry:Otlp:Endpoint"] = otlpEndpoint,
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
