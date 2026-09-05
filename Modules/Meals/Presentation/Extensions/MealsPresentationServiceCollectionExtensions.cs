using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class MealsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddMealsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(MealsPresentationServiceCollectionExtensions).Assembly);
    }
}
