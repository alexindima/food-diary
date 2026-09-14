using FluentValidation;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Common;
using FoodDiary.Modules.Ai.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Ai.Application;

public static class DependencyInjection {
    public static IServiceCollection AddAiApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IOpenAiFoodService, OpenAiFoodService>();
        services.AddScoped<IFoodRecognitionProcessor, FoodRecognitionProcessor>();
        services.AddScoped<IFoodRecognitionResultReader, FoodRecognitionResultReader>();
        services.AddScoped<IAiAdministrationReadService, AiAdministrationReadService>();
        services.AddScoped<IAiPromptAdministrationService, AiPromptAdministrationService>();
        return services;
    }
}
