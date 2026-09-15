using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Gamification.Contracts.Achievements.Common;
using FoodDiary.Modules.Gamification.Application;
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
        services.AddScoped<IAchievementDefinitionReadModelRepository>(static provider => (AchievementDefinitionStore)provider.GetRequiredService<IAchievementDefinitionStore>());
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
            IModuleScopeGuard scopeGuard = provider.GetRequiredService<IModuleScopeGuard>();
            return new AchievementEvaluationOutboxProcessor(owned, owned.AchievementEvaluationOutbox,
                provider.GetRequiredService<FoodDiary.Mediator.ISender>(), provider.GetRequiredService<IOptions<OutboxProcessingOptions>>(),
                provider.GetRequiredService<TimeProvider>(), provider.GetRequiredService<ILogger<AchievementEvaluationOutboxProcessor>>(),
                scopeGuard.EnsureCleanEntry);
        });
        return services;
    }
}
