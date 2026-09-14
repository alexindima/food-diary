using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Ai.Infrastructure;

public static class ModuleRegistration {
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static IServiceCollection AddAiPersistence(this IServiceCollection services) {
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<AiDbContext>(options => new AiDbContext(options)));
        services.AddScoped(provider => new DbContextOptions<AiDbContext>(provider.GetRequiredService<DbContextOptions<FoodDiaryDbContext>>()
            .Extensions.ToDictionary(extension => extension.GetType(), extension => extension)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, AiUserDataPurgeParticipant>());
        services.AddSingleton<IAiPromptProvider, AiPromptProvider>();
        services.AddScoped<IAiUsageWriteRepository>(provider => new AiUsageRepository(
            provider.GetRequiredService<AiDbContext>().AiUsages, CreateTransactionSynchronizer(provider)));
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
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        AiDbContext owned = provider.GetRequiredService<AiDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(shared.Database.CurrentTransaction?.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
            }
        };
    }

    public static IServiceCollection AddAiModule(this IServiceCollection services) {
        FoodDiary.Modules.Ai.Application.DependencyInjection.AddAiApplication(services);
        return services.AddAiPersistence();
    }
}
