using FluentValidation;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Export.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Export;

public static class DependencyInjection {
    public static IServiceCollection AddExportModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IExportDiaryReadService, ExportDiaryReadService>();
        return services;
    }
}
