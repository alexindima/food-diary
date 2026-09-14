using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Integrations;

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

        Assert.Same(services, services.AddAiProvider(configuration));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using IServiceScope scope = provider.CreateScope();
        IOpenAiFoodClient client = scope.ServiceProvider.GetRequiredService<IOpenAiFoodClient>();

        Assert.Multiple(
            () => Assert.IsType<OpenAiFoodClient>(client),
            () => Assert.Equal("FoodDiary.Modules.Ai.Infrastructure", client.GetType().Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi.OpenAiFoodClient", client.GetType().FullName),
            () => Assert.Equal("FoodDiary.Modules.Ai.Infrastructure", typeof(OpenAiOptions).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Modules.Ai.Infrastructure.Providers.Options.OpenAiOptions", typeof(OpenAiOptions).FullName),
            () => Assert.Same(clock, provider.GetRequiredService<TimeProvider>()));
        using HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IOpenAiFoodClient));
        Assert.Equal(TimeSpan.FromSeconds(60), httpClient.Timeout);

    }

    [Fact]
    public void AddProvider_InvalidConfigurationRetainsValidationMessage() {
        var services = new ServiceCollection();
        services.AddLogging();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["OpenAi:MaxOutputTokens"] = "0",
        }).Build();
        services.AddAiProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException failure = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<OpenAiOptions>>().Value);

        Assert.Equal(["OpenAi:MaxOutputTokens must be between 1 and 32768."], failure.Failures, StringComparer.Ordinal);
    }
}
