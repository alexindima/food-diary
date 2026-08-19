using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Wearables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static void AddWearableIntegrations(this IServiceCollection services, IConfiguration configuration) {
        FitbitOptions options = configuration
            .GetSection(FitbitOptions.SectionName)
            .Get<FitbitOptions>() ?? new FitbitOptions();
        if (!FitbitOptions.HasCompleteConfiguration(options)) {
            return;
        }

        services.AddHttpClient<FitbitClient>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IWearableClient>(sp => sp.GetRequiredService<FitbitClient>());
    }
}
