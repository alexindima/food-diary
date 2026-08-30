using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.ContentReports;
using FoodDiary.Modules.ContentReports.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.ContentReports.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddContentReportsModule(this IServiceCollection services) {
        services.AddContentReportsApplication();
        services.AddScoped<ContentReportRepository>();
        services.AddScoped<IContentReportReadModelRepository>(static provider => provider.GetRequiredService<ContentReportRepository>());
        services.AddScoped<IContentReportWriteRepository>(static provider => provider.GetRequiredService<ContentReportRepository>());
        services.AddScoped<IContentReportTargetReadService>(static provider => provider.GetRequiredService<ContentReportRepository>());
        return services;
    }
}
