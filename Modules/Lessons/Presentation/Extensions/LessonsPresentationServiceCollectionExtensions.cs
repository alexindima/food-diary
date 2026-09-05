using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class LessonsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddLessonsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(LessonsPresentationServiceCollectionExtensions).Assembly);
    }
}
