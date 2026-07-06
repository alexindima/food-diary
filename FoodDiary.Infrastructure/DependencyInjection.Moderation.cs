using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Infrastructure.Persistence.ContentReports;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddModerationPersistence(this IServiceCollection services) {
        services.AddScoped<IContentReportRepository, ContentReportRepository>();
        services.AddScoped<IContentReportReadRepository>(static provider => provider.GetRequiredService<IContentReportRepository>());
        services.AddScoped<IContentReportReadModelRepository>(static provider => provider.GetRequiredService<IContentReportRepository>());
        services.AddScoped<IContentReportWriteRepository>(static provider => provider.GetRequiredService<IContentReportRepository>());

        return services;
    }
}
