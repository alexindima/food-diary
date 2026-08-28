using FluentValidation;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Application.Wearables.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Wearables;

public static class DependencyInjection {
    public static IServiceCollection AddWearablesModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IWearableReadService, WearableReadService>();

        return services;
    }
}
