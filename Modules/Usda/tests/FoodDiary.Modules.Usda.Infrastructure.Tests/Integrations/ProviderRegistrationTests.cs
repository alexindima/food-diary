using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Modules.Usda.Infrastructure;
using FoodDiary.Integrations.Services;
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

        }).Build();

        Assert.Same(services, services.AddUsdaProvider(configuration));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using IServiceScope scope = provider.CreateScope();
        IUsdaFoodSearchService client = scope.ServiceProvider.GetRequiredService<IUsdaFoodSearchService>();

        Assert.Multiple(
            () => Assert.IsType<UsdaFoodSearchService>(client),
            () => Assert.Equal("FoodDiary.Modules.Usda.Infrastructure", client.GetType().Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Integrations.Services.UsdaFoodSearchService", client.GetType().FullName),
            () => Assert.Equal("FoodDiary.Modules.Usda.Infrastructure", typeof(UsdaApiOptions).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Integrations.Options.UsdaApiOptions", typeof(UsdaApiOptions).FullName),
            () => Assert.Same(clock, provider.GetRequiredService<TimeProvider>()));
        using HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IUsdaFoodSearchService));
        Assert.Equal(TimeSpan.FromSeconds(15), httpClient.Timeout);
        Assert.Same(provider.GetRequiredService<UsdaFoodDetailCache>(), scope.ServiceProvider.GetRequiredService<UsdaFoodDetailCache>());
    }

    [Fact]
    public void AddProvider_InvalidConfigurationRetainsValidationMessage() {
        var services = new ServiceCollection();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["UsdaApi:BaseUrl"] = "http://invalid.test",
        }).Build();
        services.AddUsdaProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException failure = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<UsdaApiOptions>>().Value);

        Assert.Equal(["UsdaApi:BaseUrl must be an absolute HTTPS URL."], failure.Failures, StringComparer.Ordinal);
    }
}
