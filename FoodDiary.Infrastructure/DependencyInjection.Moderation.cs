using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Infrastructure.Persistence.ContentReports;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddModerationPersistence(this IServiceCollection services) {
        services.AddScoped<ContentReportRepository>();
        services.AddScoped<IContentReportReadModelRepository>(static provider => provider.GetRequiredService<ContentReportRepository>());
        services.AddScoped<IContentReportWriteRepository>(static provider => provider.GetRequiredService<ContentReportRepository>());

    }
}
