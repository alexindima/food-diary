using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class RecipesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddRecipesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(RecipesPresentationServiceCollectionExtensions).Assembly);
    }
}
