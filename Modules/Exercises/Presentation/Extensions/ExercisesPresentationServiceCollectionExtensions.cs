using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class ExercisesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddExercisesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ExercisesPresentationServiceCollectionExtensions).Assembly);
    }
}
