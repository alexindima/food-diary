using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Wearables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Infrastructure;

public static class WearablesProviderRegistration {
    public static IServiceCollection AddWearablesProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<FitbitOptions>()
            .Bind(configuration.GetSection(FitbitOptions.SectionName))
            .Validate(FitbitOptions.IsEmptyOrComplete,
                "Fitbit configuration must be empty or include ClientId, ClientSecret, and an HTTPS RedirectUri (HTTP is allowed only for loopback).")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        FitbitOptions options = configuration
            .GetSection(FitbitOptions.SectionName)
            .Get<FitbitOptions>() ?? new FitbitOptions();
        if (!FitbitOptions.HasCompleteConfiguration(options)) {
            return services;
        }

        services.AddHttpClient<FitbitClient>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IWearableClient>(sp => sp.GetRequiredService<FitbitClient>());

        return services;
    }
}
