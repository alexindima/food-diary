using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Modules.Exercises.Infrastructure.Persistence;
using FoodDiary.Application.Exercises;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Exercises.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddExercisesModule(this IServiceCollection services) {
        services.AddExercisesApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<ExercisesDbContext>(static options => new ExercisesDbContext(options)));
        services.AddScoped<IExerciseEntryRepository>(static provider => new ExerciseEntryRepository(
            provider.GetRequiredService<ExercisesDbContext>().ExerciseEntries));
        services.AddScoped<IExerciseEntryReadRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryReadModelRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryWriteRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        return services;
    }
}
