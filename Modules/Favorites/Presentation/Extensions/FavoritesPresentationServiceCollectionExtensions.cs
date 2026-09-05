using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class FavoritesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddFavoritesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(FavoritesPresentationServiceCollectionExtensions).Assembly);
    }
}
