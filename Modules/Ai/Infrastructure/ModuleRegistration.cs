using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class ModuleRegistration {
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static IServiceCollection AddAiPersistence(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, AiUserDataPurgeParticipant>());
        services.AddSingleton<IAiPromptProvider, AiPromptProvider>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();
        services.AddScoped<IAiUsageReadRepository>(static provider => provider.GetRequiredService<IAiUsageRepository>());
        services.AddScoped<IAiUsageWriteRepository>(static provider => provider.GetRequiredService<IAiUsageRepository>());
        services.AddScoped<IAiQuotaRepository, AiQuotaRepository>();
        services.AddScoped<IAiPromptTemplateRepository, AiPromptTemplateRepository>();
        services.AddScoped<IAiPromptTemplateReadRepository>(static provider => provider.GetRequiredService<IAiPromptTemplateRepository>());
        services.AddScoped<IAiPromptTemplateReadModelRepository>(static provider => provider.GetRequiredService<IAiPromptTemplateRepository>());
        services.AddScoped<IAiPromptTemplateWriteRepository>(static provider => provider.GetRequiredService<IAiPromptTemplateRepository>());

        return services;
    }

    public static IServiceCollection AddAiModule(this IServiceCollection services) {
        FoodDiary.Application.Ai.DependencyInjection.AddAiApplication(services);
        return services.AddAiPersistence();
    }
}
