using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Infrastructure.Persistence.Tracking;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddFastingPersistence(this IServiceCollection services) {
        services.AddScoped<IFastingPlanRepository, FastingPlanRepository>();
        services.AddScoped<IFastingPlanReadRepository>(static provider => provider.GetRequiredService<IFastingPlanRepository>());
        services.AddScoped<IFastingPlanWriteRepository>(static provider => provider.GetRequiredService<IFastingPlanRepository>());
        services.AddScoped<IFastingOccurrenceRepository, FastingOccurrenceRepository>();
        services.AddScoped<IFastingOccurrenceReadRepository>(static provider => provider.GetRequiredService<IFastingOccurrenceRepository>());
        services.AddScoped<IFastingOccurrenceReadModelRepository>(static provider => provider.GetRequiredService<IFastingOccurrenceRepository>());
        services.AddScoped<IFastingOccurrenceWriteRepository>(static provider => provider.GetRequiredService<IFastingOccurrenceRepository>());
        services.AddScoped<IFastingCheckInRepository, FastingCheckInRepository>();
        services.AddScoped<IFastingCheckInReadRepository>(static provider => provider.GetRequiredService<IFastingCheckInRepository>());
        services.AddScoped<IFastingCheckInReadModelRepository>(static provider => provider.GetRequiredService<IFastingCheckInRepository>());
        services.AddScoped<IFastingCheckInWriteRepository>(static provider => provider.GetRequiredService<IFastingCheckInRepository>());
        services.AddScoped<IFastingSessionRepository, FastingSessionRepository>();
        services.AddScoped<IFastingSessionReadRepository>(static provider => provider.GetRequiredService<IFastingSessionRepository>());
        services.AddScoped<IFastingSessionWriteRepository>(static provider => provider.GetRequiredService<IFastingSessionRepository>());
        services.AddScoped<IFastingTelemetryEventRepository, FastingTelemetryEventRepository>();
        services.AddScoped<IFastingTelemetryEventReadRepository>(static provider => provider.GetRequiredService<IFastingTelemetryEventRepository>());
        services.AddScoped<IFastingTelemetryEventWriteRepository>(static provider => provider.GetRequiredService<IFastingTelemetryEventRepository>());

        return services;
    }
}
