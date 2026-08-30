using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Infrastructure.Persistence.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddTrackingPersistence(this IServiceCollection services) {
        services.AddScoped<IExerciseEntryRepository, ExerciseEntryRepository>();
        services.AddScoped<IExerciseEntryReadRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryReadModelRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryWriteRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());

    }
}
