using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Exercises.Presentation.Extensions;

public static class ExercisesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddExercisesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ExercisesPresentationServiceCollectionExtensions).Assembly);
    }
}
