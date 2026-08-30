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
        services.AddEmailPersistence();
        services.AddLearningPersistence();
        services.AddRecipeInteractionPersistence();
        services.AddModerationPersistence();
    }
}
