using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Lessons;
using FoodDiary.Modules.Lessons.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Lessons.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddLessonsModule(this IServiceCollection services) {
        services.AddLessonsApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<LessonsDbContext>(static options => new LessonsDbContext(options)));
        services.AddScoped<INutritionLessonRepository>(static provider => new NutritionLessonRepository(
            provider.GetRequiredService<LessonsDbContext>().NutritionLessons,
            provider.GetRequiredService<LessonsDbContext>().UserLessonProgress));
        services.AddScoped<INutritionLessonReadRepository>(static provider => provider.GetRequiredService<INutritionLessonRepository>());
        services.AddScoped<INutritionLessonReadModelRepository>(static provider => provider.GetRequiredService<INutritionLessonRepository>());
        services.AddScoped<INutritionLessonWriteRepository>(static provider => provider.GetRequiredService<INutritionLessonRepository>());
        return services;
    }
}
