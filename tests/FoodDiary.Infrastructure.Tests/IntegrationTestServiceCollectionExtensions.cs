using FoodDiary.Integrations;
using FoodDiary.Modules.Admin.Infrastructure.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
internal static class IntegrationTestServiceCollectionExtensions {
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services.AddBillingIntegrations(configuration)
            .AddAdminMailInboxIntegration(configuration)
            .AddMailRelayIntegration(configuration);
}
