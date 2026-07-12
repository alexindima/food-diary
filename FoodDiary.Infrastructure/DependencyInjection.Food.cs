using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddFoodPersistence(this IServiceCollection services) {
        return services
            .AddProductsPersistence()
            .AddRecipesPersistence()
            .AddRecentItemsPersistence()
            .AddMealsPersistence();
    }
}
