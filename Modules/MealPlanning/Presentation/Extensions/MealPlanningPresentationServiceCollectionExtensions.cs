using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.MealPlanning.Presentation.Extensions;

public static class MealPlanningPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMealPlanningPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(MealPlanningPresentationServiceCollectionExtensions).Assembly);
    }
}
