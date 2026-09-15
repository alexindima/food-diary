using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Recipes.Presentation.Extensions;

public static class RecipesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddRecipesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(RecipesPresentationServiceCollectionExtensions).Assembly);
    }
}
