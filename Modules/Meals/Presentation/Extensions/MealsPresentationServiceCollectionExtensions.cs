using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Meals.Presentation.Extensions;

public static class MealsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMealsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(MealsPresentationServiceCollectionExtensions).Assembly);
    }
}
