using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Modules.Exercises.Infrastructure.Persistence;
using FoodDiary.Application.Exercises;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Exercises.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddExercisesModule(this IServiceCollection services) {
        services.AddExercisesApplication();
        services.AddScoped<IExerciseEntryRepository, ExerciseEntryRepository>();
        services.AddScoped<IExerciseEntryReadRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryReadModelRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryWriteRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        return services;
    }
}
