using FluentValidation;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Cycles;

public static class DependencyInjection {
    public static IServiceCollection AddCyclesModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<ICycleReadService, CycleReadService>();
        return services;
    }
}
