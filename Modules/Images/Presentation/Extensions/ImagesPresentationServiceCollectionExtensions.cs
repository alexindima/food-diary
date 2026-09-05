using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class ImagesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddImagesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ImagesPresentationServiceCollectionExtensions).Assembly);
    }
}
