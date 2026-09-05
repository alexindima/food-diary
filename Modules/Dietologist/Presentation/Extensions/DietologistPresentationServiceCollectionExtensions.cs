using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class DietologistPresentationServiceCollectionExtensions {
    public static IServiceCollection AddDietologistPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(DietologistPresentationServiceCollectionExtensions).Assembly);
    }
}
