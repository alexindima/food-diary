using FluentValidation;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Ai;

public static class DependencyInjection {
    public static IServiceCollection AddAiModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
        services.AddScoped<IAiUserContextService, AiUserContextService>();
        services.AddScoped<IAiAdministrationReadService, AiAdministrationReadService>();
        services.AddScoped<IAiPromptAdministrationService, AiPromptAdministrationService>();
        services.AddScoped<IUserAiUsageSummaryReadService, UserAiUsageSummaryReadService>();
        return services;
    }
}
