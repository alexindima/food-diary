using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Modules.Fasting.Application;
using FoodDiary.Modules.Fasting.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Fasting.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddFastingModule(this IServiceCollection services) {
        services.AddFastingApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<FastingDbContext>(static options => new FastingDbContext(options)));
        services.AddScoped<IFastingPlanRepository>(static provider => new FastingPlanRepository(
            provider.GetRequiredService<FastingDbContext>().FastingPlans));
        services.AddScoped<IFastingPlanReadRepository>(static provider => provider.GetRequiredService<IFastingPlanRepository>());
        services.AddScoped<IFastingPlanWriteRepository>(static provider => provider.GetRequiredService<IFastingPlanRepository>());
        services.AddScoped<IFastingOccurrenceRepository>(static provider => new FastingOccurrenceRepository(
            provider.GetRequiredService<FastingDbContext>().FastingOccurrences, provider.GetRequiredService<IUserFastingReminderReadService>()));
        services.AddScoped<IFastingOccurrenceReadRepository>(static provider => provider.GetRequiredService<IFastingOccurrenceRepository>());
        services.AddScoped<IFastingOccurrenceReadModelRepository>(static provider => provider.GetRequiredService<IFastingOccurrenceRepository>());
        services.AddScoped<IFastingOccurrenceWriteRepository>(static provider => provider.GetRequiredService<IFastingOccurrenceRepository>());
        services.AddScoped<IFastingCheckInRepository>(static provider => new FastingCheckInRepository(
            provider.GetRequiredService<FastingDbContext>().FastingCheckIns));
        services.AddScoped<IFastingCheckInReadRepository>(static provider => provider.GetRequiredService<IFastingCheckInRepository>());
        services.AddScoped<IFastingCheckInReadModelRepository>(static provider => provider.GetRequiredService<IFastingCheckInRepository>());
        services.AddScoped<IFastingCheckInWriteRepository>(static provider => provider.GetRequiredService<IFastingCheckInRepository>());
        services.AddScoped<IFastingSessionRepository>(static provider => new FastingSessionRepository(
            provider.GetRequiredService<FastingDbContext>().FastingSessions, provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IFastingSessionReadRepository>(static provider => provider.GetRequiredService<IFastingSessionRepository>());
        services.AddScoped<IFastingSessionWriteRepository>(static provider => provider.GetRequiredService<IFastingSessionRepository>());
        services.AddScoped<IFastingTelemetryEventRepository>(static provider => new FastingTelemetryEventRepository(
            provider.GetRequiredService<FastingDbContext>().FastingTelemetryEvents));
        services.AddScoped<IFastingTelemetryEventReadRepository>(static provider => provider.GetRequiredService<IFastingTelemetryEventRepository>());
        services.AddScoped<IFastingTelemetryEventWriteRepository>(static provider => provider.GetRequiredService<IFastingTelemetryEventRepository>());

        return services;
    }
}
