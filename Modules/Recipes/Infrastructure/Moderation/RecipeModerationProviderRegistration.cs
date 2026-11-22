using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

public static class RecipeModerationProviderRegistration {
    public static IServiceCollection AddRecipeModerationProviders(this IServiceCollection services, IConfiguration configuration) {
        AddModerationHttpClient(services, nameof(OpenAiRecipeModerationProvider))
            .AddTypedClient((client, provider) => new OpenAiRecipeModerationProvider(
                client,
                provider.GetRequiredService<IImageAssetContentService>(),
                configuration["OpenAi:ApiKey"]));
        AddModerationHttpClient(services, nameof(MistralRecipeModerationProvider))
            .AddTypedClient((client, _) => new MistralRecipeModerationProvider(client, configuration["Mistral:ApiKey"]));
        services.AddTransient<IRecipeModerationProvider>(static provider => provider.GetRequiredService<OpenAiRecipeModerationProvider>());
        services.AddTransient<IRecipeModerationProvider>(static provider => provider.GetRequiredService<MistralRecipeModerationProvider>());

        return services;
    }

    private static IHttpClientBuilder AddModerationHttpClient(IServiceCollection services, string name) =>
        services.AddHttpClient(name)
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler {
                AllowAutoRedirect = false,
                UseCookies = false,
            });
}
