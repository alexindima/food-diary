using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Gamification;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Gamification.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Gamification.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddGamificationModule(this IServiceCollection services) {
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<GamificationDbContext>(static options => new GamificationDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxReplayStream, AchievementEvaluationOutboxReplayStream>());
        services.AddGamificationApplication();
        services.AddScoped<IAchievementDefinitionStore>(static provider => {
            GamificationDbContext owned = provider.GetRequiredService<GamificationDbContext>();
            IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
            return new AchievementDefinitionStore(owned, owned.AchievementDefinitions, owned.UserAchievements, () => coordinator.CurrentTransaction);
        });
        services.AddScoped<IUserAchievementStore>(static provider => {
            GamificationDbContext owned = provider.GetRequiredService<GamificationDbContext>();
            IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
            return new UserAchievementStore(owned, owned.UserAchievements, () => coordinator.CurrentTransaction);
        });
        services.AddScoped<IAchievementEvaluationOutbox>(static provider => {
            GamificationDbContext owned = provider.GetRequiredService<GamificationDbContext>();
            IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
            return new AchievementEvaluationOutbox(owned, owned.AchievementEvaluationOutbox, provider.GetRequiredService<TimeProvider>(), () => coordinator.CurrentTransaction);
        });
        services.AddScoped<IMealAchievementEvaluationRequest>(static provider => (AchievementEvaluationOutbox)provider.GetRequiredService<IAchievementEvaluationOutbox>());
        services.AddScoped<IAchievementEvaluationOutboxProcessor>(static provider => {
            GamificationDbContext owned = provider.GetRequiredService<GamificationDbContext>();
            FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
            return new AchievementEvaluationOutboxProcessor(owned, owned.AchievementEvaluationOutbox,
                provider.GetRequiredService<IAchievementReconciliationHandler>(), provider.GetRequiredService<IOptions<OutboxProcessingOptions>>(),
                provider.GetRequiredService<TimeProvider>(), provider.GetRequiredService<ILogger<AchievementEvaluationOutboxProcessor>>(),
                () => OutboxProcessingEngine.EnsureCleanEntry(shared));
        });
        return services;
    }
}
