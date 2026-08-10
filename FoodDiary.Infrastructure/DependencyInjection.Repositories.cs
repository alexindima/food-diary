using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddFeatureRepositories(this IServiceCollection services) {
        services.AddUserPersistence();
        services.AddFoodPersistence();
        services.AddDashboardReadServices();
        services.AddShoppingListPersistence();
        services.AddTrackingPersistence();
        services.AddImagePersistence();
        services.AddAiPersistence();
        services.AddDietologistPersistence();
        services.AddNotificationPersistence();
        services.AddProviderCachePersistence();
        services.AddFastingPersistence();
        services.AddMarketingPersistence();
        services.AddFavoritesPersistence();
        services.AddLearningPersistence();
        services.AddRecipeInteractionPersistence();
        services.AddModerationPersistence();
        services.AddUsdaPersistence();
        services.AddAchievementPersistence();
        services.AddWeeklyGoalPersistence();

    }
}
