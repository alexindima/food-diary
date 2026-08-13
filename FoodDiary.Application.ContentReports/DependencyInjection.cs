using FluentValidation;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Application.ContentReports.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.ContentReports;

public static class DependencyInjection {
    public static IServiceCollection AddContentReportsModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IContentReportAdministrationService, ContentReportAdministrationService>();
        services.AddScoped<IContentReportAdministrationReadService, ContentReportAdministrationReadService>();
        return services;
    }
}
