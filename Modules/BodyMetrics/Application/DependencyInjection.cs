using FluentValidation;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Services;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.BodyMetrics.Application;

public static class DependencyInjection {
    public static IServiceCollection AddBodyMetricsApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IWeightEntryReadService, WeightEntryReadService>();
        services.AddScoped<IWaistEntryReadService, WaistEntryReadService>();

        return services;
    }
}
