using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddFoodPersistence(this IServiceCollection services) {
        services.AddRecentItemsPersistence();
        services.AddMealsPersistence();
    }
}
