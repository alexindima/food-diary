using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Integrations.Services;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderRegistrationTests {
    [Fact]
    public void AddProvider_ResolvesIndependentlyAndPreservesClientContract() {
        var services = new ServiceCollection();
        services.AddLogging();
        TimeProvider clock = TimeProvider.System;
        services.AddSingleton(clock);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {

        }).Build();

        Assert.Same(services, services.AddOpenFoodFactsProvider(configuration));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using IServiceScope scope = provider.CreateScope();
        IOpenFoodFactsService client = scope.ServiceProvider.GetRequiredService<IOpenFoodFactsService>();

        Assert.Multiple(
            () => Assert.IsType<OpenFoodFactsService>(client),
            () => Assert.Equal("FoodDiary.Modules.OpenFoodFacts.Infrastructure", client.GetType().Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Integrations.Services.OpenFoodFactsService", client.GetType().FullName),
            () => Assert.Equal("FoodDiary.Modules.OpenFoodFacts.Infrastructure", typeof(OpenFoodFactsApiOptions).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Integrations.Options.OpenFoodFactsApiOptions", typeof(OpenFoodFactsApiOptions).FullName),
            () => Assert.Same(clock, provider.GetRequiredService<TimeProvider>()));
        using HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IOpenFoodFactsService));
        Assert.Equal(TimeSpan.FromSeconds(10), httpClient.Timeout);

    }

    [Fact]
    public void AddProvider_InvalidConfigurationRetainsValidationMessage() {
        var services = new ServiceCollection();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["OpenFoodFacts:BaseUrl"] = "http://invalid.test",
        }).Build();
        services.AddOpenFoodFactsProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException failure = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<OpenFoodFactsApiOptions>>().Value);

        Assert.Equal(["OpenFoodFacts:BaseUrl must be an absolute HTTPS URL."], failure.Failures, StringComparer.Ordinal);
    }
}
