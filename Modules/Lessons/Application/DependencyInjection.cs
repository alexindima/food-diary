using FluentValidation;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Lessons;

public static class DependencyInjection {
    public static IServiceCollection AddLessonsApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<ILessonReadService, LessonReadService>();
        return services;
    }
}
