using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.ContentReports.Application.Abstractions.Common;
using FoodDiary.Modules.ContentReports.Application;
using FoodDiary.Modules.ContentReports.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.ContentReports.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddContentReportsModule(this IServiceCollection services) {
        services.AddContentReportsApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<ContentReportsDbContext>(static options => new ContentReportsDbContext(options)));
        services.AddScoped(static provider => new ContentReportRepository(
            provider.GetRequiredService<ContentReportsDbContext>().ContentReports));
        services.AddScoped<IContentReportWriteRepository>(static provider => provider.GetRequiredService<ContentReportRepository>());
        return services;
    }
}
