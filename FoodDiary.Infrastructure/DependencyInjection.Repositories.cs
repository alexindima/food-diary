using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddFeatureRepositories(this IServiceCollection services) {
        services.AddUserPersistence();
        services.AddAuditPersistence();
        services.AddFoodPersistence();
        services.AddDashboardReadServices();
        services.AddShoppingListPersistence();
        services.AddTrackingPersistence();
        services.AddAiPersistence();
        services.AddNotificationPersistence();
        services.AddMarketingPersistence();
        services.AddLearningPersistence();
        services.AddRecipeInteractionPersistence();
        services.AddModerationPersistence();
        services.AddUsdaPersistence();
    }
}
