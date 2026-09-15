using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Dietologist.Presentation.Extensions;

public static class DietologistPresentationServiceCollectionExtensions {
    public static IServiceCollection AddDietologistPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(DietologistPresentationServiceCollectionExtensions).Assembly);
    }
}
