using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Ai.Infrastructure;

public static class ModuleRegistration {
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static IServiceCollection AddAiPersistence(this IServiceCollection services) {
        services.AddMemoryCache();
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<AiDbContext>(options => new AiDbContext(options)));
        services.AddScoped(provider => provider.GetRequiredService<IIndependentModuleContextOptionsFactory>()
            .CreateOptions<AiDbContext>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, AiUserDataPurgeParticipant>());
        services.AddSingleton<IAiPromptProvider, AiPromptProvider>();
        services.AddSingleton<IAiPromptPreviewRenderer, FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi.AiPromptPreviewRenderer>();
        services.AddScoped<IAiQuotaRepository, AiQuotaRepository>();
        services.AddScoped<FoodRecognitionJobStore>();
        services.AddScoped<IFoodRecognitionJobStore>(provider => provider.GetRequiredService<FoodRecognitionJobStore>());
        services.AddScoped<IFoodRecognitionJobReader>(provider => provider.GetRequiredService<FoodRecognitionJobStore>());
        services.AddScoped<AiPromptTemplateRepository>(provider => new AiPromptTemplateRepository(
            provider.GetRequiredService<AiDbContext>().AiPromptTemplates, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IAiPromptTemplateReadModelRepository>(static provider => provider.GetRequiredService<AiPromptTemplateRepository>());
        services.AddScoped<IAiPromptTemplateWriteRepository>(static provider => provider.GetRequiredService<AiPromptTemplateRepository>());

        return services;
    }

    private static Func<CancellationToken, Task> CreateTransactionSynchronizer(IServiceProvider provider) {
        IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
        AiDbContext owned = provider.GetRequiredService<AiDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
            }
        };
    }

    public static IServiceCollection AddAiModule(this IServiceCollection services) {
        FoodDiary.Modules.Ai.Application.DependencyInjection.AddAiApplication(services);
        return services.AddAiPersistence();
    }
}
