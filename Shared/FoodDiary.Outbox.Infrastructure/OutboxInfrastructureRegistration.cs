using FoodDiary.Outbox.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Outbox.Infrastructure;

public static class OutboxInfrastructureRegistration {
    public static IServiceCollection AddOutboxProcessing(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<OutboxProcessingOptions>()
            .Bind(configuration.GetSection(OutboxProcessingOptions.SectionName))
            .Validate(OutboxProcessingOptions.HasValidConfiguration,
                "OutboxProcessing requires positive durations and LeaseDuration must cover DispatchTimeout, FinalizationTimeout, and the safety margin.")
            .ValidateOnStart();
        return services;
    }
}
