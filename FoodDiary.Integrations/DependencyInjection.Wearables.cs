using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Integrations.Wearables;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static void AddWearableIntegrations(this IServiceCollection services) {
        services.AddHttpClient<FitbitClient>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IWearableClient>(sp => sp.GetRequiredService<FitbitClient>());
    }
}
