using FoodDiary.Persistence.Runtime.Options;
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
    }
}
