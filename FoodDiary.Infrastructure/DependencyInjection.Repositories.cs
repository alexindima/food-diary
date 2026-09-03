using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddFeatureRepositories(this IServiceCollection services) {
        services.AddAuditPersistence();
        services.AddFoodPersistence();
        services.AddEmailPersistence();
        services.AddModerationPersistence();
    }
}
