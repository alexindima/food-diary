using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class MealPlanningPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMealPlanningPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(MealPlanningPresentationServiceCollectionExtensions).Assembly);
    }
}
