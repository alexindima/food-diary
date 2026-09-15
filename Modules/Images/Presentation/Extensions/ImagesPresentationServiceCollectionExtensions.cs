using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Images.Presentation.Extensions;

public static class ImagesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddImagesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ImagesPresentationServiceCollectionExtensions).Assembly);
    }
}
