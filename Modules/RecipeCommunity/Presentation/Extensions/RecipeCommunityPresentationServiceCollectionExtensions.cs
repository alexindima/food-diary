using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class RecipeCommunityPresentationServiceCollectionExtensions {
    public static IServiceCollection AddRecipeCommunityPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(RecipeCommunityPresentationServiceCollectionExtensions).Assembly);
    }
}
