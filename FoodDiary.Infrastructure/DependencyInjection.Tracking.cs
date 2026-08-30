using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Infrastructure.Persistence.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddTrackingPersistence(this IServiceCollection services) {
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<IWeightEntryReadRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWeightEntryReadModelRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWeightEntryWriteRepository>(static provider => provider.GetRequiredService<IWeightEntryRepository>());
        services.AddScoped<IWaistEntryRepository, WaistEntryRepository>();
        services.AddScoped<IWaistEntryReadRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        services.AddScoped<IWaistEntryReadModelRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        services.AddScoped<IWaistEntryWriteRepository>(static provider => provider.GetRequiredService<IWaistEntryRepository>());
        services.AddScoped<IExerciseEntryRepository, ExerciseEntryRepository>();
        services.AddScoped<IExerciseEntryReadRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryReadModelRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());
        services.AddScoped<IExerciseEntryWriteRepository>(static provider => provider.GetRequiredService<IExerciseEntryRepository>());

    }
}
