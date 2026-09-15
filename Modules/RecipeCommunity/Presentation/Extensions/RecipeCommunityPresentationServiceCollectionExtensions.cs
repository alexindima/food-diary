using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.RecipeCommunity.Presentation.Extensions;

public static class RecipeCommunityPresentationServiceCollectionExtensions {
    public static IServiceCollection AddRecipeCommunityPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(RecipeCommunityPresentationServiceCollectionExtensions).Assembly);
    }
}
