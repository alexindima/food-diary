using FluentValidation;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Exercises;

public static class DependencyInjection {
    public static IServiceCollection AddExercisesModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IExerciseEntryReadService, ExerciseEntryReadService>();
        return services;
    }
}
