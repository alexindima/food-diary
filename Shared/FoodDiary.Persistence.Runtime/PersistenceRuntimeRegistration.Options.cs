using FoodDiary.Persistence.Runtime.Options;
using FoodDiary.Outbox.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Persistence.Runtime;

public static partial class PersistenceRuntimeRegistration {
    private static void AddPersistenceOptions(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(static options => !options.EnableRetries || options.MaxRetryCount > 0,
                "Database:MaxRetryCount must be greater than zero when retries are enabled.")
            .Validate(static options => !options.EnableRetries || options.MaxRetryDelaySeconds > 0,
                "Database:MaxRetryDelaySeconds must be greater than zero when retries are enabled.")
            .ValidateOnStart();

        services.AddOptions<OutboxProcessingOptions>()
            .Bind(configuration.GetSection(OutboxProcessingOptions.SectionName))
            .Validate(OutboxProcessingOptions.HasValidConfiguration,
                "OutboxProcessing requires positive durations and LeaseDuration must cover DispatchTimeout, FinalizationTimeout, and the safety margin.")
            .ValidateOnStart();
    }
}
