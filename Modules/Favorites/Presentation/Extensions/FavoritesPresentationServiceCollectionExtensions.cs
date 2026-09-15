using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Favorites.Presentation.Extensions;

public static class FavoritesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddFavoritesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(FavoritesPresentationServiceCollectionExtensions).Assembly);
    }
}
