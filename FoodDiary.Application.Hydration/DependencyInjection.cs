using FluentValidation;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Hydration;

public static class DependencyInjection {
    public static IServiceCollection AddHydrationModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IHydrationEntryReadService, HydrationEntryReadService>();
        services.AddScoped<IHydrationGoalService, HydrationGoalService>();
        return services;
    }
}
