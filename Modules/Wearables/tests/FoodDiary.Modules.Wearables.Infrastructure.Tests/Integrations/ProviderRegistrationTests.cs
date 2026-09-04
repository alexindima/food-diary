using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Integrations.Wearables;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderRegistrationTests {
    [Fact]
    public void AddProvider_ResolvesIndependentlyAndPreservesClientContract() {
        var services = new ServiceCollection();
        services.AddLogging();
        TimeProvider clock = TimeProvider.System;
        services.AddSingleton(clock);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["Fitbit:ClientId"] = "client",
            ["Fitbit:ClientSecret"] = "test-secret",
            ["Fitbit:RedirectUri"] = "https://app.test/fitbit",
        }).Build();

        Assert.Same(services, services.AddWearablesProvider(configuration));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using IServiceScope scope = provider.CreateScope();
        IWearableClient client = scope.ServiceProvider.GetRequiredService<IWearableClient>();

        Assert.Multiple(
            () => Assert.IsType<FitbitClient>(client),
            () => Assert.Equal("FoodDiary.Modules.Wearables.Infrastructure", client.GetType().Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Integrations.Wearables.FitbitClient", client.GetType().FullName),
            () => Assert.Equal("FoodDiary.Modules.Wearables.Infrastructure", typeof(FitbitOptions).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Integrations.Options.FitbitOptions", typeof(FitbitOptions).FullName),
            () => Assert.Same(clock, provider.GetRequiredService<TimeProvider>()));
        using HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(FitbitClient));
        Assert.Equal(TimeSpan.FromSeconds(30), httpClient.Timeout);
        Assert.Same(client, scope.ServiceProvider.GetRequiredService<IWearableClient>());
    }

    [Fact]
    public void AddProvider_InvalidConfigurationRetainsValidationMessage() {
        var services = new ServiceCollection();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["Fitbit:ClientId"] = "partial",
        }).Build();
        services.AddWearablesProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException failure = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FitbitOptions>>().Value);

        Assert.Equal(["Fitbit configuration must be empty or include ClientId, ClientSecret, and an HTTPS RedirectUri (HTTP is allowed only for loopback)."], failure.Failures, StringComparer.Ordinal);
    }
}
