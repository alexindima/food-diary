using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Lessons.Presentation.Extensions;

public static class LessonsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddLessonsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(LessonsPresentationServiceCollectionExtensions).Assembly);
    }
}
