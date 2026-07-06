using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddAiPersistence(this IServiceCollection services) {
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();
        services.AddScoped<IAiUsageReadRepository>(static provider => provider.GetRequiredService<IAiUsageRepository>());
        services.AddScoped<IAiUsageWriteRepository>(static provider => provider.GetRequiredService<IAiUsageRepository>());
        services.AddScoped<IAiPromptTemplateRepository, AiPromptTemplateRepository>();
        services.AddScoped<IAiPromptTemplateReadRepository>(static provider => provider.GetRequiredService<IAiPromptTemplateRepository>());
        services.AddScoped<IAiPromptTemplateReadModelRepository>(static provider => provider.GetRequiredService<IAiPromptTemplateRepository>());
        services.AddScoped<IAiPromptTemplateWriteRepository>(static provider => provider.GetRequiredService<IAiPromptTemplateRepository>());

        return services;
    }
}
